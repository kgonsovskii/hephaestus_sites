using Microsoft.AspNetCore.Mvc;
using Sites.Cp.Models;
using Sites.DataFtp;
using Sites.Web;
using Sites.Web.Abstractions;
using Sites.Web.Git;

namespace Sites.Cp.Controllers;

public sealed class SystemController : Controller
{
    [HttpGet("/system")]
    public IActionResult Index() => View();
}

[ApiController]
[Route("api/system")]
public sealed class SitesSystemApiController : ControllerBase
{
    private readonly SitesGitService _git;
    private readonly SitesCatalogService _catalog;
    private readonly Sites.Cp.Services.SitesCloneService _clone;
    private readonly Sites.Cp.Services.SitesCloneRunStore _cloneRuns;
    private readonly Sites.Cp.Services.SitesUpdateService _update;
    private readonly Sites.Cp.Services.SitesRebootService _reboot;
    private readonly ISitesDataFtpUrlProvider _dataFtpUrl;
    private readonly SitesProfileSettingsService _settings;

    public SitesSystemApiController(
        SitesGitService git,
        SitesCatalogService catalog,
        Sites.Cp.Services.SitesCloneService clone,
        Sites.Cp.Services.SitesCloneRunStore cloneRuns,
        Sites.Cp.Services.SitesUpdateService update,
        Sites.Cp.Services.SitesRebootService reboot,
        ISitesDataFtpUrlProvider dataFtpUrl,
        SitesProfileSettingsService settings)
    {
        _git = git;
        _catalog = catalog;
        _clone = clone;
        _cloneRuns = cloneRuns;
        _update = update;
        _reboot = reboot;
        _dataFtpUrl = dataFtpUrl;
        _settings = settings;
    }

    [HttpGet("info")]
    public ActionResult<SystemInfoResponse> Info()
    {
        var repoRoot = RepositoryPaths.ResolveRoot();
        return Ok(new SystemInfoResponse
        {
            Profile = SitesProfileResolver.Current,
            ProfileFilePath = SitesProfileResolver.ResolveProfileFilePath(repoRoot),
            RepositoryRoot = repoRoot,
            CloneDirectory = SitesProfileResolver.ResolveCloneDirectory(),
            ProfileDataDirectory = SitesProfileResolver.ResolveProfileDirectory(repoRoot),
            SitesJsonPath = _catalog.SitesJsonPath,
            SettingsJsonPath = _settings.SettingsJsonPath,
            IsLinux = OperatingSystem.IsLinux(),
            WebRootFullPath = _dataFtpUrl.WebRootFullPath,
            WebFtpUrl = _dataFtpUrl.BuildUrl(Request.Host.Host)
        });
    }

    [HttpPost("profile")]
    public ActionResult<ProfileUpdateResponse> SetProfile([FromBody] ProfileUpdateRequest request)
    {
        var repoRoot = RepositoryPaths.ResolveRoot();
        SitesProfileResolver.WriteProfileFile(repoRoot, request.Profile);
        _settings.Reload();
        var siteCount = _catalog.ReloadRegistry();
        return Ok(new ProfileUpdateResponse
        {
            Profile = SitesProfileResolver.Current,
            ProfileFilePath = SitesProfileResolver.ResolveProfileFilePath(repoRoot),
            SitesJsonPath = _catalog.SitesJsonPath,
            SiteCount = siteCount
        });
    }

    [HttpGet("git/status")]
    public ActionResult<SitesGitStatus> GitStatus() => Ok(_git.GetStatus());

    [HttpPost("git/pull")]
    public async Task<ActionResult<SitesGitOperationResult>> GitPull(CancellationToken cancellationToken) =>
        Ok(await _git.PullAsync(cancellationToken));

    [HttpPost("git/push")]
    public async Task<ActionResult<SitesGitOperationResult>> GitPush(CancellationToken cancellationToken) =>
        Ok(await _git.PushAsync(cancellationToken));

    [HttpPost("git/sync")]
    public async Task<ActionResult<SitesGitOperationResult>> GitSync(CancellationToken cancellationToken) =>
        Ok(await _git.SyncAsync(cancellationToken));

    [HttpPost("clone")]
    public ActionResult<CloneStartResponse> StartClone([FromBody] CloneStartRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Profile))
            return BadRequest(new { error = "Profile is required." });

        string profile;
        try
        {
            profile = SitesProfileResolver.NormalizeProfileName(request.Profile);
            if (SitesProfileResolver.IsCloneDisallowedProfile(profile))
                return BadRequest(new { error = "The 'default' profile cannot be used for remote clone. Choose a dedicated profile name." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        if (string.IsNullOrWhiteSpace(request.Host))
            return BadRequest(new { error = "Host is required." });
        if (string.IsNullOrWhiteSpace(request.User))
            return BadRequest(new { error = "SSH user is required." });
        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "SSH password is required." });

        var runId = _cloneRuns.Start(
            (state, ct) => _clone.CloneToHostAsync(
                profile,
                request.Host,
                request.User,
                request.Password,
                state.AppendLine,
                ct),
            HttpContext.RequestAborted);

        return Ok(new CloneStartResponse { RunId = runId });
    }

    [HttpGet("clone/{runId}")]
    public ActionResult<CloneStatusResponse> CloneStatus(string runId)
    {
        if (!_cloneRuns.TryGet(runId, out var state) || state is null)
            return NotFound();

        return Ok(new CloneStatusResponse
        {
            RunId = state.RunId,
            Done = state.Done,
            ExitCode = state.ExitCode,
            Error = state.Error,
            Log = state.SnapshotLines()
        });
    }

    [HttpPost("update")]
    public ActionResult<Sites.Cp.Services.SitesUpdateResult> ScheduleUpdate() =>
        Ok(_update.ScheduleUpdate());

    [HttpPost("reboot")]
    public ActionResult<RebootServerResponse> Reboot()
    {
        var result = _reboot.ScheduleReboot();
        return Ok(new RebootServerResponse
        {
            Succeeded = result.Succeeded,
            Message = result.Message
        });
    }
}
