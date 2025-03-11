using Microsoft.Extensions.Logging;
using NotificationService.Core.Interfaces;
using Polly;
using Polly.Retry;
using ServiceMarketplace.Shared.Contracts;

namespace NotificationService.Core.Events
{
    /// <summary>
    /// Handles events triggered when a new rating is created.
    /// Responsible for generating notifications for service providers.
    /// </summary>
    public class RatingCreatedEventHandler
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<RatingCreatedEventHandler> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        /// <summary>
        /// Initializes the event handler with notification service, logging, and a retry policy.
        /// </summary>
        /// <param name="notificationService">Service responsible for creating notifications.</param>
        /// <param name="logger">Logger instance for logging information and errors.</param>
        public RatingCreatedEventHandler(
            INotificationService notificationService,
            ILogger<RatingCreatedEventHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;

            // Retry policy: Retries up to 3 times with exponential backoff in case of failure
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry {RetryCount} for handling rating event due to {ExceptionMessage}",
                            retryCount, exception.Message);
                    });
        }

        /// <summary>
        /// Processes the rating created event and generates a notification.
        /// </summary>
        /// <param name="event">The rating event containing details of the new rating.</param>
        public async Task HandleAsync(RatingCreatedEvent @event)
        {
            try
            {
                _logger.LogInformation("Handling rating created event for service provider {ServiceProviderId}",
                    @event.ServiceProviderId);

                // Construct notification message
                var message = $"New rating received from customer {MaskCustomerId(@event.CustomerId)}. Score: {FormatRatingScore(@event.Score)}.";

                // Append comment if available, with truncation
                if (!string.IsNullOrEmpty(@event.Comment))
                {
                    message += $" Comment: {TruncateComment(@event.Comment)}";
                }

                // Execute notification creation with retry policy
                await _retryPolicy.ExecuteAsync(() =>
                    _notificationService.CreateNotificationAsync(@event.ServiceProviderId, message));

                _logger.LogInformation("Successfully created notification for service provider {ServiceProviderId}",
                    @event.ServiceProviderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling rating created event for service provider {ServiceProviderId}",
                    @event.ServiceProviderId);
                throw;
            }
        }

        /// <summary>
        /// Masks the customer ID for privacy by only displaying a portion of it.
        /// </summary>
        private string MaskCustomerId(Guid customerId)
        {
            return customerId.ToString().Substring(0, 8) + "...";
        }

        /// <summary>
        /// Formats the rating score in a readable format.
        /// </summary>
        private string FormatRatingScore(int score)
        {
            return $"{score}/5";
        }

        /// <summary>
        /// Truncates long comments to a maximum length.
        /// </summary>
        private string TruncateComment(string comment)
        {
            const int maxLength = 50;
            return comment.Length <= maxLength ? comment : comment.Substring(0, maxLength) + "...";
        }
    }
}
