namespace Sites.Track;

public enum TrackEventKind
{
    Hit,
    Video,
    Play,
    Goal
}

public sealed class TrackVisit
{
    public DateOnly Day { get; set; }
    public string Ip { get; set; } = "";
    public string Flow { get; set; } = "";
    public string Domain { get; set; } = "";
    public string Site { get; set; } = "";
    public string Target1 { get; set; } = "";
    public string Target2 { get; set; } = "";
    public bool Hit { get; set; }
    public bool Video { get; set; }
    public bool Play { get; set; }
    public bool Goal { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public DateTime? GoalAt { get; set; }

    public TrackVisitKey Key => new(Day, Ip, Flow, Domain);
}

public readonly record struct TrackVisitKey(DateOnly Day, string Ip, string Flow, string Domain);

public sealed class TrackStatRow
{
    public string Day { get; init; } = "";
    public string Flow { get; init; } = "";
    public string Domain { get; init; } = "";
    public int Hit { get; init; }
    public int Video { get; init; }
    public int Play { get; init; }
    public int Goal { get; init; }
}
