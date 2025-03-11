using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NotificationService.Core.Entities;
using NotificationService.Core.Interfaces;
using NotificationService.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


//  Configure Serilog for structured logging

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

//  Register services
ConfigureServices(builder);

var app = builder.Build();

//  Configure middleware and app settings
ConfigureMiddleware(app);

app.Run();

//  Encapsulated service configuration
void ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    //  OpenAPI Documentation (Swagger)
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Notification Service API", Version = "v1" });
    });

    //  Dependency Injection
    builder.Services.AddScoped<INotificationService, NotificationService.Core.Services.NotificationService>();

    //  Register infrastructure dependencies
    builder.Services.AddInfrastructure(builder.Configuration);

    //  Health checks for monitoring
    builder.Services.AddHealthChecks();
    builder.Configuration.AddEnvironmentVariables();
}

//  Encapsulated middleware configuration
void ConfigureMiddleware(WebApplication app)
{
    //  Enable Swagger for both development and production
    if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification Service API v1"));
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    //  Health check endpoint
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                components = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString())
            }));
        }
    });

    InitializeDatabase(app);
}

// Database Initialization
void InitializeDatabase(WebApplication app)
{
    var isIntegrationTest = app.Configuration.GetValue<bool>("IntegrationTestMode");
    var isRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

    if (isIntegrationTest)
    {
        app.Logger.LogInformation("Skipping database initialization (running in integration test mode).");
        return;
    }

    int maxRetries = 10;
    int retryDelayMs = 5000;

    for (int retryCount = 0; retryCount < maxRetries; retryCount++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationService.Infrastructure.Data.NotificationDbContext>();

            var connectionString = app.Configuration.GetConnectionString("NotificationDb");
            //app.Logger.LogInformation($"Database Connection String: {connectionString}");

            //  Ensure database exists
            if (!dbContext.Database.CanConnect())
            {
                app.Logger.LogInformation("Database does not exist. Creating it...");
                dbContext.Database.EnsureCreated();
            }

            //  Check if Notifications table exists before running migrations
            if (dbContext.Model.FindEntityType(typeof(Notification)) == null)
            {
                app.Logger.LogInformation("Notifications table not found! Running migrations...");
                dbContext.Database.Migrate();
            }
            else
            {
                app.Logger.LogInformation("Notifications table exists. Skipping migration.");
            }

            app.Logger.LogInformation("Database migration check completed successfully.");
            break;
        }
        catch (Exception ex)
        {
            if (retryCount == maxRetries - 1)
            {
                app.Logger.LogError(ex, "Database migration failed after {RetryCount} attempts", maxRetries);
                throw;
            }

            app.Logger.LogWarning(ex, "Database migration error. Retrying {RetryCount} of {MaxRetries} in {RetryDelayMs} ms...",
                retryCount + 1, maxRetries, retryDelayMs);

            Thread.Sleep(retryDelayMs);
        }
    }
}
namespace NotificationService.Api
{
    public partial class Program { }
}
