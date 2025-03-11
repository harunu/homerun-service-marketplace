using ServiceMarketplace.Shared.Contracts;

namespace RatingService.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for rating-related operations.
    /// </summary>
    public interface IRatingService
    {
        /// <summary>
        /// Creates a new rating for a service provider.
        /// </summary>
        /// <param name="request">The request containing rating details.</param>
        /// <returns>The created rating details.</returns>
        Task<RatingDto> CreateRatingAsync(CreateRatingRequest request);

        /// <summary>
        /// Retrieves the average rating for a given service provider.
        /// </summary>
        /// <param name="serviceProviderId">The unique identifier of the service provider.</param>
        /// <returns>The average rating and total number of ratings.</returns>
        Task<AverageRatingResponse> GetAverageRatingAsync(Guid serviceProviderId);

        /// <summary>
        /// Retrieves all ratings submitted by a specific customer with pagination.
        /// </summary>
        /// <param name="customerId">The unique identifier of the customer.</param>
        /// <param name="page">The page number for pagination.</param>
        /// <param name="pageSize">The number of records per page.</param>
        /// <returns>A paginated list of ratings submitted by the customer.</returns>
        Task<CustomerRatingsResponse> GetCustomerRatingsAsync(Guid customerId, int page, int pageSize);

        /// <summary>
        /// Retrieves the average rating given by a specific customer.
        /// </summary>
        /// <param name="customerId">The unique identifier of the customer.</param>
        /// <returns>The average rating given by the customer and total number of ratings.</returns>
        Task<CustomerAverageRatingResponse> GetCustomerAverageRatingAsync(Guid customerId);
    }
}
