namespace RatingService.Core.Entities
{
    /// <summary>
    /// Represents a rating given to a service provider.
    /// </summary>
    public class Rating
    {
        /// <summary>
        /// Unique identifier for the rating.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Identifier of the service provider being rated.
        /// </summary>
        public Guid ServiceProviderId { get; set; }

        /// <summary>
        /// Identifier of the customer providing the rating.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Rating score (typically on a scale, e.g., 1-5).
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Optional comment describing the rating.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Timestamp when the rating was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
