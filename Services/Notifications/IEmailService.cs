using System.Threading.Tasks;

namespace MediAlert.Services.Notifications;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}
