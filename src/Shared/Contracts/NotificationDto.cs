namespace ServiceMarketplace.Shared.Contracts
{
    /// <summary>
    /// Represents a notification entity in the system.
    /// </summary>
    public class NotificationDto
    {
        /// <summary>
        /// Unique identifier for the notification.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The service provider associated with this notification.
        /// </summary>
        public Guid ServiceProviderId { get; set; }

        /// <summary>
        /// Message content of the notification.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the notification was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Indicates whether the notification has been read.
        /// </summary>
        public bool IsRead { get; set; }
    }

    /// <summary>
    /// Represents a paginated response containing notifications.
    /// </summary>
    public class NotificationsResponse
    {
        /// <summary>
        /// Total number of unread notifications available.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// The current page number in the paginated result.
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// The number of notifications per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// The list of notifications for the current page.
        /// </summary>
        public IEnumerable<NotificationDto> Notifications { get; set; } = new List<NotificationDto>();
    }
}
