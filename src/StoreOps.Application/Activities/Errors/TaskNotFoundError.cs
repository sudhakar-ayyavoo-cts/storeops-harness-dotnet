using StoreOps.Application.Common;

namespace StoreOps.Application.Activities.Errors;

public sealed class TaskNotFoundError : AppError
{
    public override string Code => "TASK_NOT_FOUND";
    public override int StatusCode => 404;

    public TaskNotFoundError(Guid taskId) : base($"Task {taskId} was not found.")
    {
    }
}
