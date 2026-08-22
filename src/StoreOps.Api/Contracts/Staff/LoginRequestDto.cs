namespace StoreOps.Api.Contracts.Staff;

public sealed class LoginRequestDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
