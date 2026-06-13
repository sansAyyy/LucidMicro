using LucidMicro.BuildingBlocks.Caching.Abstractions.Contracts;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Abstractions;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Options;

namespace LucidMicro.Services.Identity.Infrastructure.ExternalServices.SmsLogin;

public sealed class RedisSmsLoginCodeStore : ISmsLoginCodeStore
{
    private readonly ICacheService _cache;
    private readonly SmsLoginOptions _options;

    public RedisSmsLoginCodeStore(ICacheService cache, SmsLoginOptions options)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        _cache = cache;
        _options = options;
    }

    public async Task<bool> CanSendAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        ThrowIfPhoneNumberIsInvalid(phoneNumber);

        var rateLimited = await _cache.GetAsync<bool?>(RateKey(phoneNumber), cancellationToken);

        return rateLimited is null;
    }

    public async Task SaveCodeAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default)
    {
        ThrowIfPhoneNumberIsInvalid(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        await _cache.SetAsync(
            CodeKey(phoneNumber),
            code.Trim(),
            TimeSpan.FromSeconds(_options.CodeTtlSeconds),
            cancellationToken);
        await _cache.SetAsync(
            RateKey(phoneNumber),
            true,
            TimeSpan.FromSeconds(_options.SendIntervalSeconds),
            cancellationToken);
        await _cache.RemoveAsync(AttemptKey(phoneNumber), cancellationToken);
    }

    public Task<string?> GetCodeAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        ThrowIfPhoneNumberIsInvalid(phoneNumber);

        return _cache.GetAsync<string>(CodeKey(phoneNumber), cancellationToken);
    }

    public async Task RemoveCodeAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        ThrowIfPhoneNumberIsInvalid(phoneNumber);

        await _cache.RemoveAsync(CodeKey(phoneNumber), cancellationToken);
        await _cache.RemoveAsync(AttemptKey(phoneNumber), cancellationToken);
    }

    public async Task<int> IncrementAttemptAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        ThrowIfPhoneNumberIsInvalid(phoneNumber);

        var key = AttemptKey(phoneNumber);
        var current = await _cache.GetAsync<int?>(key, cancellationToken) ?? 0;
        var next = current + 1;
        await _cache.SetAsync(
            key,
            next,
            TimeSpan.FromSeconds(_options.AttemptTtlSeconds),
            cancellationToken);

        return next;
    }

    private static string CodeKey(string phoneNumber)
    {
        return $"identity:sms-login:code:{phoneNumber.Trim()}";
    }

    private static string RateKey(string phoneNumber)
    {
        return $"identity:sms-login:rate:{phoneNumber.Trim()}";
    }

    private static string AttemptKey(string phoneNumber)
    {
        return $"identity:sms-login:attempt:{phoneNumber.Trim()}";
    }

    private static void ThrowIfPhoneNumberIsInvalid(string phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
    }
}
