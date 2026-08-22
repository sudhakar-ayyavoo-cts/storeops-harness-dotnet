namespace StoreOps.Domain.Staff;

public sealed class User
{
    public Guid Id { get; init; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserProfile Profile { get; set; } = new();
    public StaffRole Role { get; set; }
    public Guid StoreId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
