using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OfficeJukebox.Api.Tests.Security;

/// <summary>
/// Boots the real Api pipeline in-memory (remediation plan Phase 6). The
/// TestServer has no socket, so a startup filter injects a fake
/// Connection.RemoteIpAddress per request — this proves the LAN allowlist
/// middleware itself returns 403 for outside addresses, which a loopback-bound
/// live smoke can never exercise.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"officejukebox-api-tests-{Guid.NewGuid():N}.db");

    /// <summary>The RemoteIpAddress every in-memory request appears to come from.</summary>
    public IPAddress? RemoteIp { get; set; } = IPAddress.Loopback;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                // Throwaway database so the startup reachability check passes
                // without a Player run.
                ["Storage:ConnectionString"] = $"Data Source={_dbPath}",
                ["Organization:Domain"] = "contoso.test"
            }));

        builder.ConfigureServices(services =>
            services.AddTransient<IStartupFilter>(_ => new FakeRemoteIpStartupFilter(() => RemoteIp)));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        File.Delete(_dbPath);
    }

    private sealed class FakeRemoteIpStartupFilter(Func<IPAddress?> remoteIp) : IStartupFilter
    {
        // Startup-filter middleware wraps the whole app pipeline, so the fake
        // address is in place before the allowlist middleware runs.
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                app.Use(async (context, nextMiddleware) =>
                {
                    context.Connection.RemoteIpAddress = remoteIp();
                    await nextMiddleware();
                });
                next(app);
            };
    }
}

public class AccessControlIntegrationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Request_from_outside_the_allowlist_gets_403()
    {
        factory.RemoteIp = IPAddress.Parse("203.0.113.7");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Request_from_loopback_is_served()
    {
        factory.RemoteIp = IPAddress.Loopback;
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Request_from_a_private_range_is_served()
    {
        factory.RemoteIp = IPAddress.Parse("192.168.1.50");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Queueing_without_a_session_gets_401()
    {
        factory.RemoteIp = IPAddress.Loopback;
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/queue",
            new { provider = "manual", externalId = "x", trackName = "x" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Vetoing_without_a_session_gets_401()
    {
        factory.RemoteIp = IPAddress.Loopback;
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/queue/{Guid.NewGuid()}/veto", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Sign_in_with_a_non_company_email_is_rejected()
    {
        factory.RemoteIp = IPAddress.Loopback;
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/session",
            new { email = "intruder@evil.example", displayName = "Intruder" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Sign_in_with_a_company_email_creates_a_session()
    {
        factory.RemoteIp = IPAddress.Loopback;
        // CreateClient handles cookies, so the session persists across calls.
        using var client = factory.CreateClient();

        var signIn = await client.PostAsJsonAsync("/api/session",
            new { email = "Jane.Doe@Contoso.test", displayName = "Jane" });
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);

        var me = await client.GetFromJsonAsync<SessionBody>("/api/session");
        Assert.NotNull(me);
        Assert.Equal("jane.doe@contoso.test", me.Email); // canonical: lowercased
        Assert.Equal("Jane", me.DisplayName);

        var signOut = await client.DeleteAsync("/api/session");
        Assert.Equal(HttpStatusCode.OK, signOut.StatusCode);

        var afterSignOut = await client.GetAsync("/api/session");
        Assert.Equal(HttpStatusCode.Unauthorized, afterSignOut.StatusCode);
    }

    private sealed record SessionBody(string Email, string DisplayName);
}
