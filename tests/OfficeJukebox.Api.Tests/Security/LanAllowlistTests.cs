using System.Net;
using OfficeJukebox.Api.Security;

namespace OfficeJukebox.Api.Tests.Security;

public class LanAllowlistTests
{
    private static readonly IReadOnlyList<IPNetwork> Defaults = LanAllowlist.Parse(LanAllowlist.DefaultNetworks);

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("10.1.2.3")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.254")]
    [InlineData("192.168.1.50")]
    public void Allows_loopback_and_private_ranges(string ip) =>
        Assert.True(LanAllowlist.IsAllowed(IPAddress.Parse(ip), Defaults));

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("203.0.113.7")]
    [InlineData("172.32.0.1")] // just past 172.16.0.0/12
    [InlineData("2001:db8::1")] // public IPv6
    public void Rejects_public_addresses(string ip) =>
        Assert.False(LanAllowlist.IsAllowed(IPAddress.Parse(ip), Defaults));

    [Fact]
    public void Rejects_a_missing_remote_address() =>
        Assert.False(LanAllowlist.IsAllowed(null, Defaults));

    [Fact]
    public void Unmaps_ipv4_mapped_ipv6_before_matching()
    {
        // Kestrel reports IPv4 clients this way on dual-stack sockets.
        Assert.True(LanAllowlist.IsAllowed(IPAddress.Parse("::ffff:192.168.1.50"), Defaults));
        Assert.False(LanAllowlist.IsAllowed(IPAddress.Parse("::ffff:203.0.113.7"), Defaults));
    }

    [Fact]
    public void Allows_loopback_even_when_no_networks_are_configured() =>
        Assert.True(LanAllowlist.IsAllowed(IPAddress.Loopback, []));

    [Fact]
    public void Parse_fails_fast_on_an_invalid_cidr() =>
        Assert.Throws<InvalidOperationException>(() => LanAllowlist.Parse(["not-a-cidr"]));
}
