using Microsoft.AspNetCore.Mvc;
using StoreOps.Api.Contracts.Reports;
using StoreOps.Application.Reports;

namespace StoreOps.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportsService _service;

    public ReportsController(IReportsService service) => _service = service;

    [HttpGet("store/{storeId:guid}")]
    public async Task<ActionResult<ReportDto>> GetStoreSummary(
        Guid storeId,
        CancellationToken ct)
    {
        var report = await _service.GetStoreSummaryAsync(storeId, ct);
        return Ok(ReportDto.FromDomain(report));
    }
}
