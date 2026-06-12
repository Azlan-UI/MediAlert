using MediAlert.Repositories.Compliance;
using MediAlert.Repositories.Compliance.Interfaces;
using MediAlert.Services.Compliance.Interfaces;

namespace MediAlert.Services.Compliance;

public static class ComplianceServiceCollectionExtensions
{
    public static IServiceCollection AddComplianceServices(this IServiceCollection services)
    {
        services.AddScoped<IComplianceRepository, ComplianceRepository>();
        services.AddScoped<IComplianceService, ComplianceService>();

        return services;
    }
}
