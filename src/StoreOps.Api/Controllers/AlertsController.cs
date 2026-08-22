using Microsoft.AspNetCore.Mvc;
using StoreOps.Api.Contracts.Alerts;
using StoreOps.Application.Alerts;

namespace StoreOps.Api.Controllers;

[ApiController]
[Route("api/alerts")]
public sealed class AlertsController : ControllerBase
{
    private readonly IAlertsService _service;

    public AlertsController(IAlertsService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> List(
        [FromQuery] Guid? userId,
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        var notifications = await _service.ListAsync(userId, storeId, ct);
        return Ok(notifications.Select(NotificationDto.FromDomain).ToList());
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<NotificationDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateAlertStatusRequestDto dto,
        CancellationToken ct)
    {
        var notification = await _service.UpdateStatusAsync(id, dto.Status, ct);
        return Ok(NotificationDto.FromDomain(notification));
    }
}
