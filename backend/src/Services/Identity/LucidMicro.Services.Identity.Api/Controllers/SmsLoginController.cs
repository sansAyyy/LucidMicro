using LucidMicro.BuildingBlocks.AspNetCore.Results;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Abstractions;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Dtos.Requests;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Dtos.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LucidMicro.Services.Identity.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/sms-login")]
public sealed class SmsLoginController : ControllerBase
{
    private readonly ISmsLoginApplicationService _smsLogin;

    public SmsLoginController(ISmsLoginApplicationService smsLogin)
    {
        _smsLogin = smsLogin;
    }

    [HttpPost("codes")]
    public async Task<ActionResult> SendCodeAsync(
        [FromBody] SendSmsLoginCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _smsLogin.SendCodeAsync(request, cancellationToken);

        return this.ToActionResult(result, NoContent);
    }

    [HttpPost]
    public async Task<ActionResult<SmsLoginResponse>> LoginAsync(
        [FromBody] LoginBySmsCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _smsLogin.LoginAsync(request, cancellationToken);

        return this.ToActionResult(result);
    }
}
