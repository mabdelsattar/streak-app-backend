using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using StreakPlatform.Api.Auth;
using StreakPlatform.Api.Middleware;
using StreakPlatform.Infrastructure;
using StreakPlatform.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddControllers();
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

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<FirebaseAuthMiddleware>();

app.MapControllers();
app.Run();
