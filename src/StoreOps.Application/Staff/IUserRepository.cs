using StoreOps.Domain.Staff;

namespace StoreOps.Application.Staff;

public interface IUserRepository
{
    Task<IReadOnlyList<User>> ListAsync(Guid? storeId, CancellationToken ct);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User> AddAsync(User user, CancellationToken ct);
}
