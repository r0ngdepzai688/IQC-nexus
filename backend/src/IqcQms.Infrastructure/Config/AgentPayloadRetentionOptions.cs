namespace IqcQms.Infrastructure.Config;

public class AgentPayloadRetentionOptions
{
    public int FullResultRetentionDays { get; set; } = 90;
    public int ReplayTombstoneRetentionDays { get; set; } = 365;
    public int CleanupBatchSize { get; set; } = 100;
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    public void Validate()
    {
        if (FullResultRetentionDays <= 0)
        {
            throw new InvalidOperationException("FullResultRetentionDays must be greater than 0.");
        }

        if (ReplayTombstoneRetentionDays <= 0)
        {
            throw new InvalidOperationException("ReplayTombstoneRetentionDays must be greater than 0.");
        }

        if (ReplayTombstoneRetentionDays <= FullResultRetentionDays)
        {
            throw new InvalidOperationException("ReplayTombstoneRetentionDays must be strictly greater than FullResultRetentionDays.");
        }

        if (CleanupBatchSize <= 0 || CleanupBatchSize > 5000)
        {
            throw new InvalidOperationException("CleanupBatchSize must be between 1 and 5000.");
        }

        if (CleanupInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("CleanupInterval must be positive.");
        }
    }
}
