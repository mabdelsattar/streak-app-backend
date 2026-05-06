using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreakPlatform.Application.Common;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Application.Services;
using StreakPlatform.Infrastructure.Persistence;
using StreakPlatform.Infrastructure.Storage;

namespace StreakPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default (SQL Server).");

        var enableSensitive = config.GetValue("Database:EnableSensitiveDataLogging", false);
        var enableDetailedErrors = config.GetValue("Database:EnableDetailedErrors", false);

        services.AddDbContext<AppDbContext>(o =>
        {
            o.UseSqlServer(cs);
            // EF Core logs (commands, parameters, transactions, connection events) flow into the
            // ASP.NET Core ILoggerFactory automatically via UseLoggerFactory if registered.
            // We just toggle the verbose/sensitive switches based on appsettings.
            if (enableSensitive) o.EnableSensitiveDataLogging();
            if (enableDetailedErrors) o.EnableDetailedErrors();
        });

        services.Configure<AppOptions>(config.GetSection("App"));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IStreakRepository, StreakRepository>();
        services.AddScoped<IParticipantRepository, ParticipantRepository>();
        services.AddScoped<ICheckInRepository, CheckInRepository>();
        services.AddScoped<IProtectionRepository, ProtectionRepository>();
        services.AddScoped<IPointsTransactionRepository, PointsTransactionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Helpers
        services.AddScoped<InviteCodeGenerator>();
        services.AddScoped<InviteUrlBuilder>();

        // Storage
        services.AddSingleton<IMediaStorage, LocalMediaStorage>();

        // Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IStreakService, StreakService>();
        services.AddScoped<ICheckInService, CheckInService>();
        services.AddScoped<IPointsService, PointsService>();
        services.AddScoped<IStreakProtectionService, StreakProtectionService>();

        return services;
    }
}
