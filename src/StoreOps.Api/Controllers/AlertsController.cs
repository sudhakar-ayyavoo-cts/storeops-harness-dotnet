using Microsoft.AspNetCore.Mvc;
using StoreOps.Api.Contracts.Alerts;
using StoreOps.Application.Alerts;

namespace StoreOps.Api.Controllers;

[ApiController]
[Route("api/alerts")]
public sealed class AlertsController : ControllerBase
{
    private readonly IAlertsService _service;
    private readonly IAlertsEscalationSweepService _escalationSweepService;

    public AlertsController(IAlertsService service, IAlertsEscalationSweepService escalationSweepService)
    {
        _service = service;
        _escalationSweepService = escalationSweepService;
    }

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

    [HttpPost("sla-escalation-sweep")]
    public async Task<ActionResult<SlaEscalationSweepResultDto>> SlaEscalationSweep(CancellationToken ct)
    {
        var escalationsCreated = await _escalationSweepService.SweepAsync(ct);
        return Ok(new SlaEscalationSweepResultDto { EscalationsCreated = escalationsCreated });
    }
}
