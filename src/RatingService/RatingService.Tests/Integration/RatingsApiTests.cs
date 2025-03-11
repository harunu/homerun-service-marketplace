using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RatingService.Core.Entities;
using RatingService.Infrastructure.Data;
using ServiceMarketplace.Shared.Contracts;
using System.Net;
using System.Net.Http.Json;
using Testcontainers.MsSql;

public class RatingsApiTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly RatingDbContext _dbContext;

    public RatingsApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        var scope = factory.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<RatingDbContext>();
    }

    [Fact]
    public async Task CreateRating_ShouldReturnCreatedStatus()
    {
        // Arrange
        var request = new CreateRatingRequest
        {
            ServiceProviderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Score = 4,
            Comment = "Good service"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ratings", request);
        var content = await response.Content.ReadFromJsonAsync<RatingDto>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(content);
        Assert.Equal(request.ServiceProviderId, content.ServiceProviderId);
        Assert.Equal(request.CustomerId, content.CustomerId);
        Assert.Equal(request.Score, content.Score);
        Assert.Equal(request.Comment, content.Comment);
    }

    [Fact]
    public async Task GetAverageRating_WithExistingRatings_ShouldReturnCorrectAverage()
    {
        // Arrange
        var serviceProviderId = Guid.NewGuid();
        var ratings = new List<Rating>
        {
            new Rating
            {
                Id = Guid.NewGuid(),
                ServiceProviderId = serviceProviderId,
                CustomerId = Guid.NewGuid(),
                Score = 4,
                Comment = "Good",
                CreatedAt = DateTime.UtcNow
            },
            new Rating
            {
                Id = Guid.NewGuid(),
                ServiceProviderId = serviceProviderId,
                CustomerId = Guid.NewGuid(),
                Score = 5,
                Comment = "Excellent",
                CreatedAt = DateTime.UtcNow
            }
        };

        await _dbContext.Ratings.AddRangeAsync(ratings);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/ratings/{serviceProviderId}/average");
        var content = await response.Content.ReadFromJsonAsync<AverageRatingResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(content);
        Assert.Equal(serviceProviderId, content.ServiceProviderId);
        Assert.Equal(4.5, content.AverageScore);
        Assert.Equal(2, content.TotalRatings);
    }

    [Fact]
    public async Task GetAverageRating_WithNoRatings_ShouldReturnNotFound()
    {
        // Arrange
        var serviceProviderId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/ratings/{serviceProviderId}/average");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose()
    {
        _dbContext.Ratings.RemoveRange(_dbContext.Ratings);
        _dbContext.SaveChanges();
    }
}

public class CustomWebApplicationFactory : WebApplicationFactory<RatingService.Api.Program>, IAsyncLifetime
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
                d => d.ServiceType == typeof(DbContextOptions<RatingDbContext>));

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

            //Debugging Purposes
            //Console.WriteLine($"Using Testcontainers Connection String: {connectionString}");

            //  Register DbContext with the correct connection string
            services.AddDbContext<RatingDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RatingDbContext>();

            Console.WriteLine("Resetting and migrating test database...");
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            db.Database.Migrate();
        });
    }
}
