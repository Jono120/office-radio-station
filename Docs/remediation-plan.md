# Remediation Plan — Consistency Audit Follow-up

Addresses all findings from the 22 Jul 2026 audit (4 critical, 7 major, 8 minor), plus consolidation work to remove redundancy, plus two access-control requirements: the app must only be reachable from the local network, and only users with a company-domain email may add songs or vote. Phases are ordered so each one leaves the solution buildable and testable; later phases depend on earlier ones.

---

## Phase 1 — Configuration and environment alignment ✅ COMPLETE (22 Jul 2026)

Goal: a plain `dotnet run` of Api + Player + `npm run dev` works end to end. No behavior changes.

1. ✅ **Fix Api launch profile ports** (`OfficeJukebox.Api/Properties/launchSettings.json`)
   - Change `applicationUrl` to `http://localhost:5080` in the `http` and `https` profiles (or delete the profiles' `applicationUrl` so `appsettings.json` `Urls` wins).
   - 5080 is the documented port everywhere else: README, `appsettings.json`, Spotify redirect URI, Vite proxy, Player notify target, `use-admin-session.ts` error message.

2. ✅ **Give the Player its Api notify target** (`OfficeJukebox.Player/appsettings.json`, `Program.cs`)
   - Add `"Api": { "NotifyBaseUrl": "http://localhost:5080" }` to the committed settings.
   - Set `BaseAddress` on the `api-notifier` named client at registration and fail fast at startup if the value is missing, instead of throwing per-request inside `HttpQueueNotifier`.
   - Make notification failures non-fatal to the enqueue path: the track is already saved when the notify happens (`QueueServices.cs:81`), so a notify failure must log a warning, not turn a successful enqueue into a 500.

3. ✅ **Point the Api at the shared database** (`OfficeJukebox.Api/appsettings.json`)
   - Add the same `Storage` section the Player uses (`Data Source=%LOCALAPPDATA%/OfficeJukebox/jukebox.db`).
   - Keep the Player as the single migrator; add a startup check in the Api that the database exists/is reachable so misconfiguration fails loudly instead of silently creating an empty relative `jukebox.db`.

4. ✅ **Repo hygiene**
   - `git rm login.json` and add credential-shaped test payloads to `.gitignore`.

**Verify:** ✅ Done 22 Jul 2026 — build clean (0 warnings), 23/23 tests pass, both services started via `dotnet run` (Api on 5080), enqueue through the Api returned 201, and the Player's change notification reached `/api/internal/queue-changed`. Remaining from this checklist: a UI-driven queue + Spotify callback walkthrough, which needs a connected provider (deferred to the Phase 6 manual smoke).

---

## Phase 2 — Correctness criticals ✅ COMPLETE (22 Jul 2026)

5. ✅ **Unify time handling on UTC, time frames in the office time zone** (`SystemTimeProvider`, rules, orchestrator)
   - `ITimeProvider` now exposes `UtcNow` plus `OfficeTimeZone`/`OfficeNow`, backed by the new `Organization:TimeZone` setting (admin-set office location; machine zone when empty; invalid ids fail at startup).
   - All persisted timestamps are UTC and `PlaybackOrchestrator` writes `StartedAt` through the provider. Duration windows (`CannotQueueTrackThatHasPlayedInTheLastXHours`, score staleness) compare in pure UTC; wall-clock time frames (`OutOfHoursSkipRule` office hours, `ExceededDailyLimitVetoRule` "today") evaluate in the office zone.
   - The three test files that constructed `DateTime.Now` values were updated.

6. ✅ **Make veto/skip honor the target track id** (`QueueEndpoints.cs`, `PlaybackOrchestrator`)
   - Orchestrator signatures are now `VetoAsync(Guid, user)` / `SkipAsync(Guid, user)`: current track behaves as before, queued items are vetoed/skipped in place, unknown ids surface as 404 from the Player endpoints. Unit tests cover the mismatch and queued-item cases.
   - Bonus fix found while smoke-testing: adding a veto via `Update()` on the cached (detached) track graph threw `DbUpdateConcurrencyException`; new vetoes are now inserted explicitly through `ITrackPlayRepository.AddVetoAsync`.

7. ✅ **Eliminate the advancement race** (`PlaybackOrchestrator`, `PlaybackRuntimeState`)
   - `PlaybackRuntimeState` owns a single `SemaphoreSlim(1,1)`; start/poll/skip/veto all run their whole critical section inside it, and the old per-field locks are removed. A 16-way concurrent-start test proves single dequeue.
   - Added (found during Phase 1 verification): crash recovery in `QueueBootstrapService` — tracks stranded in `Playing` status by a shutdown are reset to `Queued` and re-queued at startup instead of silently lost. `GetQueuedAsync` generalized to `GetByStatusAsync`.

**Verify:** ✅ Done 22 Jul 2026 — 29/29 tests pass (6 new: id mismatch ×2, queued-item veto/skip, threshold, concurrency). Live smoke: veto-by-id 200, unknown id 404, stranded Phase-1 track recovered into the queue after restart.

---

## Phase 3 — Behavior and security majors ✅ COMPLETE (22 Jul 2026)

8. ✅ **Fix the repeat-queue rule** (`CannotQueueTrackThatHasPlayedInTheLastXHoursQueueRule`)
   - The rule is now global (the `q.User == user` filter and its misleading message are gone); per-user limits remain with `LimitNumberOfTracksQueuedByUser`.
   - The cutoff filter now runs SQL-side as a `Where` on the repository's `IQueryable` before the in-memory identity comparison (no new repository method needed — same effect as `GetPlayedSinceAsync` without widening the interface, since the rule API is synchronous).
   - New `TrackPlay.QueuedAt` column (UTC, set at enqueue; EF migration `AddTrackPlayQueuedAt`) backs a `StartedAt ?? QueuedAt` fallback so queued-but-unplayed duplicates are also caught. Pre-existing rows default to `DateTime.MinValue`, i.e. "long ago".

9. ✅ **Authenticate internal and Player endpoints**
   - One shared config value, `Security:InternalSharedSecret`, in both services' committed `appsettings.json` (and the dev example); both fail fast at startup if it's missing. Values must match across the two services.
   - The Api sends it as an `X-Internal-Secret` default header on the `PlayerClient`; Player middleware rejects everything except `/health` without it. The Player's notifier client sends the same header; Api middleware guards `/api/internal/*`.
   - The Player keeps its `localhost:5050` binding — it must never be exposed beyond the machine; the secret is defense in depth on top of that.

10. ✅ **Report provider enablement truthfully** (`MusicProviderRegistry`, `ProvidersController`)
    - Disabled providers are no longer registered at all: Infrastructure DI reads the `MusicProviders` section and registers only enabled catalog providers. `DisabledCatalogProvider` is deleted.
    - `ProviderInfo.IsEnabled` was dropped entirely — presence in the registry implies enabled, so the `enabled: true` in `ProviderInfoResponse` is now truthful by construction.
    - The Apple Music stub was removed (provider class, DI registration, `MusicProvidersOptions.AppleMusic`, example config section) — item 16's Apple Music bullet is done early.

11. ✅ **Single notification design** (Api)
    - `SignalRQueueNotifier` and its DI registration deleted; `QueueHub` reduced to an empty connection point (`Subscribe`/`NotifyQueueChanged` and the orphan `QueueUpdated` event removed).
    - `PlaybackProgressEvent` in `OfficeJukebox.Contracts` (trimmed to `ProgressMs`/`DurationMs`/`IsPlaying`) is now the one payload shape used by `HttpQueueNotifier`, `InternalNotificationsController`, and the SignalR broadcast; it serializes camelCase, matching the shape the frontend already reads.

**Verify:** ✅ Done 22 Jul 2026 — build clean, 31/31 tests pass (2 new: global-scope block for a different user, `QueuedAt` fallback for unplayed tracks). Migration applied on Player startup. Live smoke: Player and `api/internal/*` calls without the secret → 401, with it → 200, `/health` open; enqueue by user A 201 then the same track by user B blocked 400; `/api/providers` lists only real providers (manual, spotify, youtube — no apple-music).

---

## Phase 4 — Redundancy removal and dead code (the condensing pass) ✅ COMPLETE (22 Jul 2026, branch `phase-4-redundancy-removal`)

### Frontend

12. ✅ **Delete the abandoned UI stack** (`OfficeJukebox.Web`)
    - Removed `src/components/ui/`, `src/components/kibo-ui/`, `src/index.css`, `lib/utils.ts`, `use-theme.ts`, and the `@tailwindcss/vite` plugin from `vite.config.ts` (no `components.json` existed). `postcss.config.js` stays — it belongs to reshaped.
    - Dropped 11 dependencies: `tailwindcss`, `@tailwindcss/vite`, `tailwind-merge`, `tw-animate-css`, `class-variance-authority`, `clsx`, `@base-ui/react`, `@radix-ui/react-use-controllable-state`, `@dnd-kit/core`, `@dnd-kit/modifiers`, `motion`, `react-medium-image-zoom`. `lucide-react` stays — live pages import its icons directly.
    - Removed `queueStatusVariant`; the `StatusIndicator` bug went with the kibo-ui deletion.
    - The veto/skip helpers referenced by the audit no longer existed in `lib/api.ts` (only `apiFetch` remained) — nothing to wire or delete.
    - SignalR `useEffect` now detaches its three handlers via `connection.off(...)` in the cleanup, so StrictMode can't double-register.

### Api

13. ✅ **One proxy helper instead of six copies** (`PlayerProxy.ProxyAsync` extension)
    - All seven proxying actions (queue ×4, playback ×3) funnel through one `ProxyAsync` extension on `Task<HttpResponseMessage>` — status code and body always propagate, fixing the `GetQueue`/`GetNowPlaying` always-200 bug.
    - `IPlayerClient` is now typed with the Contracts records (`QueueTrackRequest`, `VetoRequest`, `SkipRequest`, `SetDeviceRequest`) instead of `object`.

14. ✅ **Consolidate provider OAuth plumbing** (`ProvidersController`, `ProviderTokenService`)
    - `StoreTokensAsync` now takes a `TokenRefreshResult` and computes the expiry internally (with a `fallbackRefreshToken` parameter for exchanges that don't return one) — the three copies of the expiry arithmetic collapsed into it.
    - `GetConnectUrl`'s duplicated OAuth-state block was unreachable (Spotify/YouTube early-returned before it) and is deleted; `StartAuth` remains the single OAuth entry point, and `connect-url` now serves `IMusicProvider.SetupUrl` metadata.
    - Hardcoded provider-id checks are gone: dashboard URLs live on `IMusicProvider.SetupUrl`, readiness on `IMusicCatalogProvider.IsReadyAsync()` (YouTube's checks stored key then config `ApiKey`), and the SaveConnection refresh-token path applies to any provider with an auth service rather than string-matching "spotify".

15. ✅ **Move external-id extraction into the providers** (`SearchController`)
    - `SearchAsync` now returns `CatalogSearchResult(ExternalId, Track)` — Spotify and YouTube surface the native id they already had at map time; the ~50 lines of URL-parsing heuristics in the controller are deleted.

### Application / Domain / Persistence

16. ✅ **Delete orphaned and stub code**
    - `AdminUser`, `RickRollTarget`, `SoundBoardEvent`, `SearchTerm` removed (entities, `DbSet`s, model config) with cleanup migration `RemoveOrphanedEntitiesAndIsSkipped` (applied; TrackPlays data preserved through SQLite's table rebuild).
    - ✅ Apple Music: removed in Phase 3 alongside the provider-enablement fix (item 10).
    - `IMusicPlaybackController.PauseAsync`/`ResumeAsync` and `IMusicProviderRegistry.GetAllCatalogProviders()` removed with their implementations.
    - `IsSkipped` dropped from `TrackPlay` (same migration); `Status == Skipped` is the single source, checked by `QueueManager.Dequeue`.

17. ✅ **Small logic fixes**
    - `TrackScoreService`: `AutoPlayCount` increment fixed to `isAutoplay ? 1 : 0` (data kept for future scoring). Orchestrator vetoes also stamp `VetoedAt` through `ITimeProvider` now.
    - `ExceededDailyLimitVetoRule`: counts by `TrackPlayVeto.VetoedAt` within the office-local day — a veto cast today on yesterday's track consumes today's allowance (new regression test).
    - `QueueManager.Dequeue`: iterative `while` loop replaces the recursion.

**Verify:** ✅ Done 22 Jul 2026 — backend build clean, 32/32 tests pass (new: veto-today-on-yesterday's-track). Frontend: `tsc -b` + `vite build` clean, `oxlint` clean, `depcheck` reports no unused dependencies. Cleanup migration applied on Player startup. Live smoke: queue/now-playing/providers 200, enqueue 201, veto with unknown id propagates the Player's 404 through the proxy, web app serves and proxies on 5173. All work is on branch `phase-4-redundancy-removal`, uncommitted (manual commits per workflow).

---

## Phase 5 — Network access control and user identity ✅ COMPLETE (23 Jul 2026, branch `phase-5-access-control`)

Two new requirements land here because they build on Phase 3's auth work and Phase 4's cleanup of the request payloads.

18. ✅ **Network access control — localhost for prototyping, config-only path to LAN/hosting** (Api)
    - The Api stays bound to `localhost:5080` for the demo, and the enforcement middleware ships now: `LanAllowlist` (Api/Security) is the **first** middleware in the pipeline — before Swagger, CORS, session, controllers, and the SignalR hub — and rejects with 403 any request whose `Connection.RemoteIpAddress` is not loopback or inside `Security:AllowedNetworks` (committed default: the three RFC 1918 ranges). CIDRs are parsed at startup via `IPNetwork.TryParse`, so a config typo fails the boot loudly; IPv4-mapped IPv6 addresses are unmapped before matching.
    - Only the socket address is consulted — `X-Forwarded-For` is deliberately ignored, as planned. The rollout path is unchanged: LAN = rebind `Urls` to `0.0.0.0:5080` (± pin the office subnet); hosted = provider edge controls + forwarded-headers with `KnownProxies` first.
    - The Player keeps its `localhost` binding (plus the item 9 shared secret).

19. ✅ **Domain-email identity for queueing and voting** (Api, Player contracts, frontend)
    - `OrganizationOptions` is now bound in the Api's `Program.cs` (the section was already in its `appsettings.json`).
    - New `SessionController`: `POST /api/session` validates the email with `MailAddress.TryCreate` and requires its host to equal `Organization:Domain` (case-insensitive); on success it stores the **lowercased email** (canonical identity) and display name (defaults to the email's local part) in the same cookie session as the admin login. `GET /api/session` restores state (401 when signed out), `DELETE /api/session` signs out. Missing `Organization:Domain` → 503, mirroring the admin-password guard.
    - Writes are gated by a new `RequireUserAttribute` (mirrors `RequireAdmin`) on queue/veto/skip ("like" from the plan text doesn't exist — there is no like endpoint). `QueueController` derives `User` from the session and fills it before proxying: the client-facing `QueueTrackClientRequest` has **no** `User` field, and veto/skip take no body at all — `VetoRequest`/`SkipRequest` are now Api→Player wire shapes only. Reads (queue, now-playing, search) and the SignalR hub stay open for wall displays.
    - Frontend: `useProfile` is session-backed (GET/POST/DELETE `/api/session`) instead of localStorage; the profile page's free-text username is replaced by a work-email + display-name sign-in card (signed-in state shows the account with a sign-out button, and "Queue activity" matches on the session email); the jukebox page no longer sends `user` and shows "Sign in with your work email on the Profile page…" when queueing returns 401.

20. ✅ **Follow-through on rules once identity is real**
    - The rules' `user` parameter is now always the session-derived lowercased email — the display name never reaches the Application layer, so it cannot influence `LimitNumberOfTracksQueuedByUser` or the veto daily limit. New impersonation regression test: with one user's queue full, the same track/name from a different email is not blocked, while the same email is.

**Verify:** ✅ Done 23 Jul 2026 — build clean, 33/33 tests pass (new: impersonation case). Live smoke: anonymous enqueue/veto → 401, wrong-domain sign-in → 401, malformed email → 400, domain sign-in → 200 with lowercased email, session-derived enqueue 201 attributed to the email, bodyless veto 200, sign-out then veto → 401, reads and the web app (5173, proxied) stay open. The 403 path can't be triggered from loopback by design — the fake-`RemoteIpAddress` integration test remains a Phase 6 item. All work on branch `phase-5-access-control`, uncommitted (manual commits per workflow).

---

## Phase 6 — Verification ✅ COMPLETE (23 Jul 2026, branch `phase-6-verification`)

- ✅ `dotnet build`: clean, **0 warnings**. Full test run: **54/54 pass** (33 Application + 21 in the new `OfficeJukebox.Api.Tests` project below).
- ✅ `tsc -b` + `vite build` succeed, `oxlint` clean, `npx depcheck` reports no unused dependencies.
- ✅ **New `tests/OfficeJukebox.Api.Tests` project** (the one code deliverable of this phase — added to the solution; `Program` exposed via `public partial class Program` for `WebApplicationFactory`):
  - `LanAllowlistTests` (14 cases): loopback and all three RFC 1918 ranges allowed; public IPv4/IPv6 rejected, including the `172.32.0.1` boundary just past `172.16.0.0/12`; null remote address rejected; IPv4-mapped IPv6 unmapped before matching; loopback allowed even with an empty allowlist; `Parse` throws on an invalid CIDR.
  - `AccessControlIntegrationTests` (7 cases): boots the real Api pipeline in-memory with a startup filter that fakes `Connection.RemoteIpAddress` (TestServer has no socket) and a throwaway SQLite file for the startup DB check. Proves: outside address → **403** on `/health` (the check loopback smoke can never trigger), loopback and private addresses served, queue/veto without a session → 401, wrong-domain sign-in → 401, and the full sign-in → session-restore (email lowercased) → sign-out → 401 lifecycle.
- ✅ Manual smoke (Player + Api + web via `dotnet run`/`npm run dev`): Player without the shared secret → 401; `/api/providers` lists manual/spotify/youtube truthfully; anonymous enqueue → 401; wrong-domain sign-in → 401; domain sign-in → 200; enqueue → 201 attributed to the session email; veto with an unknown id → 404, veto by the real id → 200 (the Phase 2 id semantics); the Player's `queue-changed` notifications reached `/api/internal/*` → 200 (SignalR broadcast path); web app served and proxied on 5173.
- Not covered: connecting Spotify/YouTube OAuth needs real credentials, which aren't configured on this machine — the provider connection walkthrough remains a manual step for whoever holds the keys.

## Suggested sequencing

Each phase is an independent, reviewable commit/PR. Phase 1 is pure configuration (safe, immediate payoff). Phase 2 changes runtime behavior and should carry the new tests. Phase 3 items 9–11 touch both services and the frontend event names, so land them together. Phase 4 is large but mechanical deletion — do the frontend (item 12) and backend (items 13–17) as separate commits so reverts stay cheap. Phase 5 depends on Phase 3 (session/auth groundwork) and on Phase 4's contract typing (item 13) since it removes the `User` field from those same request records.

---

**Status: all six phases complete (23 Jul 2026).** All 19 audit findings plus the two post-audit access-control requirements are resolved and verified. Phase 6's work sits on branch `phase-6-verification` (uncommitted, manual commits per workflow); the only open follow-up is the credential-dependent Spotify/YouTube OAuth walkthrough noted above.
