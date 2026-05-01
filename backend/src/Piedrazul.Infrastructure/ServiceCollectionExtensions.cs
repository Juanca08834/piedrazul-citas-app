using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Piedrazul.Application;
using Piedrazul.Infrastructure.Persistence;
using Piedrazul.Infrastructure.Services;

namespace Piedrazul.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IAdministrationService, AdministrationService>();
        services.AddScoped<IAppointmentPdfExporter, AppointmentPdfExporter>();

        return services;
    }
}
