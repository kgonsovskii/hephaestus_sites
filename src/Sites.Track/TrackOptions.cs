namespace Sites.Track;

public sealed class TrackOptions
{
    public const string SectionName = "Track";

    public string? ConnectionString { get; set; }

    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan GoalLock { get; set; } = TimeSpan.FromHours(24);

    public string GoalKey { get; set; } = "";
}
