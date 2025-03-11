using Microsoft.AspNetCore.Mvc;
using NotificationService.Core.Interfaces;
using ServiceMarketplace.Shared.Contracts;

namespace NotificationService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves paginated notifications for a given service provider.
        /// </summary>
        /// <param name="serviceProviderId">The unique identifier of the service provider.</param>
        /// <param name="page">The page number (default: 1).</param>
        /// <param name="pageSize">The number of items per page (default: 10).</param>
        /// <returns>A list of notifications or an appropriate error response.</returns>
        [HttpGet("service-provider/{serviceProviderId}")]
        [ProducesResponseType(typeof(NotificationsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetNotifications(string serviceProviderId, int page = 1, int pageSize = 10)
        {
            _logger.LogInformation("Received request to get notifications for service provider {ServiceProviderId}, Page: {Page}, PageSize: {PageSize}",
                serviceProviderId, page, pageSize);

            // Validate serviceProviderId as a valid GUID
            if (!Guid.TryParse(serviceProviderId, out var validGuid))
            {
                _logger.LogWarning("Invalid GUID received: {ServiceProviderId}", serviceProviderId);
                return BadRequest(new { Message = "Invalid service provider ID format." });
            }

            // Ensure pagination parameters are valid
            if (page < 1 || pageSize < 1)
            {
                _logger.LogWarning("Invalid pagination parameters: Page {Page}, PageSize {PageSize}", page, pageSize);
                return BadRequest(new { Message = "Page number and page size must be greater than zero." });
            }

            try
            {
                var notifications = await _notificationService.GetNotificationsAsync(validGuid, page, pageSize);

                // Return an empty list instead of 404 when no notifications are found
                if (!notifications.Notifications.Any())
                {
                    _logger.LogInformation("No notifications found for service provider ID: {ServiceProviderId}", validGuid);
                    return Ok(new NotificationsResponse { Notifications = new List<NotificationDto>() });
                }

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving notifications for service provider ID: {ServiceProviderId}", validGuid);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An unexpected error occurred. Please try again later." });
            }
        }
    }
}
