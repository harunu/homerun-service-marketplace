using Microsoft.Extensions.Logging;
using NotificationService.Core.Entities;
using NotificationService.Core.Interfaces;
using Polly;
using Polly.Retry;
using ServiceMarketplace.Shared.Contracts;

namespace NotificationService.Core.Services
{
    /// <summary>
    /// Service responsible for handling notifications, including creation, retrieval, and marking notifications as read.
    /// Implements retry policies for transient failures.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<NotificationService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public NotificationService(
            INotificationRepository notificationRepository,
            ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _logger = logger;

            // Configure retry policy with exponential backoff to handle transient failures (e.g., database connection issues)
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry {RetryCount} for database operation due to {ExceptionMessage}",
                            retryCount, exception.Message);
                    });
        }

        /// <summary>
        /// Creates a new notification for a service provider.
        /// </summary>
        /// <param name="serviceProviderId">The unique identifier of the service provider.</param>
        /// <param name="message">The notification message.</param>
        /// <returns>The ID of the created notification.</returns>
        public async Task<Guid> CreateNotificationAsync(Guid serviceProviderId, string message)
        {
            _logger.LogInformation("Creating notification for service provider {ServiceProviderId}", serviceProviderId);

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                ServiceProviderId = serviceProviderId,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            // Execute notification creation with retry policy
            var createdNotification = await _retryPolicy.ExecuteAsync(async () =>
                await _notificationRepository.CreateAsync(notification));

            return createdNotification.Id;
        }

        /// <summary>
        /// Retrieves paginated unread notifications for a given service provider.
        /// Marks retrieved notifications as read.
        /// </summary>
        /// <param name="serviceProviderId">The unique identifier of the service provider.</param>
        /// <param name="page">The page number (default: 1).</param>
        /// <param name="pageSize">The number of items per page (default: 10).</param>
        /// <returns>A paginated list of notifications.</returns>
        public async Task<NotificationsResponse> GetNotificationsAsync(Guid serviceProviderId, int page = 1, int pageSize = 10)
        {
            _logger.LogInformation("Getting notifications for ServiceProviderId: {ServiceProviderId}, Page: {Page}, PageSize: {PageSize}",
                serviceProviderId, page, pageSize);

            // Get unread notification count (with retry policy)
            var totalCount = await _retryPolicy.ExecuteAsync(async () =>
                await _notificationRepository.GetUnreadCountByServiceProviderIdAsync(serviceProviderId));

            if (totalCount == 0)
            {
                _logger.LogWarning("No unread notifications found for ServiceProviderId: {ServiceProviderId}", serviceProviderId);
            }

            // Fetch paginated unread notifications
            var notifications = await _retryPolicy.ExecuteAsync(async () =>
                await _notificationRepository.GetUnreadByServiceProviderIdAsync(serviceProviderId, page, pageSize));

            // Convert to DTOs for response
            var notificationDtos = notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                ServiceProviderId = n.ServiceProviderId,
                Message = n.Message,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead
            }).ToList();

            // Mark retrieved notifications as read
            await _retryPolicy.ExecuteAsync(async () =>
                await _notificationRepository.MarkAsReadAsync(notifications.Select(n => n.Id)));

            _logger.LogInformation("Marked {Count} notifications as read for ServiceProviderId: {ServiceProviderId}",
                notifications.Count(), serviceProviderId);

            return new NotificationsResponse
            {
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize,
                Notifications = notificationDtos
            };
        }
    }
}
