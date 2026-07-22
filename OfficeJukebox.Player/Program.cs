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

builder.Services.AddHttpClient("api-notifier", client => client.BaseAddress = new Uri(notifyBaseUrl));
builder.Services.AddSingleton<IQueueNotifier, HttpQueueNotifier>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<JukeboxDbContext>();
    await db.Database.MigrateAsync();
    var bootstrap = scope.ServiceProvider.GetRequiredService<IQueueBootstrapService>();
    await bootstrap.LoadQueuedTracksAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "OfficeJukebox.Player" }));
app.MapQueueEndpoints();

app.Run();
