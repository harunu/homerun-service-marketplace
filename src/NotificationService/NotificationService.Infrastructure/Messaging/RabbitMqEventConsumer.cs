using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ServiceMarketplace.Shared.Contracts;
using System.Text;

namespace NotificationService.Infrastructure.Messaging
{
    /// <summary>
    /// RabbitMQ event consumer that listens for rating events and processes them asynchronously.
    /// Implements automatic reconnection and retry mechanisms for resilience.
    /// </summary>
    public class RabbitMqEventConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMqSettings _settings;
        private readonly ILogger<RabbitMqEventConsumer> _logger;
        private IConnection? _connection;
        private IChannel? _channel;
        private string? _consumerTag;

        public RabbitMqEventConsumer(IServiceProvider serviceProvider,
                             IOptions<RabbitMqSettings> settings,
                             ILogger<RabbitMqEventConsumer> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeRabbitMq();
        }

        /// <summary>
        /// Establishes a connection to RabbitMQ with exponential backoff retry logic.
        /// Ensures the exchange, queue, and binding are properly configured.
        /// </summary>
        private void InitializeRabbitMq()
        {
            int maxRetries = 10;
            int retryDelayMs = 5000;

            for (int retryCount = 0; retryCount < maxRetries; retryCount++)
            {
                try
                {
                    var factory = new ConnectionFactory
                    {
                        HostName = _settings.HostName,
                        Port = _settings.Port,
                        UserName = _settings.UserName,
                        Password = _settings.Password,
                        VirtualHost = _settings.VirtualHost,
                        AutomaticRecoveryEnabled = true,
                        NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                    };

                    _connection = factory.CreateConnectionAsync().Result;
                    _channel = _connection.CreateChannelAsync().Result;

                    // Declare RabbitMQ exchange and queue with necessary properties
                    _channel.ExchangeDeclareAsync(
                        exchange: _settings.ExchangeName,
                        type: "topic",
                        durable: true,
                        autoDelete: false,
                        arguments: new Dictionary<string, object?>()
                    ).Wait();

                    _channel.QueueDeclareAsync(
                        queue: _settings.QueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null
                    ).Wait();

                    _channel.QueueBindAsync(
                        queue: _settings.QueueName,
                        exchange: _settings.ExchangeName,
                        routingKey: _settings.RoutingKey
                    ).Wait();

                    // Configure message prefetch to optimize processing
                    _channel.BasicQosAsync(0, 1, false).Wait();

                    _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port} and initialized queue {Queue}",
                        _settings.HostName, _settings.Port, _settings.QueueName);

                    // Exit the retry loop on success
                    break;
                }
                catch (Exception ex)
                {
                    if (retryCount == maxRetries - 1)
                    {
                        _logger.LogError(ex, "Failed to connect to RabbitMQ at {Host}:{Port} after {RetryCount} attempts",
                            _settings.HostName, _settings.Port, maxRetries);
                    }
                    else
                    {
                        _logger.LogWarning(ex, "Failed to connect to RabbitMQ at {Host}:{Port}. Retry attempt {RetryCount} of {MaxRetries}. Retrying in {RetryDelay}ms",
                            _settings.HostName, _settings.Port, retryCount + 1, maxRetries, retryDelayMs);

                        // Dispose of any partial connections before retrying
                        _channel?.Dispose();
                        _connection?.Dispose();

                        Thread.Sleep(retryDelayMs);
                    }
                }
            }
        }

        /// <summary>
        /// Starts consuming messages from RabbitMQ asynchronously.
        /// Ensures the consumer is properly initialized before processing messages.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            if (_channel == null)
            {
                _logger.LogError("Channel is null, cannot start consuming messages. Retrying RabbitMQ connection...");
                InitializeRabbitMq();

                // If still null after retrying, exit
                if (_channel == null)
                {
                    _logger.LogError("Failed to establish RabbitMQ connection after retries. Exiting consumer.");
                    return;
                }
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (bc, ea) =>
            {
                try
                {
                    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                    _logger.LogInformation("Received message from queue {Queue}", _settings.QueueName);

                    var @event = JsonConvert.DeserializeObject<RatingCreatedEvent>(message);
                    if (@event != null)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var eventHandler = scope.ServiceProvider.GetRequiredService<NotificationService.Core.Events.RatingCreatedEventHandler>();
                        await eventHandler.HandleAsync(@event);
                    }

                    // Acknowledge successful processing of message
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing message");
                    await _channel.BasicRejectAsync(ea.DeliveryTag, false); // Discard malformed messages
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    await _channel.BasicRejectAsync(ea.DeliveryTag, true); // Requeue failed messages for retry
                }
            };

            _consumerTag = await _channel.BasicConsumeAsync(
                queue: _settings.QueueName,
                autoAck: false,
                consumer: consumer);
        }

        /// <summary>
        /// Gracefully stops the RabbitMQ consumer and releases resources.
        /// Cancels the consumer subscription and closes the channel/connection.
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(_consumerTag) && _channel != null)
            {
                await _channel.BasicCancelAsync(_consumerTag);
            }

            _channel?.Dispose();
            _connection?.Dispose();

            await base.StopAsync(cancellationToken);
        }
    }
}
