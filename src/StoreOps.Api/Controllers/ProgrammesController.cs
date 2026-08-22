using Microsoft.AspNetCore.Mvc;
using StoreOps.Api.Contracts.Programmes;
using StoreOps.Application.Programmes;

namespace StoreOps.Api.Controllers;

[ApiController]
[Route("api/programmes")]
public sealed class ProgrammesController : ControllerBase
{
    private readonly IProgrammesService _service;

    public ProgrammesController(IProgrammesService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProgrammeDto>>> List(
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        var programmes = await _service.ListAsync(storeId, ct);
        return Ok(programmes.Select(ProgrammeDto.FromDomain).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<ProgrammeDto>> Create(
        [FromBody] CreateProgrammeRequestDto dto,
        CancellationToken ct)
    {
        var request = new CreateProgrammeRequest
        {
            Name = dto.Name,
            Description = dto.Description,
            StoreId = dto.StoreId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
        };

        var project = await _service.CreateAsync(request, ct);
        var result = ProgrammeDto.FromDomain(project);
        return CreatedAtAction(nameof(List), new { id = result.Id }, result);
    }
}
