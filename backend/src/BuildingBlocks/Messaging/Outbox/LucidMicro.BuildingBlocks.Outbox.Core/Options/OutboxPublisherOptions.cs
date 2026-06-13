namespace LucidMicro.BuildingBlocks.Outbox.Core.Options;

public sealed class OutboxPublisherOptions
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(10);

    public int BatchSize { get; set; } = 50;

    public int MaxRetryCount { get; set; } = 10;

    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(30);

    public double RetryBackoffFactor { get; set; } = 2;

    public void Validate()
    {
        if (Interval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Outbox publisher interval must be greater than zero.");
        }

        if (BatchSize <= 0)
        {
            throw new InvalidOperationException("Outbox publisher batch size must be greater than zero.");
        }

        if (MaxRetryCount <= 0)
        {
            throw new InvalidOperationException("Outbox publisher max retry count must be greater than zero.");
        }

        if (InitialRetryDelay <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Outbox publisher initial retry delay must be greater than zero.");
        }

        if (MaxRetryDelay < InitialRetryDelay)
        {
            throw new InvalidOperationException("Outbox publisher max retry delay must be greater than or equal to initial retry delay.");
        }

        if (RetryBackoffFactor < 1)
        {
            throw new InvalidOperationException("Outbox publisher retry backoff factor must be greater than or equal to one.");
        }
    }
}
