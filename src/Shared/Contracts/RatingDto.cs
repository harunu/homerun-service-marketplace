namespace ServiceMarketplace.Shared.Contracts
{
    /// <summary>
    /// Represents a rating given to a service provider.
    /// </summary>
    public class RatingDto
    {
        /// <summary>
        /// Unique identifier for the rating.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The service provider being rated.
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
    /// Represents the request to create a new rating.
    /// </summary>
    public class CreateRatingRequest
    {
        /// <summary>
        /// The service provider being rated.
        /// </summary>
        public Guid ServiceProviderId { get; set; }

        /// <summary>
        /// The customer submitting the rating.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Rating score (must be within the allowed scale).
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Optional comment provided with the rating.
        /// </summary>
        public string? Comment { get; set; }
    }

    /// <summary>
    /// Represents the average rating details of a service provider.
    /// </summary>
    public class AverageRatingResponse
    {
        /// <summary>
        /// The service provider whose average rating is being retrieved.
        /// </summary>
        public Guid ServiceProviderId { get; set; }

        /// <summary>
        /// The calculated average score based on all ratings.
        /// </summary>
        public double AverageScore { get; set; }

        /// <summary>
        /// The total number of ratings received.
        /// </summary>
        public int TotalRatings { get; set; }
    }

    /// <summary>
    /// Represents an event that is triggered when a new rating is created.
    /// </summary>
    public class RatingCreatedEvent
    {
        /// <summary>
        /// Unique identifier for the rating event.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The service provider being rated.
        /// </summary>
        public Guid ServiceProviderId { get; set; }

        /// <summary>
        /// The customer who submitted the rating.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Rating score (e.g., 1-5).
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Optional comment associated with the rating.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Timestamp when the rating was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
