using StoreOps.Domain.Staff;

namespace StoreOps.Application.Staff;

public interface IStaffService
{
    Task<IReadOnlyList<User>> ListAsync(Guid? storeId, CancellationToken ct);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<AuthToken> LoginAsync(LoginRequest request, CancellationToken ct);
}
