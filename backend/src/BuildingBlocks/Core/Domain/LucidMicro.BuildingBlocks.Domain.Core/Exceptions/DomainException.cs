namespace LucidMicro.BuildingBlocks.Domain.Core.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string? Code { get; }
}
