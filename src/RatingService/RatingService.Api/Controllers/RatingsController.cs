using Microsoft.AspNetCore.Mvc;
using RatingService.Core.Interfaces;
using ServiceMarketplace.Shared.Contracts;

namespace RatingService.Api.Controllers
{
    /// <summary>
    /// Manages ratings for service providers and customers.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RatingsController : ControllerBase
    {
        private readonly IRatingService _ratingService;
        private readonly ILogger<RatingsController> _logger;

        /// <summary>
        /// Initializes the RatingsController.
        /// </summary>
        /// <param name="ratingService">Service for handling rating operations.</param>
        /// <param name="logger">Logger instance for capturing system events.</param>
        public RatingsController(IRatingService ratingService, ILogger<RatingsController> logger)
        {
            _ratingService = ratingService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new rating for a service provider.
        /// </summary>
        /// <param name="request">Rating request details.</param>
        /// <returns>Returns the created rating or validation errors.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(RatingDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRating([FromBody] CreateRatingRequest request)
        {
            if (request == null)
            {
                _logger.LogWarning("Received null rating request.");
                throw new ArgumentNullException(nameof(request), "Request cannot be null.");
            }

            _logger.LogInformation("Creating rating: ServiceProviderId={ServiceProviderId}, CustomerId={CustomerId}",
                request.ServiceProviderId, request.CustomerId);

            if (request.ServiceProviderId == Guid.Empty || request.CustomerId == Guid.Empty)
            {
                _logger.LogWarning("Invalid GUIDs provided: ServiceProviderId={ServiceProviderId}, CustomerId={CustomerId}",
                    request.ServiceProviderId, request.CustomerId);
                return BadRequest(new { Message = "ServiceProviderId and CustomerId cannot be empty GUIDs." });
            }

            if (request.Score < 1 || request.Score > 5)
            {
                _logger.LogWarning("Invalid rating score: {Score} for ServiceProviderId={ServiceProviderId}",
                    request.Score, request.ServiceProviderId);
                return BadRequest(new { Message = "Rating score must be between 1 and 5." });
            }

            if (!string.IsNullOrWhiteSpace(request.Comment) && request.Comment.Length > 500)
            {
                _logger.LogWarning("Comment exceeds max length for ServiceProviderId={ServiceProviderId}", request.ServiceProviderId);
                return BadRequest(new { Message = "Comment must not exceed 500 characters." });
            }

            var rating = await _ratingService.CreateRatingAsync(request);

            return CreatedAtAction(nameof(GetAverageRating), new { serviceProviderId = rating.ServiceProviderId }, rating);
        }

        /// <summary>
        /// Retrieves the average rating for a service provider.
        /// </summary>
        /// <param name="serviceProviderId">Service provider ID.</param>
        /// <returns>Returns the average rating or an error response.</returns>
        [HttpGet("{serviceProviderId}/average")]
        [ProducesResponseType(typeof(AverageRatingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAverageRating(string serviceProviderId)
        {
            _logger.LogInformation("Fetching average rating for ServiceProviderId={ServiceProviderId}", serviceProviderId);

            if (!Guid.TryParse(serviceProviderId, out var validGuid))
            {
                _logger.LogWarning("Invalid GUID format: {ServiceProviderId}", serviceProviderId);
                return BadRequest(new { Message = "Invalid service provider ID format." });
            }

            var averageRating = await _ratingService.GetAverageRatingAsync(validGuid);

            if (averageRating.TotalRatings == 0)
            {
                _logger.LogWarning("No ratings found for ServiceProviderId={ServiceProviderId}", validGuid);
                return NotFound(new { Message = $"No ratings found for service provider {serviceProviderId}." });
            }

            return Ok(averageRating);
        }

        /// <summary>
        /// Retrieves paginated ratings for a customer.
        /// </summary>
        /// <param name="customerId">Customer ID.</param>
        /// <param name="page">Page number.</param>
        /// <param name="pageSize">Number of records per page.</param>
        /// <returns>Returns customer ratings or an error response.</returns>
        [HttpGet("customer/{customerId}")]
        [ProducesResponseType(typeof(CustomerRatingsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCustomerRatings(string customerId, int page = 1, int pageSize = 10)
        {
            _logger.LogInformation("Fetching ratings for CustomerId={CustomerId}, Page={Page}, PageSize={PageSize}",
                customerId, page, pageSize);

            if (!Guid.TryParse(customerId, out var validGuid))
            {
                _logger.LogWarning("Invalid CustomerId format: {CustomerId}", customerId);
                return BadRequest(new { Message = "Invalid customer ID format." });
            }

            if (page < 1 || pageSize < 1)
            {
                _logger.LogWarning("Invalid pagination: Page={Page}, PageSize={PageSize}", page, pageSize);
                return BadRequest(new { Message = "Page number and page size must be greater than zero." });
            }

            var customerRatings = await _ratingService.GetCustomerRatingsAsync(validGuid, page, pageSize);

            return Ok(customerRatings);
        }

        /// <summary>
        /// Retrieves the average rating for a customer.
        /// </summary>
        /// <param name="customerId">Customer ID.</param>
        /// <returns>Returns the average rating or an error response.</returns>
        [HttpGet("customer/{customerId}/average")]
        [ProducesResponseType(typeof(AverageRatingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCustomerAverageRating(string customerId)
        {
            _logger.LogInformation("Fetching average rating for CustomerId={CustomerId}", customerId);

            if (!Guid.TryParse(customerId, out var validGuid))
            {
                _logger.LogWarning("Invalid CustomerId format: {CustomerId}", customerId);
                return BadRequest(new { Message = "Invalid customer ID format." });
            }

            var averageRating = await _ratingService.GetCustomerAverageRatingAsync(validGuid);

            if (averageRating.TotalRatings == 0)
            {
                _logger.LogWarning("No ratings found for CustomerId={CustomerId}", validGuid);
                return NotFound(new { Message = $"No ratings found for customer {customerId}." });
            }

            return Ok(averageRating);
        }
    }
}
