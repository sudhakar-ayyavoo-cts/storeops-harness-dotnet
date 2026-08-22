using Microsoft.AspNetCore.Mvc;
using StoreOps.Api.Contracts.Staff;
using StoreOps.Application.Staff;

namespace StoreOps.Api.Controllers;

[ApiController]
[Route("api/staff")]
public sealed class StaffController : ControllerBase
{
    private readonly IStaffService _service;

    public StaffController(IStaffService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        var users = await _service.ListAsync(storeId, ct);
        return Ok(users.Select(UserDto.FromDomain).ToList());
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthTokenDto>> Login(
        [FromBody] LoginRequestDto dto,
        CancellationToken ct)
    {
        var request = new LoginRequest
        {
            Email = dto.Email,
            Password = dto.Password,
        };

        var token = await _service.LoginAsync(request, ct);
        return Ok(AuthTokenDto.FromDomain(token));
    }
}
