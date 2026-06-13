namespace LucidMicro.BuildingBlocks.Resilience.Http.Options;

public sealed class LucidHttpResilienceOptions
{
    public const string ConfigurationSectionName = "Lucid:Resilience:Http";

    public bool Enabled { get; set; } = true;

    public int TotalRequestTimeoutSeconds { get; set; } = 30;

    public int AttemptTimeoutSeconds { get; set; } = 10;

    public LucidHttpRetryOptions Retry { get; set; } = new();

    public LucidHttpCircuitBreakerOptions CircuitBreaker { get; set; } = new();

    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (TotalRequestTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("HTTP resilience total request timeout seconds must be greater than zero.");
        }

        if (AttemptTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("HTTP resilience attempt timeout seconds must be greater than zero.");
        }

        if (AttemptTimeoutSeconds > TotalRequestTimeoutSeconds)
        {
            throw new InvalidOperationException("HTTP resilience attempt timeout cannot exceed total request timeout.");
        }

        Retry.Validate();
        CircuitBreaker.Validate();
    }
}

public sealed class LucidHttpRetryOptions
{
    public int MaxRetryAttempts { get; set; } = 3;

    public int DelayMilliseconds { get; set; } = 200;

    public void Validate()
    {
        if (MaxRetryAttempts < 0)
        {
            throw new InvalidOperationException("HTTP resilience retry attempts cannot be negative.");
        }

        if (DelayMilliseconds <= 0)
        {
            throw new InvalidOperationException("HTTP resilience retry delay milliseconds must be greater than zero.");
        }
    }
}

public sealed class LucidHttpCircuitBreakerOptions
{
    public double FailureRatio { get; set; } = 0.5;

    public int MinimumThroughput { get; set; } = 20;

    public int SamplingDurationSeconds { get; set; } = 30;

    public int BreakDurationSeconds { get; set; } = 30;

    public void Validate()
    {
        if (FailureRatio is <= 0 or > 1)
        {
            throw new InvalidOperationException("HTTP resilience circuit breaker failure ratio must be greater than 0 and less than or equal to 1.");
        }

        if (MinimumThroughput <= 0)
        {
            throw new InvalidOperationException("HTTP resilience circuit breaker minimum throughput must be greater than zero.");
        }

        if (SamplingDurationSeconds <= 0)
        {
            throw new InvalidOperationException("HTTP resilience circuit breaker sampling duration seconds must be greater than zero.");
        }

        if (BreakDurationSeconds <= 0)
        {
            throw new InvalidOperationException("HTTP resilience circuit breaker break duration seconds must be greater than zero.");
        }
    }
}
