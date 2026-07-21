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

## Architecture

See [Docs/architecture.md](Docs/architecture.md), [Docs/clean-build-plan.md](Docs/clean-build-plan.md), and [Docs/multi-provider-modernization-plan.md](Docs/multi-provider-modernization-plan.md).
