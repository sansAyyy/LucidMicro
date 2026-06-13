namespace LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Dtos.Requests;

public sealed record Update__EntityName__Request
{
    public string? Name { get; init; }

    public bool IsActive { get; init; } = true;
}
