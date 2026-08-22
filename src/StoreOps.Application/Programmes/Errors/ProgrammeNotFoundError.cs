using StoreOps.Application.Common;

namespace StoreOps.Application.Programmes.Errors;

public sealed class ProgrammeNotFoundError : AppError
{
    public override string Code => "PROGRAMME_NOT_FOUND";
    public override int StatusCode => 404;

    public ProgrammeNotFoundError(Guid id) : base($"Programme {id} was not found.")
    {
    }
}
