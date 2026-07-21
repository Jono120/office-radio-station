# OfficeJukebox Architecture

## Overview

OfficeJukebox is a layered .NET 8 solution for office music queueing with multi-provider support (Spotify, Apple Music, YouTube, Manual). Users queue tracks, veto skips, and autoplay fills gaps. Playback is controlled on a shared office device via provider APIs (e.g. Spotify Connect).

```
┌─────────────┐     HTTP      ┌──────────────┐     HTTP      ┌────────────────┐
│  Web (Vite) │ ────────────► │  Api (BFF)   │ ────────────► │ Player (host)  │
│  React TS   │   SignalR     │  OAuth/search│               │ queue + playback│
└─────────────┘               └──────────────┘               └───────┬────────┘
                                                                     │
                                                                     ▼
                                                            ┌────────────────┐
                                                            │ SQLite (EF)    │
                                                            └────────────────┘
```

## Layers

| Project | Responsibility |
|---------|----------------|
| **OfficeJukebox.Domain** | Entities, `TrackRef`, repository interfaces |
| **OfficeJukebox.Application** | Queue/veto/skip rules, playback orchestrator, provider abstractions |
| **OfficeJukebox.Infrastructure** | Spotify/Apple/YouTube/Manual adapters, OAuth token storage |
| **OfficeJukebox.Infrastructure.Persistence** | EF Core `JukeboxDbContext`, SQLite repositories |
| **OfficeJukebox.Contracts** | Shared API DTOs |
| **OfficeJukebox.Player** | Queue engine, playback loop, device control |
| **OfficeJukebox.Api** | BFF, OAuth callbacks, search proxy, SignalR hub |
| **OfficeJukebox.Web** | React UI with search, queue, now-playing, provider settings |

## Provider layer

Providers implement `IMusicCatalogProvider` and optionally `IMusicPlaybackController`. The `IMusicProviderRegistry` resolves providers by ID. Capabilities (`Search`, `Resolve`, `DevicePlayback`, `RequiresAuth`) drive UI and orchestrator behaviour.

## Data model

- **TrackPlays** — queue/history with `Provider`, `ExternalId`, `Status`, full track metadata in `TrackJson`
- **TrackScores** — autoplay scores keyed by `(Provider, ExternalId)`
- **ProviderCredentials** — encrypted OAuth tokens per provider
- **SearchTerms**, **AdminUsers**, **RickRollTargets**, **SoundBoardEvents** — stubs for future features

## Configuration

See `appsettings.Development.json.example` for `Storage`, `QueueRules`, `VetoRules`, `SkipRules`, `Player`, and `MusicProviders` sections.
