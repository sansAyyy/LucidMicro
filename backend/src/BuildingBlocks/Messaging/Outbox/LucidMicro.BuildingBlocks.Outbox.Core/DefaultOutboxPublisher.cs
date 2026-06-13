using LucidMicro.BuildingBlocks.EventBus.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Outbox.Core.Mapping;
using LucidMicro.BuildingBlocks.Outbox.Core.Options;
using Microsoft.Extensions.Logging;

namespace LucidMicro.BuildingBlocks.Outbox.Core;

public sealed class DefaultOutboxPublisher : IOutboxPublisher
{
    private readonly IOutboxMessageStore _messageStore;
    private readonly IIntegrationEventEnvelopePublisher _envelopePublisher;
    private readonly TimeProvider _timeProvider;
    private readonly OutboxPublisherOptions _options;
    private readonly ILogger<DefaultOutboxPublisher> _logger;

    public DefaultOutboxPublisher(
        IOutboxMessageStore messageStore,
        IIntegrationEventEnvelopePublisher envelopePublisher,
        TimeProvider timeProvider,
        OutboxPublisherOptions options,
        ILogger<DefaultOutboxPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(messageStore);
        ArgumentNullException.ThrowIfNull(envelopePublisher);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        options.Validate();

        _messageStore = messageStore;
        _envelopePublisher = envelopePublisher;
        _timeProvider = timeProvider;
        _options = options;
        _logger = logger;
    }

    public async Task PublishPendingAsync(
        int maxCount = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCount);

        var messages = await _messageStore.ClaimPendingAsync(maxCount, cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var envelope = OutboxMessageEnvelopeMapper.Map(message);
                await _envelopePublisher.PublishAsync(envelope, cancellationToken);
                await _messageStore.MarkAsPublishedAsync(
                    message.Id,
                    _timeProvider.GetUtcNow(),
                    cancellationToken);

                _logger.LogInformation(
                    "Outbox message published. MessageId: {MessageId}, Type: {Type}",
                    message.Id,
                    message.Type);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var now = _timeProvider.GetUtcNow();
                var nextFailureCount = message.FailureCount + 1;
                var deadAt = nextFailureCount >= _options.MaxRetryCount ? now : (DateTimeOffset?)null;
                DateTimeOffset? nextRetryAt = deadAt is null
                    ? now.Add(CalculateRetryDelay(nextFailureCount))
                    : null;

                if (deadAt is null)
                {
                    _logger.LogWarning(
                        exception,
                        "Outbox message publish failed. MessageId: {MessageId}, Type: {Type}, FailureCount: {FailureCount}, NextRetryAt: {NextRetryAt}",
                        message.Id,
                        message.Type,
                        nextFailureCount,
                        nextRetryAt);
                }
                else
                {
                    _logger.LogError(
                        exception,
                        "Outbox message publish failed and marked as dead. MessageId: {MessageId}, Type: {Type}, FailureCount: {FailureCount}, DeadAt: {DeadAt}",
                        message.Id,
                        message.Type,
                        nextFailureCount,
                        deadAt);
                }

                await _messageStore.MarkAsFailedAsync(
                    message.Id,
                    GetErrorMessage(exception),
                    nextRetryAt,
                    deadAt,
                    cancellationToken);
            }

            await _messageStore.SaveChangesAsync(cancellationToken);
        }
    }

    private static string GetErrorMessage(Exception exception)
    {
        return string.IsNullOrWhiteSpace(exception.Message)
            ? exception.GetType().Name
            : exception.Message;
    }

    private TimeSpan CalculateRetryDelay(int failureCount)
    {
        var multiplier = Math.Pow(_options.RetryBackoffFactor, failureCount - 1);
        var milliseconds = _options.InitialRetryDelay.TotalMilliseconds * multiplier;

        if (double.IsInfinity(milliseconds)
            || milliseconds > _options.MaxRetryDelay.TotalMilliseconds)
        {
            return _options.MaxRetryDelay;
        }

        return TimeSpan.FromMilliseconds(milliseconds);
    }
}
