using System.Threading;
using System.Threading.Tasks;

namespace MediAlert.Services.Doctors.Interfaces;

public interface IZoomApiService
{
    Task<(string MeetingId, string JoinUrl)> CreateMeetingAsync(string topic, DateTime scheduledTime, int durationMinutes = 30, CancellationToken cancellationToken = default);
}
