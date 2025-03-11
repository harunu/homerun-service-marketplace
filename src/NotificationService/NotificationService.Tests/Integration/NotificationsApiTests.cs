using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Core.Entities;
using NotificationService.Infrastructure.Data;
using ServiceMarketplace.Shared.Contracts;
using System.Net;
using System.Net.Http.Json;
using Testcontainers.MsSql;

namespace NotificationService.Tests.Integration
{
    public class NotificationsApiTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly NotificationDbContext _dbContext;

        public NotificationsApiTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();

            var scope = factory.Services.CreateScope();
            _dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        }

        [Fact]
        public async Task GetNotifications_WithUnreadNotifications_ShouldReturnAndMarkAsRead()
        {
            // Arrange
            var serviceProviderId = Guid.NewGuid();
            var notifications = new List<Notification>
            {
                new Notification
                {
                    Id = Guid.NewGuid(),
                    ServiceProviderId = serviceProviderId,
                    Message = "Notification 1",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                    IsRead = false
                },
                new Notification
                {
                    Id = Guid.NewGuid(),
                    ServiceProviderId = serviceProviderId,
                    Message = "Notification 2",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                    IsRead = false
                }
            };

            await _dbContext.Notifications.AddRangeAsync(notifications);
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await _client.GetAsync($"/api/notifications/service-provider/{serviceProviderId}");
            var content = await response.Content.ReadFromJsonAsync<NotificationsResponse>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content);
            Assert.Equal(2, content.Notifications.Count());

            _dbContext.ChangeTracker.Clear();

            // Verify notifications are marked as read in the database
            var dbNotifications = await _dbContext.Notifications
                .Where(n => n.ServiceProviderId == serviceProviderId)
                .ToListAsync();

            Assert.All(dbNotifications, n => Assert.True(n.IsRead));

            // Second call should return empty
            var secondResponse = await _client.GetAsync($"/api/notifications/service-provider/{serviceProviderId}");
            var secondContent = await secondResponse.Content.ReadFromJsonAsync<NotificationsResponse>();

            Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
            Assert.NotNull(secondContent);
            Assert.Empty(secondContent.Notifications);
        }

        [Fact]
        public async Task GetNotifications_WithNoNotifications_ShouldReturnEmptyList()
        {
            // Arrange
            var serviceProviderId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/notifications/service-provider/{serviceProviderId}");
            var content = await response.Content.ReadFromJsonAsync<NotificationsResponse>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content);
            Assert.Empty(content.Notifications);
        }

        public void Dispose()
        {
            // Clean up the database after each test
            _dbContext.Notifications.RemoveRange(_dbContext.Notifications);
            _dbContext.SaveChanges();
        }
    }

    // Test container setup for integration testing
    public class CustomWebApplicationFactory : WebApplicationFactory<NotificationService.Api.Program>, IAsyncLifetime
    {

        private readonly MsSqlContainer _msSqlContainer;
        private readonly string _databaseName;
        private readonly string _dbPassword;

        public CustomWebApplicationFactory()
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

            var config = new ConfigurationBuilder()
                .AddJsonFile(configPath, optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            _databaseName = config["TestDatabase:Name"]
                ?? throw new InvalidOperationException("Missing required configuration: TestDatabase:Name");

            _dbPassword = config["TestDatabase:Password"]
                ?? throw new InvalidOperationException("Missing required configuration: TestDatabase:Password");

            _msSqlContainer = new MsSqlBuilder()
                .WithPassword(_dbPassword)
                .Build();
        }

        public async Task InitializeAsync()
        {
            await _msSqlContainer.StartAsync();
        }

        public new async Task DisposeAsync()
        {
            await _msSqlContainer.DisposeAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var integrationConfig = new Dictionary<string, string?>
        {
            { "IntegrationTestMode", "true" }
        };
                config.AddInMemoryCollection(integrationConfig);
            });

            builder.ConfigureServices((context, services) =>
            {
                //  Remove any existing `DbContextOptions<RatingDbContext>` registrations
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<NotificationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                //  Ensure Testcontainers SQL connection string is used
                var baseConnectionString = _msSqlContainer.GetConnectionString();
                var builder = new SqlConnectionStringBuilder(baseConnectionString)
                {
                    InitialCatalog = _databaseName
                };
                string connectionString = builder.ConnectionString;
                // DEBUG Console.WriteLine($"Using Testcontainers Connection String: {connectionString}");

                //  Register DbContext with the correct connection string
                services.AddDbContext<NotificationDbContext>(options =>
                {
                    options.UseSqlServer(connectionString);
                });

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

                Console.WriteLine("Resetting and migrating test database...");
                db.Database.EnsureDeleted();  //  Ensures a clean DB before each test
                db.Database.EnsureCreated();
                db.Database.Migrate();
            });
        }

    }
}