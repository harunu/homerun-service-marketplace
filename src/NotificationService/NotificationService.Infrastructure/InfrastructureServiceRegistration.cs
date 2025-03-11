using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Core.Events;
using NotificationService.Core.Interfaces;
using NotificationService.Infrastructure.Data;
using NotificationService.Infrastructure.Messaging;
using NotificationService.Infrastructure.Repositories;

namespace NotificationService.Infrastructure
{
    /// <summary>
    /// Registers infrastructure-related dependencies such as database context, repositories, and messaging components.
    /// </summary>
    public static class InfrastructureServiceRegistration
    {
        /// <summary>
        /// Adds infrastructure services to the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Register database context with SQL Server provider
            services.AddDbContext<NotificationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("NotificationDb")));

            // Register repositories
            services.AddScoped<INotificationRepository, NotificationRepository>();

            // Configure RabbitMQ settings from configuration
            services.Configure<RabbitMqSettings>(options =>
            {
                configuration.GetSection("RabbitMq").Bind(options);
            });

            // Register RabbitMQ consumer as a hosted background service
            services.AddHostedService<RabbitMqEventConsumer>();

            // Register event handler for processing rating events
            services.AddScoped<RatingCreatedEventHandler>();

            return services;
        }
    }
}
