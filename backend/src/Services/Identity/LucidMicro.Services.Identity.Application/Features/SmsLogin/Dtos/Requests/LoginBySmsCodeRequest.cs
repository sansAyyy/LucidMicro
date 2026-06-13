namespace LucidMicro.Services.Identity.Application.Features.SmsLogin.Dtos.Requests;

public sealed record LoginBySmsCodeRequest
{
    public string? PhoneNumber { get; init; }

    public string? Code { get; init; }
}
