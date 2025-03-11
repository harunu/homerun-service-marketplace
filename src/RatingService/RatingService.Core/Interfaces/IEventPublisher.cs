namespace RatingService.Core.Interfaces
{
    /// <summary>
    /// Defines a contract for publishing events to an event bus or messaging system.
    /// </summary>
    /// <typeparam name="T">Type of event to be published.</typeparam>
    public interface IEventPublisher<T>
    {
        /// <summary>
        /// Publishes an event asynchronously.
        /// </summary>
        /// <param name="event">The event to be published.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PublishAsync(T @event);
    }
}
