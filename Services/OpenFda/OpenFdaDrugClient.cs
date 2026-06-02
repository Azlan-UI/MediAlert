using MediAlert.Configuration;
using MediAlert.Constants;
using MediAlert.DTOs.OpenFda;
using MediAlert.Services.OpenFda.Interfaces;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediAlert.Services.OpenFda;

public sealed class OpenFdaDrugClient : IOpenFdaDrugClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly OpenFdaSettings _settings;
    private readonly ILogger<OpenFdaDrugClient> _logger;

    public OpenFdaDrugClient(
        HttpClient httpClient,
        IOptions<OpenFdaSettings> settings,
        ILogger<OpenFdaDrugClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<OpenFdaClientResult<OpenFdaDrugSearchResponse>> SearchDrugLabelsAsync(
        OpenFdaDrugSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = SanitizeSearchTerm(request.Query);
        if (string.IsNullOrWhiteSpace(query))
        {
            return OpenFdaClientResult<OpenFdaDrugSearchResponse>.Failure(
                OpenFdaErrorCodes.InvalidRequest,
                "A drug search query is required.",
                HttpStatusCode.BadRequest);
        }

        var limit = NormalizeLimit(request.Limit);
        var skip = Math.Max(0, request.Skip ?? 0);
        var searchExpression = BuildDrugLabelSearchExpression(query);
        var requestUri = BuildDrugLabelUri(searchExpression, limit, skip);

        var rawResult = await SendWithRetryAsync(requestUri, cancellationToken);
        if (!rawResult.Succeeded)
        {
            if (rawResult.StatusCode == HttpStatusCode.NotFound)
            {
                return OpenFdaClientResult<OpenFdaDrugSearchResponse>.Success(
                    new OpenFdaDrugSearchResponse { Query = query },
                    HttpStatusCode.OK);
            }

            return OpenFdaClientResult<OpenFdaDrugSearchResponse>.Failure(
                rawResult.ErrorCode ?? OpenFdaErrorCodes.RequestFailed,
                rawResult.ErrorMessage ?? "OpenFDA request failed.",
                rawResult.StatusCode,
                rawResult.RetryAfter);
        }

        var response = rawResult.Data;
        var mapped = MapSearchResponse(query, response);

        return OpenFdaClientResult<OpenFdaDrugSearchResponse>.Success(
            mapped,
            rawResult.StatusCode);
    }

    public async Task<OpenFdaClientResult<OpenFdaLabelApiResponse>> GetRawDrugLabelAsync(
        string query,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var sanitizedQuery = SanitizeSearchTerm(query);
        if (string.IsNullOrWhiteSpace(sanitizedQuery))
        {
            return OpenFdaClientResult<OpenFdaLabelApiResponse>.Failure(
                OpenFdaErrorCodes.InvalidRequest,
                "A drug label query is required.",
                HttpStatusCode.BadRequest);
        }

        var requestUri = BuildDrugLabelUri(
            BuildDrugLabelSearchExpression(sanitizedQuery),
            NormalizeLimit(limit),
            skip: 0);

        return await SendWithRetryAsync(requestUri, cancellationToken);
    }

    private async Task<OpenFdaClientResult<OpenFdaLabelApiResponse>> SendWithRetryAsync(
        string requestUri,
        CancellationToken cancellationToken)
    {
        var totalAttempts = Math.Max(1, _settings.MaxRetryAttempts + 1);

        for (var attempt = 1; attempt <= totalAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            try
            {
                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<OpenFdaLabelApiResponse>(
                        JsonOptions,
                        cancellationToken);

                    if (data is null)
                    {
                        _logger.LogWarning("OpenFDA returned an empty JSON body for {RequestUri}", requestUri);

                        return OpenFdaClientResult<OpenFdaLabelApiResponse>.Failure(
                            OpenFdaErrorCodes.InvalidResponse,
                            "OpenFDA returned an empty response body.",
                            response.StatusCode);
                    }

                    return OpenFdaClientResult<OpenFdaLabelApiResponse>.Success(
                        data,
                        response.StatusCode);
                }

                var retryAfter = GetRetryAfter(response);
                if (ShouldRetry(response.StatusCode) && attempt < totalAttempts)
                {
                    var delay = retryAfter ?? GetRetryDelay(attempt);

                    _logger.LogWarning(
                        "OpenFDA request returned {StatusCode}. Retrying attempt {Attempt}/{TotalAttempts} after {Delay}ms.",
                        (int)response.StatusCode,
                        attempt,
                        totalAttempts,
                        delay.TotalMilliseconds);

                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                return await CreateHttpFailureAsync(response, retryAfter, cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt < totalAttempts)
                {
                    var delay = GetRetryDelay(attempt);
                    _logger.LogWarning(
                        "OpenFDA request timed out. Retrying attempt {Attempt}/{TotalAttempts} after {Delay}ms.",
                        attempt,
                        totalAttempts,
                        delay.TotalMilliseconds);

                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                return OpenFdaClientResult<OpenFdaLabelApiResponse>.Failure(
                    OpenFdaErrorCodes.Timeout,
                    "OpenFDA request timed out.",
                    HttpStatusCode.RequestTimeout);
            }
            catch (HttpRequestException ex)
            {
                if (attempt < totalAttempts)
                {
                    var delay = GetRetryDelay(attempt);
                    _logger.LogWarning(
                        ex,
                        "OpenFDA network request failed. Retrying attempt {Attempt}/{TotalAttempts} after {Delay}ms.",
                        attempt,
                        totalAttempts,
                        delay.TotalMilliseconds);

                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                _logger.LogError(ex, "OpenFDA network request failed after {Attempts} attempts.", totalAttempts);

                return OpenFdaClientResult<OpenFdaLabelApiResponse>.Failure(
                    OpenFdaErrorCodes.Unavailable,
                    "OpenFDA is unavailable or the network request failed.",
                    ex.StatusCode);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "OpenFDA returned invalid JSON.");

                return OpenFdaClientResult<OpenFdaLabelApiResponse>.Failure(
                    OpenFdaErrorCodes.InvalidResponse,
                    "OpenFDA returned invalid JSON.");
            }
        }

        return OpenFdaClientResult<OpenFdaLabelApiResponse>.Failure(
            OpenFdaErrorCodes.RequestFailed,
            "OpenFDA request failed.");
    }

    private async Task<OpenFdaClientResult<OpenFdaLabelApiResponse>> CreateHttpFailureAsync(
        HttpResponseMessage response,
        TimeSpan? retryAfter,
        CancellationToken cancellationToken)
    {
        var responseText = await ReadSafeResponseTextAsync(response, cancellationToken);

        _logger.LogWarning(
            "OpenFDA request failed with status {StatusCode}. Response: {ResponseText}",
            (int)response.StatusCode,
            responseText);

        var errorCode = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => OpenFdaErrorCodes.InvalidRequest,
            HttpStatusCode.NotFound => OpenFdaErrorCodes.NotFound,
            HttpStatusCode.TooManyRequests => OpenFdaErrorCodes.RateLimited,
            HttpStatusCode.RequestTimeout => OpenFdaErrorCodes.Timeout,
            HttpStatusCode.ServiceUnavailable => OpenFdaErrorCodes.Unavailable,
            HttpStatusCode.GatewayTimeout => OpenFdaErrorCodes.Timeout,
            _ => OpenFdaErrorCodes.RequestFailed,
        };

        var message = response.StatusCode == HttpStatusCode.TooManyRequests
            ? "OpenFDA rate limit reached. Try again later."
            : "OpenFDA request failed.";

        return OpenFdaClientResult<OpenFdaLabelApiResponse>.Failure(
            errorCode,
            message,
            response.StatusCode,
            retryAfter);
    }

    private string BuildDrugLabelUri(string searchExpression, int limit, int skip)
    {
        var endpoint = NormalizeEndpoint(_settings.DrugLabelEndpoint);
        var queryParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            queryParts.Add($"api_key={Uri.EscapeDataString(_settings.ApiKey)}");
        }

        queryParts.Add($"search={Uri.EscapeDataString(searchExpression)}");
        queryParts.Add($"limit={limit}");

        if (skip > 0)
        {
            queryParts.Add($"skip={skip}");
        }

        return $"{endpoint}?{string.Join("&", queryParts)}";
    }

    private string BuildDrugLabelSearchExpression(string query)
    {
        var quotedQuery = $"\"{query}\"";

        return string.Join(
            " OR ",
            $"openfda.brand_name:{quotedQuery}",
            $"openfda.generic_name:{quotedQuery}",
            $"openfda.substance_name:{quotedQuery}",
            $"active_ingredient:{quotedQuery}");
    }

    private int NormalizeLimit(int? requestedLimit)
    {
        var defaultLimit = Math.Max(1, _settings.DefaultLimit);
        var maxLimit = Math.Max(defaultLimit, _settings.MaxLimit);
        var requested = requestedLimit.GetValueOrDefault(defaultLimit);

        return Math.Clamp(requested, 1, maxLimit);
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return "/drug/label.json";
        }

        return endpoint.StartsWith('/')
            ? endpoint
            : $"/{endpoint}";
    }

    private static string SanitizeSearchTerm(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        var trimmed = query.Trim();
        var safe = Regex.Replace(trimmed, @"[^\w\s\-\./]", string.Empty);

        return Regex.Replace(safe, @"\s+", " ").Trim();
    }

    private static bool ShouldRetry(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            || (int)statusCode >= 500;

    private TimeSpan GetRetryDelay(int attempt)
    {
        var baseDelay = Math.Max(100, _settings.RetryDelayMilliseconds);
        var delay = baseDelay * Math.Pow(2, attempt - 1);

        return TimeSpan.FromMilliseconds(Math.Min(delay, 10_000));
    }

    private static TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter?.Delta is not null)
        {
            return retryAfter.Delta.Value;
        }

        if (retryAfter?.Date is not null)
        {
            var delay = retryAfter.Date.Value - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }

        return null;
    }

    private static async Task<string> ReadSafeResponseTextAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (responseText.Length <= 500)
        {
            return responseText;
        }

        return $"{responseText[..500]}...";
    }

    private static OpenFdaDrugSearchResponse MapSearchResponse(
        string query,
        OpenFdaLabelApiResponse? response)
    {
        var results = response?.Results ?? [];

        return new OpenFdaDrugSearchResponse
        {
            Query = query,
            TotalResults = response?.Meta?.Results?.Total ?? results.Count,
            Results = results.Select(MapSearchItem).ToList(),
        };
    }

    private static OpenFdaDrugSearchItem MapSearchItem(OpenFdaLabelResult result) =>
        new()
        {
            LabelId = result.Id,
            SetId = result.SetId,
            EffectiveTime = result.EffectiveTime,
            BrandNames = ToList(result.OpenFda?.BrandName),
            GenericNames = ToList(result.OpenFda?.GenericName),
            ManufacturerNames = ToList(result.OpenFda?.ManufacturerName),
            ProductTypes = ToList(result.OpenFda?.ProductType),
            Routes = ToList(result.OpenFda?.Route),
            SubstanceNames = ToList(result.OpenFda?.SubstanceName),
            Warnings = ToList(result.Warnings),
            BoxedWarnings = ToList(result.BoxedWarnings),
            Contraindications = ToList(result.Contraindications),
            DrugInteractions = ToList(result.DrugInteractions),
        };

    private static List<string> ToList(IEnumerable<string>? values) =>
        values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
        ?? [];
}
