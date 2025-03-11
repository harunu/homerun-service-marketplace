using NotificationService.Core.Entities;

namespace NotificationService.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for managing notifications in the data store.
    /// </summary>
    public interface INotificationRepository
    {
        /// <summary>
        /// Creates a new notification entry in the database.
        /// </summary>
        /// <param name="notification">The notification entity to be created.</param>
        /// <returns>The created notification entity.</returns>
        Task<Notification> CreateAsync(Notification notification);

        /// <summary>
        /// Retrieves a paginated list of unread notifications for a service provider.
        /// </summary>
        /// <param name="serviceProviderId">The ID of the service provider.</param>
        /// <param name="page">The current page number (1-based index).</param>
        /// <param name="pageSize">The number of notifications per page.</param>
        /// <returns>A list of unread notifications for the specified provider.</returns>
        Task<IEnumerable<Notification>> GetUnreadByServiceProviderIdAsync(Guid serviceProviderId, int page, int pageSize);

        /// <summary>
        /// Gets the total count of unread notifications for a service provider.
        /// </summary>
        /// <param name="serviceProviderId">The ID of the service provider.</param>
        /// <returns>The total number of unread notifications.</returns>
        Task<int> GetUnreadCountByServiceProviderIdAsync(Guid serviceProviderId);

        /// <summary>
        /// Marks a list of notifications as read.
        /// </summary>
        /// <param name="notificationIds">A collection of notification IDs to be marked as read.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task MarkAsReadAsync(IEnumerable<Guid> notificationIds);
    }
}
