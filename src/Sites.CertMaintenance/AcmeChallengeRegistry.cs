using System.Collections.Concurrent;

namespace Sites.CertMaintenance;

public sealed class AcmeChallengeRegistry : IAcmeChallengePublisher
{
    private readonly ConcurrentDictionary<string, string> _responses = new(StringComparer.Ordinal);

    public void PublishChallenge(string token, string keyAuthz) =>
        _responses[token] = keyAuthz;

    public void ClearChallenges() =>
        _responses.Clear();

    public bool TryGetResponse(string token, out string keyAuthz) =>
        _responses.TryGetValue(token, out keyAuthz!);
}

public interface IAcmeChallengePublisher
{
    void PublishChallenge(string token, string keyAuthz);

    void ClearChallenges();
}
