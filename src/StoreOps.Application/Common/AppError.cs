namespace StoreOps.Application.Common;

public abstract class AppError : Exception
{
    public abstract string Code { get; }
    public abstract int StatusCode { get; }

    protected AppError(string message) : base(message)
    {
    }
}
