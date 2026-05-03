using Piedrazul.Application;

namespace Piedrazul.Application.Abstractions.Infrastructure;

public interface IAppointmentPdfExporter
{
    byte[] Export(string centerName, string providerName, string specialty, DateOnly date, IReadOnlyList<AppointmentResponse> appointments);
}
