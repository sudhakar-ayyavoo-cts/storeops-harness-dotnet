using StoreOps.Domain.Staff;

namespace StoreOps.Api.Contracts.Staff;

public sealed class UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Department { get; init; }
    public StaffRole Role { get; init; }
    public Guid StoreId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public static UserDto FromDomain(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.Profile.FirstName,
        LastName = user.Profile.LastName,
        Department = user.Profile.Department,
        Role = user.Role,
        StoreId = user.StoreId,
        CreatedAt = user.CreatedAt,
    };
}
