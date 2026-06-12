using MediAlert.Repositories.Billing;
using MediAlert.Repositories.Billing.Interfaces;
using MediAlert.Services.Notifications;
using MediAlert.Repositories.Caregiver;
using MediAlert.Repositories.Caregiver.Interfaces;
using MediAlert.Repositories.Doctors;
using MediAlert.Repositories.Doctors.Interfaces;
using MediAlert.Services.Appointments;
using MediAlert.Services.Appointments.Interfaces;
using MediAlert.Services.Billing;
using MediAlert.Services.Billing.Interfaces;
using MediAlert.Services.Caregiver;
using MediAlert.Services.Caregiver.Interfaces;
using MediAlert.Services.Doctors;
using MediAlert.Services.Doctors.Interfaces;
using MediAlert.Services.Reports;
using MediAlert.Services.Reports.Interfaces;
using MediAlert.Services.Reports.Queries;
using MediAlert.Services.Reports.Statistics;

namespace MediAlert.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddModule6To10Services(this IServiceCollection services)
    {
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddHttpClient<IZoomApiService, ZoomApiService>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<ICaregiverService, CaregiverService>();
        services.AddScoped<ICaregiverRepository, CaregiverRepository>();
        services.AddScoped<IStripeBillingService, StripeBillingService>();
        services.AddScoped<IBillingRepository, BillingRepository>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IPdfExportService, ComplianceReportPdfExportService>();
        services.AddScoped<IReportQueryLayer, ReportQueryLayer>();
        services.AddScoped<IStatisticsEngine, StatisticsEngine>();
        services.AddScoped<MediAlert.Services.HealthProfile.Interfaces.IHealthProfileService, MediAlert.Services.HealthProfile.HealthProfileService>();
        services.AddScoped<MediAlert.Services.Admin.Interfaces.IAdminService, MediAlert.Services.Admin.AdminService>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }
}
