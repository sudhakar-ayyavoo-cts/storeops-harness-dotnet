using StoreOps.Domain.Staff;

namespace StoreOps.Api.Contracts.Staff;

public sealed class AuthTokenDto
{
    public string Token { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }

    public static AuthTokenDto FromDomain(AuthToken token) => new()
    {
        Token = token.Token,
        UserId = token.UserId,
        ExpiresAt = token.ExpiresAt,
    };
}
