namespace NotificationService.Core.Entities
{
    /// <summary>
    /// Represents a notification for a service provider.
    /// </summary>
    public class Notification
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
        /// The message content of the notification.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp indicating when the notification was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Indicates whether the notification has been read.
        /// </summary>
        public bool IsRead { get; set; }
    }
}
