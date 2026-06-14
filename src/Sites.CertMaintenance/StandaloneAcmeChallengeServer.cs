using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace Sites.CertMaintenance;

/// <summary>
/// Standalone HTTP listener for Sites.CertTool when Sites.Host is not running.
/// </summary>
public sealed class StandaloneAcmeChallengeServer : IAcmeChallengePublisher, IAsyncDisposable
{
    private readonly HttpListener _listener;
    private readonly ConcurrentDictionary<string, string> _responses = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _cts = new();
    private Task? _listenTask;

    public StandaloneAcmeChallengeServer(int port)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{port}/");
    }

    public void PublishChallenge(string token, string keyAuthz) =>
        _responses[token] = keyAuthz;

    public void ClearChallenges() =>
        _responses.Clear();

    public void Start()
    {
        _listener.Start();
        _listenTask = Task.Run(ListenLoopAsync);
    }

    private async Task ListenLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().WaitAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (HttpListenerException)
            {
                break;
            }

            _ = Task.Run(() => HandleRequestAsync(context));
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            var path = context.Request.Url?.AbsolutePath ?? "/";
            const string prefix = "/.well-known/acme-challenge/";
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.Close();
                return;
            }

            var token = path[prefix.Length..].Trim('/');
            if (!_responses.TryGetValue(token, out var keyAuthz))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.Close();
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(keyAuthz);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "text/plain";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        }
        catch
        {
            try
            {
                context.Response.Abort();
            }
            catch
            {
                // ignored
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _listener.Stop();
        _listener.Close();

        if (_listenTask is not null)
        {
            try
            {
                await _listenTask;
            }
            catch
            {
                // ignored
            }
        }

        _cts.Dispose();
    }
}
