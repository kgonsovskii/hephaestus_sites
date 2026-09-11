using Microsoft.AspNetCore.Mvc;
using Sites.Cp.Models;
using Sites.Track;

namespace Sites.Cp.Controllers;

public sealed class StatsController : Controller
{
    [HttpGet("/stats")]
    public IActionResult Index() => View();
}

[ApiController]
[Route("api/stats")]
public sealed class SitesStatsApiController : ControllerBase
{
    private readonly TrackStore _store;

    public SitesStatsApiController(TrackStore store) => _store = store;

    [HttpGet]
    public ActionResult<TrackStatsResponse> Get(
        [FromQuery] int days = 7,
        [FromQuery] string? flow = null,
        [FromQuery] string? domain = null)
    {
        if (days < 1)
            days = 1;
        if (days > 90)
            days = 90;

        var to = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = to.AddDays(1 - days);
        var rows = TrackStats.Aggregate(_store.Snapshot(), from, to, flow, domain);
        return Ok(new TrackStatsResponse
        {
            From = from.ToString("yyyy-MM-dd"),
            To = to.ToString("yyyy-MM-dd"),
            Days = days,
            Rows = rows,
            Totals = new TrackStatTotals
            {
                Hit = rows.Sum(row => row.Hit),
                Video = rows.Sum(row => row.Video),
                Play = rows.Sum(row => row.Play),
                Goal = rows.Sum(row => row.Goal)
            }
        });
    }
}
