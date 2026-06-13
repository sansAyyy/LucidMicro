namespace LucidMicro.Services.Identity.Application.Features.SmsLogin.Dtos.Requests;

public sealed record SendSmsLoginCodeRequest
{
    public string? PhoneNumber { get; init; }
}
