namespace StoreOps.Domain.Staff;

public sealed class UserProfile
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Department { get; set; }
}
