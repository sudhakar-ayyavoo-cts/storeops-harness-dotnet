using StoreOps.Application.Common;

namespace StoreOps.Application.Staff.Errors;

public sealed class InvalidCredentialsError : AppError
{
    public override string Code => "INVALID_CREDENTIALS";
    public override int StatusCode => 401;

    public InvalidCredentialsError() : base("Invalid email or password.")
    {
    }
}
