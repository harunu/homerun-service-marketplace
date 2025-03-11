using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService.Core.Entities;
using NotificationService.Core.Interfaces;
using NotificationService.Infrastructure.Data;

namespace NotificationService.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for managing notification persistence in the database.
    /// Provides methods for creating, retrieving, and updating notification records.
    /// </summary>
    public class NotificationRepository : INotificationRepository
    {
        private readonly NotificationDbContext _context;
        private readonly ILogger<NotificationRepository> _logger;

        public NotificationRepository(NotificationDbContext context, ILogger<NotificationRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new notification record in the database.
        /// </summary>
        /// <param name="notification">The notification entity to be created.</param>
        /// <returns>The created notification entity.</returns>
        public async Task<Notification> CreateAsync(Notification notification)
        {
            try
            {
                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync();
                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for service provider {ServiceProviderId}",
                    notification.ServiceProviderId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves the count of unread notifications for a specific service provider.
        /// </summary>
        /// <param name="serviceProviderId">The unique identifier of the service provider.</param>
        /// <returns>The count of unread notifications.</returns>
        public async Task<int> GetUnreadCountByServiceProviderIdAsync(Guid serviceProviderId)
        {
            try
            {
                return await _context.Notifications
                    .Where(n => n.ServiceProviderId == serviceProviderId && !n.IsRead)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unread notifications count for ServiceProviderId: {ServiceProviderId}", serviceProviderId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a paginated list of unread notifications for a service provider.
        /// </summary>
        /// <param name="serviceProviderId">The unique identifier of the service provider.</param>
        /// <param name="page">The page number (default: 1).</param>
        /// <param name="pageSize">The number of notifications per page (default: 10).</param>
        /// <returns>A paginated list of unread notifications.</returns>
        public async Task<IEnumerable<Notification>> GetUnreadByServiceProviderIdAsync(Guid serviceProviderId, int page = 1, int pageSize = 10)
        {
            try
            {
                return await _context.Notifications
                    .Where(n => n.ServiceProviderId == serviceProviderId && !n.IsRead)
                    .OrderByDescending(n => n.CreatedAt) // Newest notifications first
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unread notifications for ServiceProviderId: {ServiceProviderId}", serviceProviderId);
                throw;
            }
        }

        /// <summary>
        /// Marks a collection of notifications as read in the database.
        /// </summary>
        /// <param name="notificationIds">A collection of notification IDs to be marked as read.</param>
        public async Task MarkAsReadAsync(IEnumerable<Guid> notificationIds)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => notificationIds.Contains(n.Id))
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notifications as read");
                throw;
            }
        }
    }
}
