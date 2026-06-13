namespace LucidMicro.Tests.Shared.Time;

public sealed class TestTimeProvider : TimeProvider
{
    public TestTimeProvider(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; set; }

    public override DateTimeOffset GetUtcNow()
    {
        return UtcNow;
    }
}
