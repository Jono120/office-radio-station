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

## Phase 4 — Redundancy removal and dead code (the condensing pass)

### Frontend

12. **Delete the abandoned UI stack** (`OfficeJukebox.Web`)
    - Remove `src/components/ui/`, `src/components/kibo-ui/`, `src/index.css`, `components.json`, and the `@tailwindcss/vite` plugin from `vite.config.ts`.
    - Drop the dependencies that exist only for it: `tailwindcss`, `@tailwindcss/vite`, `tailwind-merge`, `tw-animate-css`, `class-variance-authority`, `lucide-react`, `@base-ui/react`, `@radix-ui/react-use-controllable-state`, `clsx` (`lib/utils.ts` goes with it), plus `@dnd-kit/*`, `motion`, and `react-medium-image-zoom` if a usage search confirms they're unreferenced.
    - Remove dead helpers `use-theme` and `queueStatusVariant`; this also disposes of the `StatusIndicator` className bug.
    - Wire the veto/skip helpers in `lib/api.ts` into the queue UI (they become meaningful once Phase 2 item 6 lands) or delete them.
    - Return a cleanup function from every SignalR `useEffect` (`connection.off(...)`) so StrictMode doesn't double-register handlers, and refetch only on the matching event.

### Api

13. **One proxy helper instead of six copies** (`QueueController`, `PlaybackController`)
    - Every action repeats read-content + build `ContentResult`. Extract a single `ProxyAsync(Task<HttpResponseMessage>)` helper (controller base or extension).
    - This also fixes a latent bug: `GetQueue` and `GetNowPlaying` use `Content(...)`, which always returns 200 even when the Player errored, while the other four propagate the status code.
    - Type `IPlayerClient` with the `OfficeJukebox.Contracts` request records instead of `object` so the Api↔Player contract is compiler-checked.

14. **Consolidate provider OAuth plumbing** (`ProvidersController`, `ProviderTokenService`)
    - `GetConnectUrl` and `StartAuth` duplicate the state-generation/session/build-URL block — extract one private helper; the two endpoints likely collapse into one (the frontend only needs one of them — check usage and delete the other).
    - The "compute `expiresAt` then `StoreTokensAsync`" block appears three times (`SaveConnection`, `Callback`, `ProviderTokenService.GetAccessTokenAsync`). Add `StoreTokensAsync(string providerId, TokenRefreshResult tokens)` to `IProviderTokenService` and compute the expiry in one place.
    - Replace the hardcoded `"spotify"`/`"youtube"` string checks (connect URLs, SaveConnection special case, `IsProviderReadyAsync`) with metadata on the provider abstraction (e.g. `DashboardUrl`, `SupportsApiKeyFallback`) so adding a provider doesn't require editing the controller.

15. **Move external-id extraction into the providers** (`SearchController`)
    - The controller parses Spotify/YouTube URLs to recover an external id (`ExtractExternalId`/`ExtractYouTubeVideoId`, ~50 lines). Providers already know their ids — have `SearchAsync` results carry `ExternalId` (extend the search result model) and delete the heuristics.

### Application / Domain / Persistence

16. **Delete orphaned and stub code**
    - Entities `AdminUser`, `RickRollTarget`, `SoundBoardEvent`, `SearchTerm`: unmapped by any feature — remove entities, `DbSet`s, and add one cleanup migration. (If any are planned features, note it in the plan doc and keep the entity out of the DbContext until built.)
    - ✅ Apple Music: removed in Phase 3 alongside the provider-enablement fix (item 10).
    - Remove `IMusicPlaybackController.PauseAsync`/`ResumeAsync` (no endpoint calls them) and `IMusicProviderRegistry.GetAllCatalogProviders()` (no callers), plus their provider implementations.
    - `TrackPlay` keeps both `IsSkipped` and `Status = Skipped` — redundant duplicated state; keep `Status` and derive/drop `IsSkipped`.

17. **Small logic fixes**
    - `TrackScoreService`: `AutoPlayCount += isAutoplay ? 0 : 1` is inverted and neither counter affects the score — either fix the increment _and_ use the counts in scoring, or delete both fields. Recommendation: fix the increment (`isAutoplay ? 1 : 0`) and keep the data; it's cheap and useful for future scoring.
    - `ExceededDailyLimitVetoRule`: count vetoes by `TrackPlayVeto.VetoedAt` date, not the track's `StartedAt`.
    - `QueueManager.Dequeue`: replace recursive self-call with a `while` loop.

---

## Phase 5 — Network access control and user identity

Two new requirements land here because they build on Phase 3's auth work and Phase 4's cleanup of the request payloads.

18. **Network access control — localhost for prototyping, config-only path to LAN/hosting** (Api)
    - Prototype/demo stance (current): keep the Api bound to `localhost:5080`. The whole demo runs on one device, which naturally mimics the restricted-network setup — nothing else can reach it.
    - Still ship the access-control middleware now (registered before routing so it also covers the SignalR hub and static assets): reject any request whose `RemoteIpAddress` is not loopback or inside an allowed CIDR list from a new `Security:AllowedNetworks` section, defaulting to loopback + the RFC 1918 private ranges (`10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`). On localhost every request arrives via loopback, so the demo exercises the exact enforcement path that will guard the LAN later — the check is real, not stubbed.
    - Later rollout is configuration only, no code changes:
      - **LAN phase:** change `Urls` to `http://0.0.0.0:5080` and (optionally) pin `Security:AllowedNetworks` to the office subnet.
      - **Hosted phase:** the CIDR allowlist stops being the right control once traffic goes through the host's edge; at that point front the app with the hosting provider's access controls and set `AllowedNetworks` to the proxy's addresses, using ASP.NET's forwarded-headers middleware with `KnownProxies` set. Do **not** trust `X-Forwarded-For` before then — otherwise the check is spoofable.
    - The Player keeps its `localhost` binding in every phase — only the Api ever talks to it, so it should never be network-visible (this complements the shared-secret auth from item 9).

19. **Domain-email identity for queueing and voting** (Api, Player contracts, frontend)
    - Today identity is a self-chosen display name stored in localStorage and sent in request bodies — anyone can impersonate anyone, which also makes the per-user queue/veto rules unenforceable. This item replaces that honor system.
    - Bind the `Organization` section to an `OrganizationOptions` class in the Api: `Name`, `Domain` (e.g. `contoso.com`). *(Partially done in Phase 2: `OrganizationOptions` now exists in Application with `Name`/`Domain`/`TimeZone` and is bound where `AddApplication()` runs — the Api still needs its own binding when item 19 lands.)*
    - Add a sign-in endpoint (`POST /api/session`): the user submits their work email and a display name; the Api validates the email's domain against `Organization:Domain` (case-insensitive, exact suffix match on `@domain`) and stores the identity in the same cookie session infrastructure the admin login already uses. Add `GET /api/session` for the frontend to restore state and `DELETE /api/session` to sign out.
    - Gate the write endpoints — queue a track, veto, skip, like — on that session, and derive `User` **server-side** from the session instead of trusting the request body. Remove the `User` field from the client-facing request contracts (the Api fills it in before proxying to the Player). Read-only endpoints (queue, now-playing, search) stay open to the LAN so a wall display doesn't need a login.
    - Frontend: replace the free-text username field on the profile page with the sign-in flow (email + display name), source `useProfile` from `GET /api/session`, and surface a sign-in prompt when a write returns 401.
    - Scope note: this validates the email's domain, it does not verify mailbox ownership. Combined with LAN-only access that is a reasonable bar for an office jukebox. If real verification is wanted later, add a magic-link email step behind the same session mechanism — the endpoint surface doesn't change.

20. **Follow-through on rules once identity is real**
    - `LimitNumberOfTracksQueuedByUser` and the veto daily limit now key off the session-derived email instead of a spoofable display name — verify the rules compare the canonical identity (email), not the display name, and add a test for the impersonation case (same display name, different email).

---

## Phase 6 — Verification

- `dotnet build` (0 warnings) and full test run, including the new tests from Phase 2.
- `tsc -b` and `oxlint` clean after the frontend deletion; `npm run build` succeeds.
- Manual smoke: fresh clone workflow — copy the dev settings example, run Player + Api + web, connect a provider, sign in with a domain email, queue, veto with the new id semantics, watch SignalR updates.
- Access-control checks (all runnable on one device): loopback requests succeed; an integration test that fakes a non-allowed `RemoteIpAddress` gets 403 (proves the middleware works before any LAN rollout); queueing without a session gets 401; signing in with a non-domain email is rejected.
- Re-run a dependency usage check (`npx depcheck` or grep) to confirm no removed package is still imported.

## Suggested sequencing

Each phase is an independent, reviewable commit/PR. Phase 1 is pure configuration (safe, immediate payoff). Phase 2 changes runtime behavior and should carry the new tests. Phase 3 items 9–11 touch both services and the frontend event names, so land them together. Phase 4 is large but mechanical deletion — do the frontend (item 12) and backend (items 13–17) as separate commits so reverts stay cheap. Phase 5 depends on Phase 3 (session/auth groundwork) and on Phase 4's contract typing (item 13) since it removes the `User` field from those same request records.
