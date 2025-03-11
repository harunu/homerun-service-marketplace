using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RatingService.Api.Controllers;
using RatingService.Core.Entities;
using RatingService.Core.Interfaces;
using ServiceMarketplace.Shared.Contracts;


namespace RatingService.Tests.Unit
{
    public class RatingServiceTests
    {
        private readonly Mock<IRatingRepository> _mockRepository;
        private readonly Mock<IEventPublisher<RatingCreatedEvent>> _mockEventPublisher;
        private readonly Mock<ILogger<Core.Services.RatingService>> _mockLogger;
        private readonly Core.Services.RatingService _service;

        public RatingServiceTests()
        {
            _mockRepository = new Mock<IRatingRepository>();
            _mockEventPublisher = new Mock<IEventPublisher<RatingCreatedEvent>>();
            _mockLogger = new Mock<ILogger<Core.Services.RatingService>>();
            _service = new Core.Services.RatingService(
                _mockRepository.Object,
                _mockEventPublisher.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task CreateRatingAsync_ShouldCreateRatingAndPublishEvent()
        {
            // Arrange
            var serviceProviderId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var request = new CreateRatingRequest
            {
                ServiceProviderId = serviceProviderId,
                CustomerId = customerId,
                Score = 5,
                Comment = "Great service!"
            };

            var createdRating = new Rating
            {
                Id = Guid.NewGuid(),
                ServiceProviderId = serviceProviderId,
                CustomerId = customerId,
                Score = 5,
                Comment = "Great service!",
                CreatedAt = DateTime.UtcNow
            };

            _mockRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Rating>()))
                .ReturnsAsync(createdRating);

            _mockEventPublisher
                .Setup(publisher => publisher.PublishAsync(It.IsAny<RatingCreatedEvent>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateRatingAsync(request);

            // Assert
            Assert.Equal(createdRating.Id, result.Id);
            Assert.Equal(serviceProviderId, result.ServiceProviderId);
            Assert.Equal(customerId, result.CustomerId);
            Assert.Equal(5, result.Score);
            Assert.Equal("Great service!", result.Comment);

            _mockRepository.Verify(repo => repo.CreateAsync(It.IsAny<Rating>()), Times.Once);
            _mockEventPublisher.Verify(publisher => publisher.PublishAsync(It.IsAny<RatingCreatedEvent>()), Times.Once);
        }

        [Fact]
        public async Task GetAverageRatingAsync_ShouldReturnCorrectAverageRating()
        {
            // Arrange
            var serviceProviderId = Guid.NewGuid();
            var expectedAverage = 4.5;
            var expectedTotal = 10;

            _mockRepository
                .Setup(repo => repo.GetAverageRatingAsync(serviceProviderId))
                .ReturnsAsync(expectedAverage);

            _mockRepository
                .Setup(repo => repo.GetTotalRatingsAsync(serviceProviderId))
                .ReturnsAsync(expectedTotal);

            // Act
            var result = await _service.GetAverageRatingAsync(serviceProviderId);

            // Assert
            Assert.Equal(serviceProviderId, result.ServiceProviderId);
            Assert.Equal(expectedAverage, result.AverageScore);
            Assert.Equal(expectedTotal, result.TotalRatings);

            _mockRepository.Verify(repo => repo.GetAverageRatingAsync(serviceProviderId), Times.Once);
            _mockRepository.Verify(repo => repo.GetTotalRatingsAsync(serviceProviderId), Times.Once);
        }

        [Fact]
        public async Task GetCustomerRatingsAsync_ShouldReturnPaginatedCustomerRatings()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var page = 1;
            var pageSize = 2;
            var expectedTotal = 3;

            var ratings = new List<Rating>
            {
                new Rating { Id = Guid.NewGuid(), ServiceProviderId = Guid.NewGuid(), CustomerId = customerId, Score = 5, Comment = "Excellent", CreatedAt = DateTime.UtcNow },
                new Rating { Id = Guid.NewGuid(), ServiceProviderId = Guid.NewGuid(), CustomerId = customerId, Score = 4, Comment = "Good", CreatedAt = DateTime.UtcNow }
            };

            _mockRepository
                .Setup(repo => repo.GetTotalRatingsByCustomerIdAsync(customerId))
                .ReturnsAsync(expectedTotal);

            _mockRepository
                .Setup(repo => repo.GetRatingsByCustomerIdAsync(customerId, page, pageSize))
                .ReturnsAsync(ratings);

            // Act
            var result = await _service.GetCustomerRatingsAsync(customerId, page, pageSize);

            // Assert
            Assert.Equal(expectedTotal, result.TotalCount);
            Assert.Equal(page, result.CurrentPage);
            Assert.Equal(pageSize, result.PageSize);
            Assert.Equal(ratings.Count, result.Ratings.Count);

            _mockRepository.Verify(repo => repo.GetRatingsByCustomerIdAsync(customerId, page, pageSize), Times.Once);
            _mockRepository.Verify(repo => repo.GetTotalRatingsByCustomerIdAsync(customerId), Times.Once);
        }

        [Fact]
        public async Task GetCustomerAverageRatingAsync_ShouldReturnCorrectAverage()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var expectedAverage = 4.2;
            var expectedTotal = 15;

            _mockRepository
                .Setup(repo => repo.GetCustomerAverageRatingAsync(customerId))
                .ReturnsAsync(expectedAverage);

            _mockRepository
                .Setup(repo => repo.GetTotalRatingsByCustomerIdAsync(customerId))
                .ReturnsAsync(expectedTotal);

            // Act
            var result = await _service.GetCustomerAverageRatingAsync(customerId);

            // Assert
            Assert.Equal(customerId, result.CustomerId);
            Assert.Equal(expectedAverage, result.AverageScore);
            Assert.Equal(expectedTotal, result.TotalRatings);

            _mockRepository.Verify(repo => repo.GetCustomerAverageRatingAsync(customerId), Times.Once);
            _mockRepository.Verify(repo => repo.GetTotalRatingsByCustomerIdAsync(customerId), Times.Once);
        }

        [Fact]
        public async Task GetCustomerRatingsAsync_ShouldHandleEmptyRatings()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var page = 1;
            var pageSize = 10;

            _mockRepository
                .Setup(repo => repo.GetTotalRatingsByCustomerIdAsync(customerId))
                .ReturnsAsync(0);

            // Act
            var result = await _service.GetCustomerRatingsAsync(customerId, page, pageSize);

            // Assert
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Ratings);

            _mockRepository.Verify(repo => repo.GetTotalRatingsByCustomerIdAsync(customerId), Times.Once);
            _mockRepository.Verify(repo => repo.GetRatingsByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }


        [Fact]
        public async Task GetCustomerRatingsAsync_ShouldThrowException_WhenInvalidInput()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var page = -1;
            var pageSize = 10;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetCustomerRatingsAsync(customerId, page, pageSize));
        }

        [Fact]
        public async Task GetServiceProviderRatingsAsync_ShouldThrowException_WhenInvalidInput()
        {
            // Arrange
            var serviceProviderId = Guid.NewGuid();
            var page = -1;
            var pageSize = 10;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetCustomerRatingsAsync(serviceProviderId, page, pageSize));
        }

        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000000", "f47ac10b-58cc-4372-a567-0e02b2c3d479")] // Invalid ServiceProviderId
        [InlineData("f47ac10b-58cc-4372-a567-0e02b2c3d479", "00000000-0000-0000-0000-000000000000")] // Invalid CustomerId
        [InlineData("00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000")] // Both Invalid
        public async Task CreateRatingAsync_ShouldReturnBadRequest_WhenInvalidGuidsAreProvided(string serviceProviderIdStr, string customerIdStr)
        {
            // Arrange
            var serviceProviderId = Guid.Parse(serviceProviderIdStr);
            var customerId = Guid.Parse(customerIdStr);

            var request = new CreateRatingRequest
            {
                ServiceProviderId = serviceProviderId,
                CustomerId = customerId,
                Score = 5,
                Comment = "Great service!"
            };

            var mockRatingService = new Mock<IRatingService>();
            var mockLogger = new Mock<ILogger<RatingsController>>();
            var controller = new RatingsController(mockRatingService.Object, mockLogger.Object);

            // Act
            var result = await controller.CreateRating(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.NotNull(badRequestResult.Value);
            var valueObject = badRequestResult.Value!;
            var message = valueObject.ToString()!;

            Assert.Contains("GUID", message, StringComparison.OrdinalIgnoreCase);
        }

    }
}
