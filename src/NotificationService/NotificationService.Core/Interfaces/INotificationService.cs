using ServiceMarketplace.Shared.Contracts;

namespace NotificationService.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for notification-related operations.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Creates a new notification for a service provider.
        /// </summary>
        /// <param name="serviceProviderId">The ID of the service provider.</param>
        /// <param name="message">The notification message.</param>
        /// <returns>The ID of the created notification.</returns>
        Task<Guid> CreateNotificationAsync(Guid serviceProviderId, string message);

        /// <summary>
        /// Retrieves a paginated list of unread notifications for a service provider.
        /// </summary>
        /// <param name="serviceProviderId">The ID of the service provider.</param>
        /// <param name="page">The page number (default is 1).</param>
        /// <param name="pageSize">The number of notifications per page (default is 10).</param>
        /// <returns>A paginated response containing unread notifications.</returns>
        Task<NotificationsResponse> GetNotificationsAsync(Guid serviceProviderId, int page = 1, int pageSize = 10);
    }
}
