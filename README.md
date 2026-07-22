# OfficeJukebox

Greenfield office jukebox built on .NET 10, SQLite (EF Core), and React. Supports multiple music providers (Spotify, Apple Music, YouTube) with shared-device playback.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
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
},
"Admin": {
  "Password": "change-me"
}
```

### 3. Connect and pick a device

1. Run Api, Player, and Web (see **Run locally** above).
2. Open the Web UI → **Settings** (user icon) → **Accounts** → sign in with the password from `Admin:Password` in your API config.
3. Connect Spotify (and other providers) from the admin settings page and approve access.
4. Open Spotify on your office speaker, computer, or phone (Spotify Connect).
5. In admin settings, click **Devices** → **Use** on the target playback device.
6. Return to the jukebox and search/queue tracks — playback runs on the selected Connect device.

Tokens are stored encrypted in SQLite and refreshed automatically.

## YouTube connection

OfficeJukebox uses the [YouTube Data API v3](https://developers.google.com/youtube/v3) for search and queue metadata. YouTube does not support device playback through this app — queued tracks include a link you can open on a speaker or cast device.

### 1. Create a Google Cloud API key

1. Sign in to the [Google Cloud Console](https://console.cloud.google.com/).
2. Create or select a project.
3. Enable **YouTube Data API v3** (APIs & Services → Library).
4. Create an API key (APIs & Services → Credentials → Create credentials → API key).
5. Restrict the key to YouTube Data API v3 if desired.

### 2. Configure the API key

In both `OfficeJukebox.Api/appsettings.Development.json` and `OfficeJukebox.Player/appsettings.Development.json`:

```json
"MusicProviders": {
  "YouTube": {
    "Enabled": true,
    "ApiKey": "your-youtube-data-api-key"
  }
}
```

No OAuth or admin connect step is required — search works as soon as the API key is set.

### 3. Search and queue

1. Run Api, Player, and Web (see **Run locally** above).
2. Open the jukebox page and select **YouTube** as the search provider.
3. Search for tracks and add them to the queue.

Queued YouTube tracks store metadata and a watch link. Playback on a shared office device requires opening the link or casting separately until a device integration is added.

## Architecture

See [Docs/architecture.md](Docs/architecture.md), [Docs/clean-build-plan.md](Docs/clean-build-plan.md), and [Docs/multi-provider-modernization-plan.md](Docs/multi-provider-modernization-plan.md).
