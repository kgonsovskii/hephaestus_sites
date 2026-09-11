using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Sites.Track;

public sealed class TrackPostgres
{
    private readonly IOptions<TrackOptions> _options;
    private readonly ILogger<TrackPostgres> _logger;

    public TrackPostgres(IOptions<TrackOptions> options, ILogger<TrackPostgres> logger)
    {
        _options = options;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ResolveConnectionString());

    public async Task<IReadOnlyList<TrackVisit>> LoadRecentAsync(CancellationToken cancellationToken)
    {
        var cs = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(cs))
            return [];

        try
        {
            await using var connection = new NpgsqlConnection(cs);
            await connection.OpenAsync(cancellationToken);
            await using var cmd = new NpgsqlCommand(
                """
                SELECT day, ip, flow, domain, site, target1, target2, hit, video, play, goal, first_seen, last_seen, goal_at
                FROM track_visit
                WHERE last_seen >= @since
                """,
                connection);
            cmd.Parameters.AddWithValue("since", DateTime.UtcNow.AddDays(-30));
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var list = new List<TrackVisit>();
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new TrackVisit
                {
                    Day = DateOnly.FromDateTime(reader.GetDateTime(0)),
                    Ip = reader.GetString(1),
                    Flow = reader.GetString(2),
                    Domain = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Site = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Target1 = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Target2 = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    Hit = reader.GetBoolean(7),
                    Video = reader.GetBoolean(8),
                    Play = reader.GetBoolean(9),
                    Goal = reader.GetBoolean(10),
                    FirstSeen = DateTime.SpecifyKind(reader.GetDateTime(11), DateTimeKind.Utc),
                    LastSeen = DateTime.SpecifyKind(reader.GetDateTime(12), DateTimeKind.Utc),
                    GoalAt = reader.IsDBNull(13)
                        ? null
                        : DateTime.SpecifyKind(reader.GetDateTime(13), DateTimeKind.Utc)
                });
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Track postgres load skipped");
            return [];
        }
    }

    public async Task FlushAsync(IReadOnlyCollection<TrackVisit> visits, CancellationToken cancellationToken)
    {
        var cs = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(cs) || visits.Count == 0)
            return;

        await using var connection = new NpgsqlConnection(cs);
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        foreach (var visit in visits)
        {
            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO track_visit (
                  day, ip, flow, domain, site, target1, target2, hit, video, play, goal, first_seen, last_seen, goal_at)
                VALUES (
                  @day, @ip, @flow, @domain, @site, @target1, @target2, @hit, @video, @play, @goal, @first_seen, @last_seen, @goal_at)
                ON CONFLICT (day, ip, flow, domain) DO UPDATE SET
                  hit = track_visit.hit OR EXCLUDED.hit,
                  video = track_visit.video OR EXCLUDED.video,
                  play = track_visit.play OR EXCLUDED.play,
                  goal = track_visit.goal OR EXCLUDED.goal,
                  last_seen = GREATEST(track_visit.last_seen, EXCLUDED.last_seen),
                  site = CASE WHEN EXCLUDED.site <> '' THEN EXCLUDED.site ELSE track_visit.site END,
                  target1 = CASE WHEN EXCLUDED.target1 <> '' THEN EXCLUDED.target1 ELSE track_visit.target1 END,
                  target2 = CASE WHEN EXCLUDED.target2 <> '' THEN EXCLUDED.target2 ELSE track_visit.target2 END,
                  goal_at = COALESCE(track_visit.goal_at, EXCLUDED.goal_at)
                """,
                connection,
                tx);
            cmd.Parameters.AddWithValue("day", visit.Day.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("ip", visit.Ip);
            cmd.Parameters.AddWithValue("flow", visit.Flow);
            cmd.Parameters.AddWithValue("domain", visit.Domain);
            cmd.Parameters.AddWithValue("site", visit.Site);
            cmd.Parameters.AddWithValue("target1", visit.Target1);
            cmd.Parameters.AddWithValue("target2", visit.Target2);
            cmd.Parameters.AddWithValue("hit", visit.Hit);
            cmd.Parameters.AddWithValue("video", visit.Video);
            cmd.Parameters.AddWithValue("play", visit.Play);
            cmd.Parameters.AddWithValue("goal", visit.Goal);
            cmd.Parameters.AddWithValue("first_seen", visit.FirstSeen);
            cmd.Parameters.AddWithValue("last_seen", visit.LastSeen);
            cmd.Parameters.AddWithValue("goal_at", (object?)visit.GoalAt ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    private string? ResolveConnectionString()
    {
        var options = _options.Value;
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
            return options.ConnectionString;

        return null;
    }
}
