using OfficeJukebox.Api.Hubs;
using OfficeJukebox.Api.Options;
using OfficeJukebox.Api.Security;
using OfficeJukebox.Api.Services;
using OfficeJukebox.Application.Configuration;
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
builder.Services.Configure<OrganizationOptions>(builder.Configuration.GetSection(OrganizationOptions.SectionName));

// Item 18: parse the LAN allowlist at startup so a CIDR typo fails loudly.
var allowedNetworks = LanAllowlist.Parse(
    builder.Configuration.GetSection("Security:AllowedNetworks").Get<string[]>()
    ?? LanAllowlist.DefaultNetworks);

var internalSecret = builder.Configuration["Security:InternalSharedSecret"];
if (string.IsNullOrWhiteSpace(internalSecret))
{
    throw new InvalidOperationException(
        "Security:InternalSharedSecret is not configured. It must match the Player's value.");
}

builder.Services.AddHttpClient<IPlayerClient, PlayerClient>(client =>
{
    var baseUrl = builder.Configuration["Player:BaseUrl"] ?? "http://localhost:5050";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("X-Internal-Secret", internalSecret);
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

// First middleware in the pipeline so it also covers the SignalR hub, Swagger,
// and health endpoints: only loopback and Security:AllowedNetworks may talk to
// the Api at all. Trusts only the socket address — never X-Forwarded-For.
app.Use(async (context, next) =>
{
    if (!LanAllowlist.IsAllowed(context.Connection.RemoteIpAddress, allowedNetworks))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { error = "OfficeJukebox is only available on the office network." });
        return;
    }

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseSession();

// The internal notification endpoints exist solely for the Player; without
// this gate anyone reaching the Api could broadcast spoofed SignalR events.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/internal"))
    {
        if (!context.Request.Headers.TryGetValue("X-Internal-Secret", out var provided) ||
            provided != internalSecret)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid internal secret." });
            return;
        }
    }

    await next();
});

app.MapControllers();
app.MapHub<QueueHub>("/hubs/queue");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "OfficeJukebox.Api" }));

app.Run();
