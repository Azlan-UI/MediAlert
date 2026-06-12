using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MediAlert.Services.Doctors.Interfaces;

namespace MediAlert.Services.Doctors;

public sealed class ZoomApiService : IZoomApiService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ZoomApiService> _logger;
    private readonly HttpClient _httpClient;

    public ZoomApiService(IConfiguration configuration, ILogger<ZoomApiService> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<(string MeetingId, string JoinUrl)> CreateMeetingAsync(string topic, DateTime scheduledTime, int durationMinutes = 30, CancellationToken cancellationToken = default)
    {
        var accountId = _configuration["Zoom:AccountId"];
        var clientId = _configuration["Zoom:ClientId"];
        var clientSecret = _configuration["Zoom:ClientSecret"];

        if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            _logger.LogWarning("Zoom credentials not configured. Returning fallback mock meeting.");
            return ($"Z-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}", $"https://zoom.us/j/mock_meeting?pwd=fallback");
        }

        try
        {
            // 1. Get OAuth Token
            var tokenRequestUrl = "https://zoom.us/oauth/token";
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenRequestUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "account_credentials" },
                    { "account_id", accountId }
                })
            };
            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);

            var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
            tokenResponse.EnsureSuccessStatusCode();

            var tokenContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            using var tokenDocument = JsonDocument.Parse(tokenContent);
            var accessToken = tokenDocument.RootElement.GetProperty("access_token").GetString();

            var userEmail = _configuration["Zoom:UserEmail"];
            if (string.IsNullOrEmpty(userEmail))
            {
                throw new Exception("Zoom:UserEmail is not configured. Server-to-Server apps require a specific user email instead of 'me'.");
            }

            // 2. Create Meeting
            var meetingRequestUrl = $"https://api.zoom.us/v2/users/{userEmail}/meetings";
            var meetingPayload = new
            {
                topic = topic,
                type = 2, // Scheduled meeting
                start_time = scheduledTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                duration = durationMinutes,
                timezone = "UTC",
                settings = new { host_video = true, participant_video = true, join_before_host = false }
            };

            var meetingRequest = new HttpRequestMessage(HttpMethod.Post, meetingRequestUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(meetingPayload), Encoding.UTF8, "application/json")
            };
            meetingRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var meetingResponse = await _httpClient.SendAsync(meetingRequest, cancellationToken);
            meetingResponse.EnsureSuccessStatusCode();

            var meetingContent = await meetingResponse.Content.ReadAsStringAsync(cancellationToken);
            using var meetingDocument = JsonDocument.Parse(meetingContent);
            var meetingId = meetingDocument.RootElement.GetProperty("id").GetInt64().ToString();
            var joinUrl = meetingDocument.RootElement.GetProperty("join_url").GetString()!;

            return (meetingId, joinUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Zoom meeting via API. Using fallback.");
            return ($"Z-ERR-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}", $"https://zoom.us/j/error_fallback");
        }
    }
}
