using MediAlert.Configuration;
using MediAlert.Services.OpenFda.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace MediAlert.Services.OpenFda;

public static class OpenFdaServiceCollectionExtensions
{
    public static IServiceCollection AddOpenFdaIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OpenFdaSettings>(
            configuration.GetSection("OpenFda"));

        services
            .AddHttpClient<IOpenFdaDrugClient, OpenFdaDrugClient>((serviceProvider, client) =>
            {
                var settings = serviceProvider
                    .GetRequiredService<IOptions<OpenFdaSettings>>()
                    .Value;

                client.BaseAddress = new Uri(settings.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds));
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                if (!string.IsNullOrWhiteSpace(settings.UserAgent))
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
                }
            });

        return services;
    }
}
