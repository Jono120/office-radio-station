# OfficeJukebox

Greenfield office jukebox built on .NET 8, SQLite (EF Core), and React. Supports multiple music providers (Spotify, Apple Music, YouTube) with shared-device playback.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) (for the web UI)

## Build

```bash
dotnet build OfficeJukebox.sln
dotnet test tests/OfficeJukebox.Application.Tests/OfficeJukebox.Application.Tests.csproj
```

## Run locally

1. Copy `appsettings.Development.json.example` to `OfficeJukebox.Player/appsettings.Development.json` and `OfficeJukebox.Api/appsettings.Development.json`, then adjust as needed.
2. Start the **Player** (queue engine + SQLite):

   ```bash
   dotnet run --project OfficeJukebox.Player
   ```

   Listens on `http://localhost:5050`.

3. Start the **API** (BFF + SignalR):

   ```bash
   dotnet run --project OfficeJukebox.Api
   ```

   Listens on `http://localhost:5080` — proxies queue to Player at `Player:BaseUrl`.

4. Start the **Web** UI:

   ```bash
   cd OfficeJukebox.Web
   npm install
   npm run dev
   ```

## Database

SQLite file defaults to `%LOCALAPPDATA%/OfficeJukebox/jukebox.db`. Migrations run automatically on Player startup.

To add migrations:

```bash
dotnet ef migrations add <Name> \
  --project OfficeJukebox.Infrastructure.Persistence \
  --startup-project OfficeJukebox.Player
```

## Spotify connection

OfficeJukebox uses the [Spotify Web API](https://developer.spotify.com/documentation/web-api) for search and Spotify Connect playback.

### 1. Create a Spotify app

1. Sign in at [Spotify for Developers](https://developer.spotify.com/dashboard) and create an app.
2. Add redirect URI: `http://localhost:5080/api/providers/spotify/callback`
3. Copy the **Client ID** and **Client secret**.

**Development mode notes (2026):** Spotify development apps require the app owner to have **Spotify Premium**, allowlist up to **5 users** in the developer dashboard, and cap search results at **10** tracks per request. See [Spotify's developer access update](https://developer.spotify.com/blog/2026-02-06-update-on-developer-access-and-platform-security).

### 2. Configure credentials

Copy `appsettings.Development.json.example` to both `OfficeJukebox.Api/appsettings.Development.json` and `OfficeJukebox.Player/appsettings.Development.json`, then set:

```json
"MusicProviders": {
  "WebAppUrl": "http://localhost:5173",
  "Spotify": {
    "Enabled": true,
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "RedirectUri": "http://localhost:5080/api/providers/spotify/callback"
  }
}
```

### 3. Connect and pick a device

1. Run Api, Player, and Web (see **Run locally** above).
2. Open the Web UI → **Providers** → **Connect** on Spotify and approve access.
3. Open Spotify on your office speaker, computer, or phone (Spotify Connect).
4. Click **Devices** → **Use** on the target playback device.
5. Search and queue tracks — playback runs on the selected Connect device.

Tokens are stored encrypted in SQLite and refreshed automatically.

## Architecture

See [Docs/architecture.md](Docs/architecture.md), [Docs/clean-build-plan.md](Docs/clean-build-plan.md), and [Docs/multi-provider-modernization-plan.md](Docs/multi-provider-modernization-plan.md).
