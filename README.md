# Roblox Account Manager Reimagined

A ground-up rebuild of [ic3w0lf22/Roblox-Account-Manager](https://github.com/ic3w0lf22/Roblox-Account-Manager)
(the original WinForms app, archived in 2024), updated for the Roblox client and
web APIs as they exist today and given a completely new interface.

The original sources are still in this fork under `RBX Alt Manager/` for
reference. The app you get when you build is the new one in `src/RamReimagined`.

```
dotnet build
dotnet run --project src/RamReimagined
```

---

## What changed

### Interface

The old app was a stack of WinForms designer dialogs with its own tab control and
theme editor. Reimagined is a single borderless window with:

- A **sidebar rail** — Accounts, Servers, Tools, Settings, About — with an accent
  highlight, brand mark and a live "Roblox client detected" status.
- A **page header** with a title, one-line explanation and the actions that belong
  to that page, so nothing important is hidden in a context menu.
- **Custom-painted account cards** — avatar, display name, handle, user ID, tag,
  presence dot and a presence chip, with select-on-click, an inline Launch button
  and a small menu for the rest. Cards are painted, not compiled from stock
  controls, so hover, selection and density are consistent.
- **A real server browser** — place ID, game name, player counts, ping, FPS and a
  Join action per server, instead of a generic list dialog.
- **Inline toasts** for every action (added, removed, launched, expired session)
  rather than modal message boxes.
- **Accent palette** (violet, cyan, emerald, magenta, amber) applied instantly
  across the whole shell; the choice is remembered.
- Consistent dark palette, rounded surfaces, vector icons drawn in code (no icon
  font, no image assets, no external theme files).

### Behaviour and APIs

| Area | Original | Reimagined |
|---|---|---|
| Session check | `users.roblox.com/v0` style calls, later removed | `GET users.roblox.com/v1/users/authenticated` |
| Launch | ticket + hand-built `assetgame` URL | ticket + `https://www.roblox.com/games/start?placeId=…&gameInstanceId=…` |
| CSRF | retried ad hoc | every POST retries once on the `403` CSRF challenge, token cached per cookie |
| Cookie refresh | not handled | refreshed `.ROBLOSECURITY` from `Set-Cookie` is written back to the store |
| Presence | polling loop in the UI thread | `POST presence.roblox.com/v1/presence/users`, batched per refresh, one viewer cookie |
| Avatars / icons | downloader with a settings page | cached thumbnail loader with per-session memory cache |
| Servers | list dialog | `games.roblox.com/v1/games/{place}/servers/Public` with population, ping and FPS |
| Storage | JSON with DPAPI | JSON with DPAPI (CurrentUser), atomic writes, corruption-safe load |
| Multi-instance | singleton objects in the main form | encapsulated lock with an explicit enable/disable state and status text |
| Fast flags | file patcher dialog | one-click preset write to the newest build's `ClientAppSettings.json`, with backup and clear |
| Graphics | manual settings | writes `SavedQualityLevel` into `GlobalBasicSettings_13.xml`, keeping a `.ram-backup` |
| Client windows | window tools | grid tiling across running `RobloxPlayerBeta` windows |

Notes on the modern launch path:

- Roblox rejects password logins from third-party tools, so accounts are added
  from an existing browser session. Paste `.ROBLOSECURITY` (DevTools →
  Application → Cookies → roblox.com) and the app validates it against
  `users.roblox.com` before storing anything.
- Tickets are requested with `POST auth.roblox.com/v1/authentication-ticket/`,
  including the `Referer`/`Origin` pair and an automatically retried CSRF token.
  The client is started as
  `RobloxPlayerBeta.exe --app -t <ticket> -j <start url> -b <tracker>`.
- Multi-instance support only creates the `ROBLOX_singletonMutex` and
  `ROBLOX_singletonEvent` kernel objects in the session namespace. Nothing is
  injected, no client file is patched, and no other process is touched. Roblox
  has described multi-instancing as potentially malicious behaviour, so this
  remains at your own risk.

---

## Screens

**Accounts** — search, select, launch. Each card shows live presence; the toolbar
takes a join place ID, so a batch launch is one click. The card menu holds copy
cookie, copy user ID, mute for batch launches, tag and remove.

**Servers** — load any place's public servers, see population, ping and FPS, then
drop every selected account into that exact server.

**Tools** — multi-instance lock, lowest graphics, recommended fast flags (and a
clear button), presence refresh, client window tiling and shortcuts to the Roblox
and app data folders.

**Settings** — default place, launch stagger, presence interval, server page size,
lowest graphics before launch, tiling, batch confirmation and the accent palette.
Everything is saved immediately to `%APPDATA%\RobloxAccountManagerReimagined`.

**About** — what this fork is, what it deliberately does not do, and the current
environment (client path, account file, multi-instance state).

---

## Layout

```
src/RamReimagined/
  Program.cs                 entry point
  Core/
    RobloxApi.cs             authenticated requests, CSRF retry, presence, servers, thumbnails
    RobloxLauncher.cs        ticket exchange, launch, fast flags, quality, tiling, singleton lock
    AccountStore.cs          account model, DPAPI store, cookie normalisation
    Protection.cs            CryptProtectData / CryptUnprotectData interop
    AppPaths.cs              per-user paths + settings persistence
  UI/
    MainForm.cs              borderless shell, rail, header, page routing, hit-test resizing
    Theme.cs                 palette, fonts, rounded geometry, text helpers
    AppState.cs              shared state, presence refresh, batch launch
    Dialogs.cs               styled modal sheets and the status toast
    Controls/                pill buttons, toggles, text fields, account card, server row, icons
    Pages/                   Accounts, Servers, Tools, Settings, About
```

No NuGet dependencies: WinForms, `System.Text.Json` and `System.Net.Http` from the
shared framework, plus `crypt32` interop for DPAPI.

---

## Requirements

- Windows 10/11
- .NET 9 SDK (or the .NET 9 desktop runtime for a published build)
- The Roblox client installed for the account you are signed in with

## Build

```
build.bat            # framework-dependent build into dist\
```

or:

```
dotnet publish src/RamReimagined -c Release -o dist
```

---

## Security and limits

- Cookies are encrypted with Windows DPAPI for the current Windows user. The
  account file is unreadable if copied to another user or machine.
- The app never asks for a password and never performs a password login.
- Sessions can expire (password change, Roblox invalidation). **Validate** in the
  header checks every stored session and reports which ones died; re-add those.
- No telemetry, no network calls other than the Roblox endpoints the features need.
- Windows-only by construction (DPAPI, Win32 window handling, WinForms).

## License

GPL-3.0, inherited from the upstream project. See `LICENSE`.
Original work © ic3w0lf22; this fork © its contributors.
Not affiliated with Roblox Corporation.
