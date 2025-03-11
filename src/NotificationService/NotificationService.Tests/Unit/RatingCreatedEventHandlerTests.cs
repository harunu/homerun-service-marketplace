using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Core.Events;
using NotificationService.Core.Interfaces;
using ServiceMarketplace.Shared.Contracts;

namespace NotificationService.Tests.Unit
{
    public class RatingCreatedEventHandlerTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<ILogger<RatingCreatedEventHandler>> _mockLogger;
        private readonly RatingCreatedEventHandler _handler;

        public RatingCreatedEventHandlerTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _mockLogger = new Mock<ILogger<RatingCreatedEventHandler>>();
            _handler = new RatingCreatedEventHandler(
                _mockNotificationService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task HandleAsync_ShouldCreateNotification()
        {
            // Arrange
            var serviceProviderId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var notificationId = Guid.NewGuid();

            var @event = new RatingCreatedEvent
            {
                Id = Guid.NewGuid(),
                ServiceProviderId = serviceProviderId,
                CustomerId = customerId,
                Score = 4,
                Comment = "Good service",
                CreatedAt = DateTime.UtcNow
            };

            _mockNotificationService
                .Setup(service => service.CreateNotificationAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>()))
                .ReturnsAsync(notificationId);

            // Act
            await _handler.HandleAsync(@event);

            // Assert
            _mockNotificationService.Verify(
                service => service.CreateNotificationAsync(
                    serviceProviderId,
                    It.Is<string>(s => s.Contains("New rating") && s.Contains("Score: 4/5"))),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithLongComment_ShouldTruncateComment()
        {
            // Arrange
            var serviceProviderId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var notificationId = Guid.NewGuid();

            var longComment = new string('A', 100); // Comment longer than 50 chars

            var @event = new RatingCreatedEvent
            {
                Id = Guid.NewGuid(),
                ServiceProviderId = serviceProviderId,
                CustomerId = customerId,
                Score = 5,
                Comment = longComment,
                CreatedAt = DateTime.UtcNow
            };

            _mockNotificationService
                .Setup(service => service.CreateNotificationAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>()))
                .ReturnsAsync(notificationId);

            // Act
            await _handler.HandleAsync(@event);

            // Assert
            _mockNotificationService.Verify(
                service => service.CreateNotificationAsync(
                    serviceProviderId,
                    It.Is<string>(s => s.Contains("...") && s.Length < longComment.Length + 50)),
                Times.Once);
        }
    }
}