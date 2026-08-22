using StoreOps.Application.Common;

namespace StoreOps.Application.Staff.Errors;

public sealed class StaffNotFoundError : AppError
{
    public override string Code => "STAFF_NOT_FOUND";
    public override int StatusCode => 404;

    public StaffNotFoundError(Guid id) : base($"Staff member {id} was not found.")
    {
    }
}
