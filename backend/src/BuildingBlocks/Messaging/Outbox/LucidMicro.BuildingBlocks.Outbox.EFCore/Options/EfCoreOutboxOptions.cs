namespace LucidMicro.BuildingBlocks.Outbox.EFCore.Options;

public sealed class EfCoreOutboxOptions
{
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(5);

    public void Validate()
    {
        if (LockDuration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Outbox lock duration must be greater than zero.");
        }
    }
}
