using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RatingService.Core.Interfaces;
using System.Text;

namespace RatingService.Infrastructure.Messaging
{
    /// <summary>
    /// Configuration settings for RabbitMQ connection and messaging.
    /// </summary>
    public class RabbitMqSettings
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        public string ExchangeName { get; set; } = "ratings_exchange";
        public string RoutingKey { get; set; } = "rating.created";
    }

    /// <summary>
    /// Generic event publisher for RabbitMQ, ensuring reliable messaging.
    /// </summary>
    /// <typeparam name="T">The type of event being published.</typeparam>
    public class RabbitMqEventPublisher<T> : IEventPublisher<T>, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly RabbitMqSettings _settings;
        private readonly ILogger<RabbitMqEventPublisher<T>> _logger;
        private bool _disposed;

        public RabbitMqEventPublisher(IOptions<RabbitMqSettings> settings, ILogger<RabbitMqEventPublisher<T>> logger)
        {
            _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings)); ;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost
            };

            try
            {
                // Establish RabbitMQ connection and channel
                _connection = factory.CreateConnectionAsync().Result;
                _channel = _connection.CreateChannelAsync().Result;

                // Declare exchange for event routing
                _channel.ExchangeDeclareAsync(
                    exchange: _settings.ExchangeName,
                    type: "topic",
                    durable: true,
                    autoDelete: false,
                    arguments: new Dictionary<string, object?>()
                ).Wait();

                _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}", _settings.HostName, _settings.Port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to RabbitMQ at {Host}:{Port}", _settings.HostName, _settings.Port);
                throw;
            }
        }

        /// <summary>
        /// Publishes an event to the configured RabbitMQ exchange.
        /// </summary>
        /// <param name="event">The event to be published.</param>
        public async Task PublishAsync(T @event)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RabbitMqEventPublisher<T>));

            try
            {
                var message = JsonConvert.SerializeObject(@event);
                var body = Encoding.UTF8.GetBytes(message);
                ReadOnlyMemory<byte> messageBody = new ReadOnlyMemory<byte>(body);

                var properties = new BasicProperties
                {
                    Persistent = true, // Ensures messages are durable
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                };

                await _channel.BasicPublishAsync(
                    exchange: _settings.ExchangeName,
                    routingKey: _settings.RoutingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: messageBody
                );

                _logger.LogInformation("Published event of type {EventType} to exchange {Exchange} with routing key {RoutingKey}",
                    typeof(T).Name, _settings.ExchangeName, _settings.RoutingKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish event of type {EventType}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Disposes the RabbitMQ connection and channel.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            (_channel as IChannel)?.Dispose();
            _connection?.Dispose();

            _disposed = true;
        }
    }
}
