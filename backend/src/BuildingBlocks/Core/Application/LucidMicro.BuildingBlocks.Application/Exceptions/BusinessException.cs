using LucidMicro.BuildingBlocks.Application.Results;

namespace LucidMicro.BuildingBlocks.Application.Exceptions;

public class BusinessException : Exception
{
    public BusinessException(string message)
        : this(Error.Failure("Business.Error", message))
    {
    }

    public BusinessException(string code, string message)
        : this(Error.Failure(code, message))
    {
    }

    public BusinessException(Error error)
        : base(error.Message)
    {
        Error = error;
    }

    public Error Error { get; }

    public string Code => Error.Code;
}
