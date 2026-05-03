using Piedrazul.Application;

namespace Piedrazul.Infrastructure.Services;

public interface IAppointmentPdfExporter
{
    byte[] Export(string centerName, string providerName, string specialty, DateOnly date, IReadOnlyList<AppointmentResponse> appointments);
}
