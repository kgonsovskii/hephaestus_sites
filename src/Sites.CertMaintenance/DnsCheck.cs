using System.Net;
using System.Net.Sockets;

namespace Sites.CertMaintenance;

public static class DnsCheck
{
    public static async Task CheckAsync(
        IReadOnlyList<string> domains,
        CancellationToken cancellationToken = default)
    {
        string? localIp = null;
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("1.1.1.1", 80);
            if (socket.LocalEndPoint is IPEndPoint endPoint)
                localIp = endPoint.Address.ToString();
        }
        catch
        {
            // ignored
        }

        foreach (var domain in domains)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(domain);
                var ips = string.Join(", ", addresses.Select(address => address.ToString()));
                var marker = localIp is not null && addresses.Any(address => address.ToString() == localIp)
                    ? "ok"
                    : "check";
                Console.WriteLine($"  [{marker}] {domain} -> {ips}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [fail] {domain} -> {ex.Message}");
            }
        }
    }
}
