namespace LucidMicro.BuildingBlocks.Application.Results;

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    public static Error Failure(string code, string message)
    {
        return Create(code, message, ErrorType.Failure);
    }

    public static Error Validation(string code, string message)
    {
        return Create(code, message, ErrorType.Validation);
    }

    public static Error NotFound(string code, string message)
    {
        return Create(code, message, ErrorType.NotFound);
    }

    public static Error Conflict(string code, string message)
    {
        return Create(code, message, ErrorType.Conflict);
    }

    public static Error Unauthorized(string code, string message)
    {
        return Create(code, message, ErrorType.Unauthorized);
    }

    public static Error Forbidden(string code, string message)
    {
        return Create(code, message, ErrorType.Forbidden);
    }

    private static Error Create(string code, string message, ErrorType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new Error(code, message, type);
    }
}
