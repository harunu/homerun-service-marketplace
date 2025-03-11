namespace NotificationService.Infrastructure.Messaging
{
    /// <summary>
    /// Configuration settings for connecting to a RabbitMQ broker.
    /// </summary>
    public class RabbitMqSettings
    {
        /// <summary>
        /// RabbitMQ host address (default: localhost).
        /// </summary>
        public string HostName { get; set; } = "localhost";

        /// <summary>
        /// RabbitMQ connection port (default: 5672).
        /// </summary>
        public int Port { get; set; } = 5672;

        /// <summary>
        /// Username for authenticating with RabbitMQ (default: guest).
        /// </summary>
        public string UserName { get; set; } = "guest";

        /// <summary>
        /// Password for authenticating with RabbitMQ (default: guest).
        /// </summary>
        public string Password { get; set; } = "guest";

        /// <summary>
        /// Virtual host used within RabbitMQ (default: /).
        /// </summary>
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// Name of the exchange where messages will be published.
        /// </summary>
        public string ExchangeName { get; set; } = "ratings_exchange";

        /// <summary>
        /// Name of the queue where notification messages will be consumed.
        /// </summary>
        public string QueueName { get; set; } = "notifications_queue";

        /// <summary>
        /// Routing key used to bind messages to the queue.
        /// </summary>
        public string RoutingKey { get; set; } = "rating.created";
    }
}

