namespace ServiceMarketplace.Shared.Contracts
{
    /// <summary>
    /// Represents a rating given by a customer.
    /// </summary>
    public class CustomerRatingDto
    {
        /// <summary>
        /// Unique identifier for the rating.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The service provider that was rated.
        /// </summary>
        public Guid ServiceProviderId { get; set; }

        /// <summary>
        /// The customer who submitted the rating.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Rating score (e.g., on a scale of 1-5).
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Optional comment provided with the rating.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Timestamp when the rating was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents the response for paginated customer ratings.
    /// </summary>
    public class CustomerRatingsResponse
    {
        /// <summary>
        /// The total number of ratings given by the customer.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// The current page number in the paginated result.
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// The number of ratings per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// The list of ratings submitted by the customer.
        /// </summary>
        public List<CustomerRatingDto> Ratings { get; set; } = new();
    }

    /// <summary>
    /// Represents the average rating given by a customer.
    /// </summary>
    public class CustomerAverageRatingResponse
    {
        /// <summary>
        /// The customer whose average rating is being retrieved.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// The calculated average score based on all ratings submitted by the customer.
        /// </summary>
        public double AverageScore { get; set; }

        /// <summary>
        /// The total number of ratings given by the customer.
        /// </summary>
        public int TotalRatings { get; set; }
    }
}
