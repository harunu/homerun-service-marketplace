using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Api.Controllers;
using NotificationService.Core.Entities;
using NotificationService.Core.Interfaces;
using ServiceMarketplace.Shared.Contracts;


namespace NotificationService.Tests.Unit
{

    public class NotificationServiceTests
    {
        private readonly Mock<INotificationRepository> _mockRepository;
        private readonly Mock<ILogger<NotificationService.Core.Services.NotificationService>> _mockLogger;
        private readonly NotificationService.Core.Services.NotificationService _service;


        public NotificationServiceTests()
        {
            _mockRepository = new Mock<INotificationRepository>();
            _mockLogger = new Mock<ILogger<NotificationService.Core.Services.NotificationService>>();
            _service = new NotificationService.Core.Services.NotificationService(
            _mockRepository.Object,
            _mockLogger.Object
        );
        }

        [Fact]
        public async Task CreateNotificationAsync_ShouldCreateAndReturnId()
        {
            // Arrange
            var serviceProviderId = Guid.NewGuid();
            var message = "New rating received";
            var notificationId = Guid.NewGuid();

            var createdNotification = new Notification
            {
                Id = notificationId,
                ServiceProviderId = serviceProviderId,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _mockRepository
                .Setup(repo => repo.CreateAsync(It.IsAny<Notification>()))
                .ReturnsAsync(createdNotification);

            // Act
            var result = await _service.CreateNotificationAsync(serviceProviderId, message);

            // Assert
            Assert.Equal(notificationId, result);
            _mockRepository.Verify(repo => repo.CreateAsync(It.IsAny<Notification>()), Times.Once);
        }

        [Fact]
        public async Task GetNotificationsAsync_WithUnreadNotifications_ShouldReturnPaginatedResults()
        {
            // Arrange
            var serviceProviderId = Guid.NewGuid();
            int page = 1, pageSize = 2;

            var notifications = new List<Notification>
            {
                new() {
                    Id = Guid.NewGuid(),
                    ServiceProviderId = serviceProviderId,
                    Message = "Notification 1",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                    IsRead = false
                },
                new() {
                    Id = Guid.NewGuid(),
                    ServiceProviderId = serviceProviderId,
                    Message = "Notification 2",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                    IsRead = false
                }
            };

            // Mock the repository methods
            _mockRepository
                .Setup(repo => repo.GetUnreadCountByServiceProviderIdAsync(serviceProviderId))
                .ReturnsAsync(5);

            _mockRepository
                .Setup(repo => repo.GetUnreadByServiceProviderIdAsync(serviceProviderId, page, pageSize))
                .ReturnsAsync(notifications);

            _mockRepository
                .Setup(repo => repo.MarkAsReadAsync(It.IsAny<IEnumerable<Guid>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.GetNotificationsAsync(serviceProviderId, page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(2, result.Notifications.Count());
            Assert.Equal("Notification 1", result.Notifications.First().Message);
            Assert.Equal("Notification 2", result.Notifications.Last().Message);

            _mockRepository.Verify(repo => repo.GetUnreadCountByServiceProviderIdAsync(serviceProviderId), Times.Once);
            _mockRepository.Verify(repo => repo.GetUnreadByServiceProviderIdAsync(serviceProviderId, page, pageSize), Times.Once);
            _mockRepository.Verify(repo => repo.MarkAsReadAsync(It.IsAny<IEnumerable<Guid>>()), Times.Once);
        }

        [Fact]
        public async Task GetNotificationsAsync_WithNoUnreadNotifications_ShouldReturnEmptyList()
        {
            // Arrange
            var serviceProviderId = Guid.NewGuid();
            int page = 1, pageSize = 2;

            var mockService = new Mock<INotificationService>();

            // Setup the mock service 
            mockService
                .Setup(service => service.GetNotificationsAsync(serviceProviderId, page, pageSize))
                .ReturnsAsync(new NotificationsResponse { Notifications = new List<NotificationDto>() });

            var mockLogger = new Mock<ILogger<NotificationsController>>();
            var controller = new NotificationsController(mockService.Object, mockLogger.Object);

            // Act
            var result = await controller.GetNotifications(serviceProviderId.ToString(), page, pageSize);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var responseContent = Assert.IsType<NotificationsResponse>(okResult.Value);
            Assert.NotNull(responseContent);
            Assert.Empty(responseContent.Notifications);
        }


        [Fact]
        public async Task GetNotificationsAsync_WithoutPaginationParams_ShouldDefaultToPage1PageSize10()
        {
            // Arrange
            var serviceProviderId = Guid.NewGuid();
            var defaultPage = 1;
            var defaultPageSize = 10;

            var notifications = new List<Notification>
            {
                new Notification
                {
                    Id = Guid.NewGuid(),
                    ServiceProviderId = serviceProviderId,
                    Message = "Notification 1",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                    IsRead = false
                }
            };

            _mockRepository
                .Setup(repo => repo.GetUnreadCountByServiceProviderIdAsync(serviceProviderId))
                .ReturnsAsync(20);

            _mockRepository
                .Setup(repo => repo.GetUnreadByServiceProviderIdAsync(serviceProviderId, defaultPage, defaultPageSize))
                .ReturnsAsync(notifications);

            _mockRepository
                .Setup(repo => repo.MarkAsReadAsync(It.IsAny<IEnumerable<Guid>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.GetNotificationsAsync(serviceProviderId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Notifications);
            Assert.Equal(20, result.TotalCount);
            Assert.Equal(defaultPage, result.CurrentPage);
            Assert.Equal(defaultPageSize, result.PageSize);

            _mockRepository.Verify(repo => repo.GetUnreadCountByServiceProviderIdAsync(serviceProviderId), Times.Once);
            _mockRepository.Verify(repo => repo.GetUnreadByServiceProviderIdAsync(serviceProviderId, defaultPage, defaultPageSize), Times.Once);
            _mockRepository.Verify(repo => repo.MarkAsReadAsync(It.IsAny<IEnumerable<Guid>>()), Times.Once);
        }
    }

}