using System.Collections.Concurrent;

namespace Sites.Track;

public sealed class TrackStore
{
    private readonly ConcurrentDictionary<TrackVisitKey, TrackVisit> _visits = new();
    private readonly object _dirtyLock = new();
    private bool _dirty;

    public bool IsDirty
    {
        get
        {
            lock (_dirtyLock)
                return _dirty;
        }
    }

    public void ReplaceAll(IEnumerable<TrackVisit> visits)
    {
        _visits.Clear();
        foreach (var visit in visits)
            _visits[visit.Key] = visit;

        lock (_dirtyLock)
            _dirty = false;
    }

    public TrackVisit? TryGet(TrackVisitKey key) =>
        _visits.TryGetValue(key, out var visit) ? visit : null;

    public IReadOnlyCollection<TrackVisit> Snapshot() => _visits.Values.ToArray();

    public TrackVisit Touch(
        DateOnly day,
        string ip,
        string flow,
        string domain,
        string site,
        string target1,
        string target2,
        TrackEventKind kind,
        DateTime now)
    {
        var key = new TrackVisitKey(day, ip, flow, domain);
        var visit = _visits.AddOrUpdate(
            key,
            _ => Create(day, ip, flow, domain, site, target1, target2, kind, now),
            (_, existing) =>
            {
                lock (existing)
                {
                    Apply(existing, kind, now);
                    if (site.Length > 0)
                        existing.Site = site;
                    if (target1.Length > 0)
                        existing.Target1 = target1;
                    if (target2.Length > 0)
                        existing.Target2 = target2;
                    return existing;
                }
            });

        MarkDirty();
        return visit;
    }

    public bool TryMarkGoal(string ip, TimeSpan lockWindow, DateTime now, out TrackVisit? visit)
    {
        visit = null;
        var cutoff = now - lockWindow;

        foreach (var candidate in _visits.Values)
        {
            DateTime? goalAt;
            lock (candidate)
                goalAt = candidate.GoalAt;

            if (!TrackCookie.SameClientIp(candidate.Ip, ip))
                continue;

            if (goalAt is DateTime locked && locked >= cutoff)
                return false;
        }

        TrackVisit? best = null;
        foreach (var candidate in _visits.Values)
        {
            if (!TrackCookie.SameClientIp(candidate.Ip, ip))
                continue;
            if (candidate.LastSeen < cutoff)
                continue;
            if (string.IsNullOrEmpty(candidate.Flow))
                continue;

            if (best is null || IsBetterGoalMatch(candidate, best))
                best = candidate;
        }

        if (best is null)
            return false;

        lock (best)
        {
            if (best.GoalAt is DateTime existing && existing >= cutoff)
                return false;

            best.Goal = true;
            best.GoalAt = now;
            best.LastSeen = now;
        }

        MarkDirty();
        visit = best;
        return true;
    }

    public void MarkClean()
    {
        lock (_dirtyLock)
            _dirty = false;
    }

    private void MarkDirty()
    {
        lock (_dirtyLock)
            _dirty = true;
    }

    private static TrackVisit Create(
        DateOnly day,
        string ip,
        string flow,
        string domain,
        string site,
        string target1,
        string target2,
        TrackEventKind kind,
        DateTime now)
    {
        var visit = new TrackVisit
        {
            Day = day,
            Ip = ip,
            Flow = flow,
            Domain = domain,
            Site = site,
            Target1 = target1,
            Target2 = target2,
            FirstSeen = now,
            LastSeen = now
        };
        Apply(visit, kind, now);
        return visit;
    }

    private static void Apply(TrackVisit visit, TrackEventKind kind, DateTime now)
    {
        visit.LastSeen = now;
        switch (kind)
        {
            case TrackEventKind.Hit:
                visit.Hit = true;
                break;
            case TrackEventKind.Video:
                visit.Hit = true;
                visit.Video = true;
                break;
            case TrackEventKind.Play:
                visit.Hit = true;
                visit.Play = true;
                break;
            case TrackEventKind.Goal:
                visit.Goal = true;
                visit.GoalAt = now;
                break;
        }
    }

    /// <summary>
    /// Credit the flow this IP used last (campaign landing after organic Play must win).
    /// Same LastSeen: Play, then Video, then Hit.
    /// </summary>
    private static bool IsBetterGoalMatch(TrackVisit candidate, TrackVisit best)
    {
        if (candidate.LastSeen != best.LastSeen)
            return candidate.LastSeen > best.LastSeen;
        return Score(candidate) > Score(best);
    }

    private static int Score(TrackVisit visit)
    {
        if (visit.Play)
            return 3;
        if (visit.Video)
            return 2;
        if (visit.Hit)
            return 1;
        return 0;
    }
}
