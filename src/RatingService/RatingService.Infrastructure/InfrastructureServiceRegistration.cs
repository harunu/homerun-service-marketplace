using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RatingService.Core.Interfaces;
using RatingService.Infrastructure.Data;
using RatingService.Infrastructure.Messaging;
using RatingService.Infrastructure.Repositories;
using ServiceMarketplace.Shared.Contracts;

namespace RatingService.Infrastructure
{
    /// <summary>
    /// Registers application infrastructure dependencies (DB, messaging, repositories).
    /// </summary>
    public static class InfrastructureServiceRegistration
    {
        /// <summary>
        /// Adds infrastructure services to the application dependency injection container.
        /// </summary>
        /// <param name="services">The DI service collection.</param>
        /// <param name="configuration">Application configuration settings.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddInfrastructureService(this IServiceCollection services, IConfiguration configuration)
        {
            // Register Database Context
            services.AddDbContext<RatingDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("RatingDb")));

            // Register Repositories
            services.AddScoped<IRatingRepository, RatingRepository>();

            // Configure RabbitMQ settings
            services.Configure<RabbitMqSettings>(options =>
                configuration.GetSection("RabbitMq").Bind(options));

            // Register Event Publisher for messaging
            services.AddSingleton<IEventPublisher<RatingCreatedEvent>, RabbitMqEventPublisher<RatingCreatedEvent>>();

            return services;
        }
    }
}
