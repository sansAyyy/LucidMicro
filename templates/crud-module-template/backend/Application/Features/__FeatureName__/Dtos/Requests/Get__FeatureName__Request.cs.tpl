namespace LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Dtos.Requests;

public sealed record Get__FeatureName__Request
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Keyword { get; init; }
}
