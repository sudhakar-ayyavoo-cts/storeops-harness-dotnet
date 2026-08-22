using StoreOps.Domain.Programmes;

namespace StoreOps.Application.Programmes;

public interface IProgrammesService
{
    Task<IReadOnlyList<Project>> ListAsync(Guid? storeId, CancellationToken ct);
    Task<Project> CreateAsync(CreateProgrammeRequest request, CancellationToken ct);
    Task<Project> GetByIdAsync(Guid id, CancellationToken ct);
}
