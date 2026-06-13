using System.Security.Cryptography;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Abstractions;

namespace LucidMicro.Services.Identity.Application.Features.SmsLogin.Services;

public sealed class RandomSmsLoginCodeGenerator : ISmsLoginCodeGenerator
{
    public string Generate()
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000);

        return code.ToString("D6");
    }
}
