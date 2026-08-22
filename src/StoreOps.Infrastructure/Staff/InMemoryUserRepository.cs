using System.Collections.Concurrent;
using StoreOps.Application.Staff;
using StoreOps.Domain.Staff;

namespace StoreOps.Infrastructure.Staff;

internal sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _store = new();

    public Task<IReadOnlyList<User>> ListAsync(Guid? storeId, CancellationToken ct)
    {
        var users = _store.Values.AsEnumerable();
        if (storeId.HasValue)
        {
            users = users.Where(u => u.StoreId == storeId.Value);
        }

        return Task.FromResult<IReadOnlyList<User>>(users.ToList());
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        _store.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        var user = _store.Values.FirstOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<User> AddAsync(User user, CancellationToken ct)
    {
        _store[user.Id] = user;
        return Task.FromResult(user);
    }
}
