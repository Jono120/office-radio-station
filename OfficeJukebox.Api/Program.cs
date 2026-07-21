using OfficeJukebox.Api.Hubs;
using OfficeJukebox.Api.Services;
using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IQueueNotifier, SignalRQueueNotifier>();
builder.Services.AddHttpClient<IPlayerClient, PlayerClient>(client =>
{
    var baseUrl = builder.Configuration["Player:BaseUrl"] ?? "http://localhost:5050";
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSession();
app.MapControllers();
app.MapHub<QueueHub>("/hubs/queue");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "OfficeJukebox.Api" }));

app.Run();
