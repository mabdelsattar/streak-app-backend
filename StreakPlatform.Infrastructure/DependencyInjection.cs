using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreakPlatform.Application.Common;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Application.Services;
using StreakPlatform.Infrastructure.Persistence;

namespace StreakPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default (SQL Server).");
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(cs));

        services.Configure<AppOptions>(config.GetSection("App"));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IStreakRepository, StreakRepository>();
        services.AddScoped<IParticipantRepository, ParticipantRepository>();
        services.AddScoped<ICheckInRepository, CheckInRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<InviteCodeGenerator>();
        services.AddScoped<InviteUrlBuilder>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IStreakService, StreakService>();
        services.AddScoped<ICheckInService, CheckInService>();

        return services;
    }
}
