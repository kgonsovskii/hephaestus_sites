namespace Sites.Track;

public static class TrackStats
{
    public static IReadOnlyList<TrackStatRow> Aggregate(
        IEnumerable<TrackVisit> visits,
        DateOnly from,
        DateOnly to,
        string? flow = null,
        string? domain = null)
    {
        var flowFilter = string.IsNullOrWhiteSpace(flow) ? null : flow.Trim();
        var domainFilter = string.IsNullOrWhiteSpace(domain) ? null : domain.Trim();

        var groups = new Dictionary<(DateOnly Day, string Flow, string Domain), Group>(GroupKeyComparer.Instance);

        foreach (var visit in visits)
        {
            if (visit.Day < from || visit.Day > to)
                continue;
            if (flowFilter is not null &&
                !string.Equals(visit.Flow, flowFilter, StringComparison.OrdinalIgnoreCase))
                continue;
            if (domainFilter is not null &&
                !string.Equals(visit.Domain, domainFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            var key = (visit.Day, visit.Flow, visit.Domain);
            if (!groups.TryGetValue(key, out var group))
            {
                group = new Group();
                groups[key] = group;
            }

            if (visit.Hit)
                group.Hit.Add(visit.Ip);
            if (visit.Video)
                group.Video.Add(visit.Ip);
            if (visit.Play)
                group.Play.Add(visit.Ip);
            if (visit.Goal)
                group.Goal.Add(visit.Ip);
        }

        return groups
            .OrderByDescending(entry => entry.Key.Day)
            .ThenBy(entry => entry.Key.Flow, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Key.Domain, StringComparer.OrdinalIgnoreCase)
            .Select(entry => new TrackStatRow
            {
                Day = entry.Key.Day.ToString("yyyy-MM-dd"),
                Flow = entry.Key.Flow,
                Domain = entry.Key.Domain,
                Hit = entry.Value.Hit.Count,
                Video = entry.Value.Video.Count,
                Play = entry.Value.Play.Count,
                Goal = entry.Value.Goal.Count
            })
            .ToArray();
    }

    private sealed class Group
    {
        public HashSet<string> Hit { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Video { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Play { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Goal { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class GroupKeyComparer : IEqualityComparer<(DateOnly Day, string Flow, string Domain)>
    {
        public static readonly GroupKeyComparer Instance = new();

        public bool Equals((DateOnly Day, string Flow, string Domain) x, (DateOnly Day, string Flow, string Domain) y) =>
            x.Day == y.Day &&
            string.Equals(x.Flow, y.Flow, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Domain, y.Domain, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((DateOnly Day, string Flow, string Domain) obj) =>
            HashCode.Combine(obj.Day, StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Flow), StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Domain));
    }
}
