using StoreOps.Application.Programmes.Errors;
using StoreOps.Domain.Programmes;

namespace StoreOps.Application.Programmes;

public sealed class ProgrammesService : IProgrammesService
{
    private readonly IProjectRepository _projectRepository;

    public ProgrammesService(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<IReadOnlyList<Project>> ListAsync(Guid? storeId, CancellationToken ct)
        => await _projectRepository.ListAsync(storeId, ct);

    public async Task<Project> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var project = await _projectRepository.GetByIdAsync(id, ct);
        if (project is null)
        {
            throw new ProgrammeNotFoundError(id);
        }

        return project;
    }

    public async Task<Project> CreateAsync(CreateProgrammeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ProgrammeValidationError("Name is required.");
        }

        if (request.StoreId == Guid.Empty)
        {
            throw new ProgrammeValidationError("StoreId is required.");
        }

        if (request.EndDate.HasValue && request.EndDate <= request.StartDate)
        {
            throw new ProgrammeValidationError("EndDate must be after StartDate.");
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            StoreId = request.StoreId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        return await _projectRepository.AddAsync(project, ct);
    }
}
