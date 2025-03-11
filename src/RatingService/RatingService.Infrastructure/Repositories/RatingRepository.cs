using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RatingService.Core.Entities;
using RatingService.Core.Interfaces;
using RatingService.Infrastructure.Data;


namespace RatingService.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for handling rating-related database operations.
    /// </summary>
    public class RatingRepository : IRatingRepository
    {
        private readonly RatingDbContext _context;
        private readonly ILogger<RatingRepository> _logger;

        public RatingRepository(RatingDbContext context, ILogger<RatingRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Creates and saves a new rating in the database.
        /// </summary>
        /// <param name="rating">The rating entity to be created.</param>
        /// <returns>The created rating entity.</returns>
        public async Task<Rating> CreateAsync(Rating rating)
        {
            try
            {
                await _context.Ratings.AddAsync(rating);
                await _context.SaveChangesAsync();
                return rating;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating rating for service provider {ServiceProviderId}", rating.ServiceProviderId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all ratings for a specific service provider.
        /// </summary>
        /// <param name="serviceProviderId">The ID of the service provider.</param>
        /// <returns>List of ratings ordered by creation date (descending).</returns>
        public async Task<IEnumerable<Rating>> GetByServiceProviderIdAsync(Guid serviceProviderId)
        {
            return await _context.Ratings
                .Where(r => r.ServiceProviderId == serviceProviderId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Calculates the average rating score for a given service provider.
        /// </summary>
        /// <param name="serviceProviderId">The ID of the service provider.</param>
        /// <returns>The average rating score, or 0 if no ratings exist.</returns>
        public async Task<double> GetAverageRatingAsync(Guid serviceProviderId)
        {
            var ratings = await _context.Ratings
                .Where(r => r.ServiceProviderId == serviceProviderId)
                .ToListAsync();

            if (!ratings.Any())
                return 0;

            return ratings.Average(r => r.Score);
        }

        /// <summary>
        /// Retrieves the total number of ratings for a specific service provider.
        /// </summary>
        /// <param name="serviceProviderId">The ID of the service provider.</param>
        /// <returns>Total number of ratings.</returns>
        public async Task<int> GetTotalRatingsAsync(Guid serviceProviderId)
        {
            return await _context.Ratings
                .CountAsync(r => r.ServiceProviderId == serviceProviderId);
        }

        /// <summary>
        /// Retrieves all ratings submitted by a specific customer.
        /// </summary>
        /// <param name="customerId">The ID of the customer.</param>
        /// <param name="page">The page number for pagination.</param>
        /// <param name="pageSize">The number of records per page.</param>
        /// <returns>Paginated list of ratings submitted by the customer.</returns>
        public async Task<IEnumerable<Rating>> GetRatingsByCustomerIdAsync(Guid customerId, int page, int pageSize)
        {
            return await _context.Ratings
                .Where(r => r.CustomerId == customerId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves the total number of ratings submitted by a specific customer.
        /// </summary>
        /// <param name="customerId">The ID of the customer.</param>
        /// <returns>Total number of ratings submitted by the customer.</returns>
        public async Task<int> GetTotalRatingsByCustomerIdAsync(Guid customerId)
        {
            return await _context.Ratings
                .CountAsync(r => r.CustomerId == customerId);
        }

        /// <summary>
        /// Calculates the average rating score given by a specific customer.
        /// </summary>
        /// <param name="customerId">The ID of the customer.</param>
        /// <returns>The average rating score given by the customer, or 0 if no ratings exist.</returns>
        public async Task<double> GetCustomerAverageRatingAsync(Guid customerId)
        {
            var ratings = await _context.Ratings
                .Where(r => r.CustomerId == customerId)
                .ToListAsync();

            if (!ratings.Any())
                return 0;

            return ratings.Average(r => r.Score);
        }
    }
}
