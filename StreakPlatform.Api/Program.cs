using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using StreakPlatform.Api.Auth;
using StreakPlatform.Api.Middleware;
using StreakPlatform.Infrastructure;
using StreakPlatform.Infrastructure.Persistence;
using AppOpts = StreakPlatform.Application.Common.AppOptions;

// Bootstrap a static Serilog logger as early as possible so even startup
// failures get captured to the rolling log file.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Connection", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Transaction", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine("Logs", "streak-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Streak API");

    var builder = WebApplication.CreateBuilder(args);

    // Replace default ASP.NET logging with Serilog. This routes ALL framework logs
    // (including EF Core SQL command logging) through Serilog's pipeline,
    // so SQL queries appear on console AND in the daily rolling file.
    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: Path.Combine("Logs", "streak-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}"));

    // Firebase Admin — service account from configured path or GOOGLE_APPLICATION_CREDENTIALS.
    var firebaseCredPath = builder.Configuration["Firebase:ServiceAccountPath"];
    if (FirebaseApp.DefaultInstance is null)
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = string.IsNullOrWhiteSpace(firebaseCredPath)
                ? GoogleCredential.GetApplicationDefault()
                : GoogleCredential.FromFile(firebaseCredPath)
        });
    }

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
    builder.Services.AddControllers()
        .AddJsonOptions(o =>
        {
            // Accept and emit enum names as strings (e.g. "Action", "Like") so the
            // frontend doesn't have to know integer values.
            o.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter(allowIntegerValues: true));
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? new[] { "http://localhost:4200" };
    builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
        p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

    var app = builder.Build();

    // Auto-apply migrations on startup for dev convenience.
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    // HTTP request logging — one structured line per request to console + file.
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    // Static file serving for uploaded media at {MediaPublicPath}.
    var appOpts = app.Services.GetRequiredService<IOptions<AppOpts>>().Value;
    var mediaPath = Path.Combine(builder.Environment.ContentRootPath, appOpts.MediaStorageDirectory);
    Directory.CreateDirectory(mediaPath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(mediaPath),
        RequestPath = appOpts.MediaPublicPath
    });

    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors();

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<FirebaseAuthMiddleware>();

    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Streak API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
