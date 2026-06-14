using System.Threading.Channels;

namespace Sites.Web.Git;

/// <summary>Wakes repo git sync when sites.json changes (like Hephaestus <c>NotifyHostsChanged</c>).</summary>
public sealed class SitesCatalogChangedSignal
{
    private readonly Channel<bool> _wake = Channel.CreateUnbounded<bool>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });

    public void NotifyCatalogChanged() => _wake.Writer.TryWrite(true);

    public Task WhenWakeAsync(CancellationToken cancellationToken = default) =>
        _wake.Reader.ReadAsync(cancellationToken).AsTask();

    public void DrainExtraSignals()
    {
        while (_wake.Reader.TryRead(out _)) { }
    }
}
