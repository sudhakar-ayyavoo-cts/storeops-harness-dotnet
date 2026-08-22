using Microsoft.AspNetCore.Mvc;
using StoreOps.Api.Contracts.Activities;
using StoreOps.Application.Activities;
using StoreOps.Domain.Activities;
using DomainTaskStatus = StoreOps.Domain.Activities.TaskStatus;

namespace StoreOps.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public sealed class ActivitiesController : ControllerBase
{
    private readonly IActivitiesService _service;

    public ActivitiesController(IActivitiesService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> List(
        [FromQuery] DomainTaskStatus? status,
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        var tasks = await _service.ListAsync(status, storeId, ct);
        return Ok(tasks.Select(TaskDto.FromDomain).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create(
        [FromBody] CreateTaskRequestDto dto,
        CancellationToken ct)
    {
        var request = new CreateTaskRequest
        {
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            Category = dto.Category,
            StoreId = dto.StoreId,
            AssignedToUserId = dto.AssignedToUserId,
            DueDate = dto.DueDate,
        };

        var task = await _service.CreateAsync(request, ct);
        var result = TaskDto.FromDomain(task);
        return CreatedAtAction(nameof(List), new { id = result.Id }, result);
    }
}
