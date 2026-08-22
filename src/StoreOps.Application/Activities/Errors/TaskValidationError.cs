using StoreOps.Application.Common;

namespace StoreOps.Application.Activities.Errors;

public sealed class TaskValidationError : AppError
{
    public override string Code => "TASK_VALIDATION_ERROR";
    public override int StatusCode => 422;

    public TaskValidationError(string message) : base(message)
    {
    }
}
