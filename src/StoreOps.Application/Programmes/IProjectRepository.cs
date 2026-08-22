using StoreOps.Domain.Programmes;

namespace StoreOps.Application.Programmes;

public interface IProjectRepository
{
    Task<IReadOnlyList<Project>> ListAsync(Guid? storeId, CancellationToken ct);
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Project> AddAsync(Project project, CancellationToken ct);
}
