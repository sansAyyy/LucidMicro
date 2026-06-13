namespace LucidMicro.Services.Identity.Application.Features.SmsLogin.Abstractions;

public interface ISmsLoginCodeStore
{
    Task<bool> CanSendAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default);

    Task SaveCodeAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default);

    Task<string?> GetCodeAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default);

    Task RemoveCodeAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default);

    Task<int> IncrementAttemptAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default);
}
