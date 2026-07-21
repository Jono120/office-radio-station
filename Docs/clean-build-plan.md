# Clean build plan

## Goals

- New organization-ready codebase with **no Trade Me branding** in `src/`
- **SQLite + EF Core** from day one (no MongoDB)
- Port **minimum viable rule logic** from legacy PlayMe.Server with tests
- Vertical slice: build → test → Player API → Api BFF → React scaffold → CI

## Decisions

| Area | Choice | Rationale |
|------|--------|-----------|
| Storage | SQLite via EF Core 8 | Simple local deploy; `%LOCALAPPDATA%/OfficeJukebox/jukebox.db` |
| Queue state | In-memory `QueueManager` + persisted `TrackPlays` | Matches legacy pattern; persistence for history/scoring |
| Scoring | C# `TrackScoreService` | Replaces Mongo MapReduce with deterministic C# aggregation + random factor |
| Player | ASP.NET Core minimal API on port 5050 | Single process for queue + DB migrations |
| API | BFF + HttpClient to Player | Decouples UI/auth from player process |
| Frontend | Vite + React + TypeScript | Modern scaffold; full UI deferred |
| Tests | xUnit + Moq | Ported from NUnit/Moq legacy tests |

## Explicit non-goals (MVP)

- Spotify or other music provider integration
- Authentication / admin UI
- Full Durandal/Knockout UI parity
- Rick roll, soundboard, broadcast messages (entity stubs only)

## Legacy reference only

Rule logic was read from `PlayMe.Server` and `PlayMe.UnitTest` at repo root. No legacy project files were modified.

## Compliance

- Root `LICENSE` (MIT) retained
- `NOTICE.md` attributes ported rule logic to original Trade Me 2014 work
- CI `forbidden-strings` check blocks legacy terms in `src/`
