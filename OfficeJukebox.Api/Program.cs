using OfficeJukebox.Api.Hubs;
using OfficeJukebox.Api.Options;
using OfficeJukebox.Api.Services;
using OfficeJukebox.Application.Abstractions;
using OfficeJukebox.Infrastructure;
using OfficeJukebox.Infrastructure.Persistence;
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
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "OfficeJukebox.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin =>
                    Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                    uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
            return;
        }

        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection(AdminOptions.SectionName));
builder.Services.AddSingleton<IQueueNotifier, SignalRQueueNotifier>();
builder.Services.AddHttpClient<IPlayerClient, PlayerClient>(client =>
{
    var baseUrl = builder.Configuration["Player:BaseUrl"] ?? "http://localhost:5050";
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

// The Player owns the database and runs migrations; the Api only reads/writes it.
// Fail loudly on a misconfigured Storage section instead of silently creating an
// empty database at a different path.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<JukeboxDbContext>();
    if (!await db.Database.CanConnectAsync())
    {
        throw new InvalidOperationException(
            "Cannot reach the OfficeJukebox database. Check Storage:ConnectionString in appsettings, " +
            "and start OfficeJukebox.Player at least once first — it creates and migrates the database.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseSession();
app.MapControllers();
app.MapHub<QueueHub>("/hubs/queue");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "OfficeJukebox.Api" }));

app.Run();
