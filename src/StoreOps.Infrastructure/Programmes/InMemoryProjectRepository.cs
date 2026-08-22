using System.Collections.Concurrent;
using StoreOps.Application.Programmes;
using StoreOps.Domain.Programmes;

namespace StoreOps.Infrastructure.Programmes;

internal sealed class InMemoryProjectRepository : IProjectRepository
{
    private readonly ConcurrentDictionary<Guid, Project> _store = new();

    public Task<IReadOnlyList<Project>> ListAsync(Guid? storeId, CancellationToken ct)
    {
        var projects = _store.Values.AsEnumerable();
        if (storeId.HasValue)
        {
            projects = projects.Where(p => p.StoreId == storeId.Value);
        }

        return Task.FromResult<IReadOnlyList<Project>>(projects.ToList());
    }

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        _store.TryGetValue(id, out var project);
        return Task.FromResult(project);
    }

    public Task<Project> AddAsync(Project project, CancellationToken ct)
    {
        _store[project.Id] = project;
        return Task.FromResult(project);
    }
}
