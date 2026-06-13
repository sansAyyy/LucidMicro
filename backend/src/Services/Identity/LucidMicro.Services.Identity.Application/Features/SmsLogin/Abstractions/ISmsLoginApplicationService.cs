using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Dtos.Responses;

namespace LucidMicro.Services.Identity.Application.Features.SmsLogin.Abstractions;

public interface ISmsLoginApplicationService
{
    Task<Result> SendCodeAsync(
        SendSmsLoginCodeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<SmsLoginResponse>> LoginAsync(
        LoginBySmsCodeRequest request,
        CancellationToken cancellationToken = default);
}
