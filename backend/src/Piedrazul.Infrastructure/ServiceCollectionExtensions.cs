using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Piedrazul.Application.Abstractions.Infrastructure;
using Piedrazul.Application.Abstractions.Repositories;
using Piedrazul.Infrastructure.Cache;
using Piedrazul.Infrastructure.Notifications;
using Piedrazul.Infrastructure.Observability;
using Piedrazul.Infrastructure.Persistence;
using Piedrazul.Infrastructure.Persistence.Repositories;
using Piedrazul.Infrastructure.Services;
using StackExchange.Redis;

namespace Piedrazul.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();

        services.AddScoped<IAppointmentPdfExporter, AppointmentPdfExporter>();
        services.AddScoped<IAuditLogger, SerilogAuditLogger>();

        var redisConnectionString = configuration.GetSection("Redis").GetValue<string>("ConnectionString")
                                    ?? configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<ICacheService, NullCacheService>();
        }
        else
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnectionString + ",abortConnect=false"));
            services.AddSingleton<ICacheService, RedisCacheService>();
        }

        var notificationsBaseUrl = configuration.GetSection("Notifications").GetValue<string>("BaseUrl");
        if (string.IsNullOrWhiteSpace(notificationsBaseUrl))
        {
            services.AddSingleton<INotificationClient, NoOpNotificationClient>();
        }
        else
        {
            services.AddHttpClient<INotificationClient, HttpNotificationClient>(client =>
            {
                client.BaseAddress = new Uri(notificationsBaseUrl);
            });
        }

        return services;
    }
}
