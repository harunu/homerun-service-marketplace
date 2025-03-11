using RatingService.Core.Entities;

namespace RatingService.Core.Interfaces
{
    /// <summary>
    /// Defines a contract for managing rating-related data operations.
    /// </summary>
    public interface IRatingRepository
    {
        /// <summary>
        /// Creates a new rating in the repository.
        /// </summary>
        /// <param name="rating">The rating entity to be created.</param>
        /// <returns>The created rating entity.</returns>
        Task<Rating> CreateAsync(Rating rating);

        /// <summary>
        /// Retrieves all ratings for a given service provider.
        /// </summary>
        /// <param name="serviceProviderId">The unique identifier of the service provider.</param>
        /// <returns>A collection of ratings associated with the service provider.</returns>
        Task<IEnumerable<Rating>> GetByServiceProviderIdAsync(Guid serviceProviderId);

        /// <summary>
        /// Calculates the average rating for a specific service provider.
        /// </summary>
        /// <param name="serviceProviderId">The unique identifier of the service provider.</param>
        /// <returns>The average rating score.</returns>
        Task<double> GetAverageRatingAsync(Guid serviceProviderId);

        /// <summary>
        /// Gets the total number of ratings for a service provider.
        /// </summary>
        /// <param name="serviceProviderId">The unique identifier of the service provider.</param>
        /// <returns>The total count of ratings.</returns>
        Task<int> GetTotalRatingsAsync(Guid serviceProviderId);

        /// <summary>
        /// Retrieves all ratings submitted by a specific customer with pagination.
        /// </summary>
        /// <param name="customerId">The unique identifier of the customer.</param>
        /// <param name="page">The page number for pagination.</param>
        /// <param name="pageSize">The number of records per page.</param>
        /// <returns>A paginated collection of ratings submitted by the customer.</returns>
        Task<IEnumerable<Rating>> GetRatingsByCustomerIdAsync(Guid customerId, int page, int pageSize);

        /// <summary>
        /// Gets the total number of ratings submitted by a specific customer.
        /// </summary>
        /// <param name="customerId">The unique identifier of the customer.</param>
        /// <returns>The total count of ratings given by the customer.</returns>
        Task<int> GetTotalRatingsByCustomerIdAsync(Guid customerId);

        /// <summary>
        /// Calculates the average rating given by a specific customer.
        /// </summary>
        /// <param name="customerId">The unique identifier of the customer.</param>
        /// <returns>The average rating score given by the customer.</returns>
        Task<double> GetCustomerAverageRatingAsync(Guid customerId);
    }
}
