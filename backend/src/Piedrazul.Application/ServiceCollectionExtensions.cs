using Microsoft.Extensions.DependencyInjection;

namespace Piedrazul.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IAdministrationService, AdministrationService>();
        return services;
    }
}
