using System.Net;

namespace OfficeJukebox.Api.Security;

/// <summary>
/// Network access control (remediation plan item 18). Requests are allowed
/// only from loopback or the CIDR ranges configured under
/// Security:AllowedNetworks (default: the RFC 1918 private ranges). On the
/// localhost-only demo every request arrives via loopback, so this exercises
/// the exact enforcement path that will guard the LAN later.
///
/// Deliberately trusts only Connection.RemoteIpAddress — never X-Forwarded-For.
/// When the app is eventually fronted by a proxy, enable ASP.NET's
/// forwarded-headers middleware with KnownProxies first, otherwise the check
/// becomes spoofable.
/// </summary>
public static class LanAllowlist
{
    public static readonly string[] DefaultNetworks =
    [
        "10.0.0.0/8",
        "172.16.0.0/12",
        "192.168.0.0/16"
    ];

    /// <summary>Parses CIDR strings, failing fast at startup on a typo.</summary>
    public static IReadOnlyList<IPNetwork> Parse(IReadOnlyList<string> cidrs)
    {
        var networks = new List<IPNetwork>(cidrs.Count);
        foreach (var cidr in cidrs)
        {
            if (!IPNetwork.TryParse(cidr, out var network))
            {
                throw new InvalidOperationException(
                    $"Security:AllowedNetworks contains an invalid CIDR range: '{cidr}'.");
            }

            networks.Add(network);
        }

        return networks;
    }

    public static bool IsAllowed(IPAddress? remoteIp, IReadOnlyList<IPNetwork> allowedNetworks)
    {
        if (remoteIp is null)
        {
            return false;
        }

        // Kestrel reports IPv4 clients as IPv4-mapped IPv6 when dual-stack.
        var ip = remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4() : remoteIp;

        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }

        return allowedNetworks.Any(network => network.Contains(ip));
    }
}
