using StoreOps.Application.Common;

namespace StoreOps.Application.Programmes.Errors;

public sealed class ProgrammeValidationError : AppError
{
    public override string Code => "PROGRAMME_VALIDATION_ERROR";
    public override int StatusCode => 422;

    public ProgrammeValidationError(string message) : base(message)
    {
    }
}
