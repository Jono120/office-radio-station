using Microsoft.EntityFrameworkCore;
using OfficeJukebox.Application;
using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Application.Queue.Services;
using OfficeJukebox.Infrastructure;
using OfficeJukebox.Infrastructure.Persistence;
using OfficeJukebox.Player.Endpoints;
using OfficeJukebox.Player.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddPlayback();
builder.Services.AddInfrastructure(builder.Configuration);
var notifyBaseUrl = builder.Configuration["Api:NotifyBaseUrl"];
if (string.IsNullOrWhiteSpace(notifyBaseUrl))
{
    throw new InvalidOperationException(
        "Api:NotifyBaseUrl is not configured. Set it to the OfficeJukebox.Api base URL (e.g. http://localhost:5080).");
}

var internalSecret = builder.Configuration["Security:InternalSharedSecret"];
if (string.IsNullOrWhiteSpace(internalSecret))
{
    throw new InvalidOperationException(
        "Security:InternalSharedSecret is not configured. It must match the Api's value.");
}

builder.Services.AddHttpClient("api-notifier", client =>
{
    client.BaseAddress = new Uri(notifyBaseUrl);
    client.DefaultRequestHeaders.Add("X-Internal-Secret", internalSecret);
});
builder.Services.AddSingleton<IQueueNotifier, HttpQueueNotifier>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<JukeboxDbContext>();
    await db.Database.MigrateAsync();
    var bootstrap = scope.ServiceProvider.GetRequiredService<IQueueBootstrapService>();
    await bootstrap.LoadQueuedTracksAsync();
}

// Everything except /health requires the shared secret: the Player has no
// user auth of its own and must only be reachable by the Api. This closes the
// [RequireAdmin] bypass via direct Player calls.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/health"))
    {
        await next();
        return;
    }

    if (!context.Request.Headers.TryGetValue("X-Internal-Secret", out var provided) ||
        provided != internalSecret)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid internal secret." });
        return;
    }

    await next();
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "OfficeJukebox.Player" }));
app.MapQueueEndpoints();

app.Run();
