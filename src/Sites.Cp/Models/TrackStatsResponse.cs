using Sites.Track;

namespace Sites.Cp.Models;

public sealed class TrackStatsResponse
{
    public string From { get; init; } = "";

    public string To { get; init; } = "";

    public int Days { get; init; }

    public IReadOnlyList<TrackStatRow> Rows { get; init; } = [];

    public TrackStatTotals Totals { get; init; } = new();
}

public sealed class TrackStatTotals
{
    public int Hit { get; init; }

    public int Video { get; init; }

    public int Play { get; init; }

    public int Goal { get; init; }
}
