using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RatingService.Core.Entities;
using RatingService.Core.Interfaces;
using ServiceMarketplace.Shared.Contracts;

namespace RatingService.Core.Services
{
    /// <summary>
    /// Handles rating-related operations such as creating ratings, retrieving averages, 
    /// fetching ratings for customers, and publishing rating events.
    /// </summary>
    public class RatingService : IRatingService
    {
        private readonly IRatingRepository _ratingRepository;
        private readonly IEventPublisher<RatingCreatedEvent> _eventPublisher;
        private readonly ILogger<RatingService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public RatingService(
            IRatingRepository ratingRepository,
            IEventPublisher<RatingCreatedEvent> eventPublisher,
            ILogger<RatingService> logger)
        {
            _ratingRepository = ratingRepository;
            _eventPublisher = eventPublisher;
            _logger = logger;

            // Configure retry policy with exponential backoff to handle transient failures (e.g., event publishing)
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        logger.LogWarning("Retry {RetryCount} for publishing rating event due to {ExceptionMessage}",
                            retryCount, exception.Message);
                    });
        }

        /// <summary>
        /// Creates a rating and publishes an event asynchronously.
        /// </summary>
        /// <param name="request">The request containing rating details.</param>
        /// <returns>A DTO representing the created rating.</returns>
        public async Task<RatingDto> CreateRatingAsync(CreateRatingRequest request)
        {
            _logger.LogInformation("Creating rating for service provider {ServiceProviderId} from customer {CustomerId}",
                request.ServiceProviderId, request.CustomerId);

            var rating = new Rating
            {
                Id = Guid.NewGuid(),
                ServiceProviderId = request.ServiceProviderId,
                CustomerId = request.CustomerId,
                Score = request.Score,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };

            // Persist the rating
            var createdRating = await _ratingRepository.CreateAsync(rating);

            var ratingCreatedEvent = new RatingCreatedEvent
            {
                Id = createdRating.Id,
                ServiceProviderId = createdRating.ServiceProviderId,
                CustomerId = createdRating.CustomerId,
                Score = createdRating.Score,
                Comment = createdRating.Comment,
                CreatedAt = createdRating.CreatedAt
            };

            // Publish the event with retry policy
            try
            {
                await _retryPolicy.ExecuteAsync(() => _eventPublisher.PublishAsync(ratingCreatedEvent));
                _logger.LogInformation("Rating event published successfully for rating {RatingId}", createdRating.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish rating event for rating {RatingId} after retries", createdRating.Id);
            }

            return new RatingDto
            {
                Id = createdRating.Id,
                ServiceProviderId = createdRating.ServiceProviderId,
                CustomerId = createdRating.CustomerId,
                Score = createdRating.Score,
                Comment = createdRating.Comment,
                CreatedAt = createdRating.CreatedAt
            };
        }

        /// <summary>
        /// Retrieves the average rating for a given service provider.
        /// </summary>
        /// <param name="serviceProviderId">The unique identifier of the service provider.</param>
        /// <returns>An object containing the average rating and total number of ratings.</returns>
        public async Task<AverageRatingResponse> GetAverageRatingAsync(Guid serviceProviderId)
        {
            _logger.LogInformation("Getting average rating for service provider {ServiceProviderId}", serviceProviderId);

            var averageRating = await _ratingRepository.GetAverageRatingAsync(serviceProviderId);
            var totalRatings = await _ratingRepository.GetTotalRatingsAsync(serviceProviderId);

            return new AverageRatingResponse
            {
                ServiceProviderId = serviceProviderId,
                AverageScore = averageRating,
                TotalRatings = totalRatings
            };
        }

        /// <summary>
        /// Retrieves paginated ratings submitted by a customer.
        /// </summary>
        /// <param name="customerId">The unique identifier of the customer.</param>
        /// <param name="page">The page number (default: 1).</param>
        /// <param name="pageSize">The number of items per page (default: 10).</param>
        /// <returns>A paginated list of customer ratings.</returns>
        public async Task<CustomerRatingsResponse> GetCustomerRatingsAsync(Guid customerId, int page = 1, int pageSize = 10)
        {
            _logger.LogInformation("Getting ratings from customer {CustomerId}, Page: {Page}, PageSize: {PageSize}", customerId, page, pageSize);

            // Validate pagination parameters
            if (page < 1 || pageSize < 1)
            {
                _logger.LogWarning("Invalid pagination parameters for CustomerId {CustomerId}", customerId);
                throw new ArgumentException("Page number and page size must be greater than zero.");
            }

            // Get total count of ratings (with retry policy)
            var totalCount = await _retryPolicy.ExecuteAsync(async () =>
                await _ratingRepository.GetTotalRatingsByCustomerIdAsync(customerId));

            if (totalCount == 0)
            {
                _logger.LogWarning("No ratings found from CustomerId: {CustomerId}", customerId);
                return new CustomerRatingsResponse
                {
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize,
                    Ratings = new List<CustomerRatingDto>()
                };
            }

            // Fetch paginated customer ratings
            var ratings = await _retryPolicy.ExecuteAsync(async () =>
                await _ratingRepository.GetRatingsByCustomerIdAsync(customerId, page, pageSize));

            var ratingDtos = ratings.Select(r => new CustomerRatingDto
            {
                Id = r.Id,
                ServiceProviderId = r.ServiceProviderId,
                CustomerId = r.CustomerId,
                Score = r.Score,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList();

            return new CustomerRatingsResponse
            {
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize,
                Ratings = ratingDtos
            };
        }

        /// <summary>
        /// Retrieves the average rating given by a specific customer.
        /// </summary>
        /// <param name="customerId">The unique identifier of the customer.</param>
        /// <returns>An object containing the average rating and total number of ratings given by the customer.</returns>
        public async Task<CustomerAverageRatingResponse> GetCustomerAverageRatingAsync(Guid customerId)
        {
            _logger.LogInformation("Getting average rating given by customer {CustomerId}", customerId);

            var averageRating = await _ratingRepository.GetCustomerAverageRatingAsync(customerId);
            var totalRatings = await _ratingRepository.GetTotalRatingsByCustomerIdAsync(customerId);

            return new CustomerAverageRatingResponse
            {
                CustomerId = customerId,
                AverageScore = averageRating,
                TotalRatings = totalRatings
            };
        }
    }
}
