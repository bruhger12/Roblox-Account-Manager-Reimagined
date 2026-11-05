# Roblox Account Manager Reimagined

A modern rebuild of [ic3w0lf22's Roblox Account Manager](https://github.com/ic3w0lf22/Roblox-Account-Manager): a Windows desktop tool for keeping several Roblox accounts signed in, checking which of them are online, browsing public servers and launching clients into a place as any of them.

This is an independent GPL-3.0 fork. It is not affiliated with Roblox Corporation.

## What changed

The original manager was a .NET Framework 4.7.2 WinForms app whose endpoints had drifted and whose UI had grown around a grid of dialogs. This fork is a .NET 9 WinForms application with a new shell, a current endpoint layer and no injected client code.

- Accounts, presence, thumbnails, server lists and authentication tickets use Roblox's current public endpoints.
- Sessions are protected with Windows DPAPI for the signed-in Windows user.
- The UI is drawn from scratch: no icon font, no image assets, no design system package.
- Multi-instance support only holds the two kernel objects Roblox itself uses. Nothing is patched into the client.

## Browser sign-in, restored

The original's most-missed feature was the disposable browser that adds an account just because you signed into it. It is back, with the same behaviour on a WebView2 host:

- **Sign in with browser** clears the cookie jar, opens `roblox.com/login` in a dedicated window and waits.
- When the session lands on Roblox after login, the window reads `.ROBLOSECURITY`, validates it with `users.roblox.com` and adds the account.
- The cookie jar is then wiped and the window hides. The next sign-in starts clean.
- **Open in browser** on any account seeds that stored cookie and opens `roblox.com/home` as that account.
- Closing the browser window always wipes cookies, so a disposed window cannot leak a session.

No password is requested or stored.

## Other account tools

- Paste a single cookie, or import many cookies at once (one per line) with per-session validation.
- Per-account tag and a local note, both searchable.
- Select several accounts and launch, mute, include or remove them as a batch.
- Recent places are remembered from successful launches and offered beside the place field.
- Presence refresh, avatar thumbnails, session revalidation and cookie copying.

## Launching and servers

- Launching trades the stored cookie for a fresh authentication ticket and starts `RobloxPlayerBeta.exe` with the modern `--app -t ... -j https://www.roblox.com/games/start?...` arguments.
- Public server discovery uses `games.roblox.com`, with population, ping and FPS shown as measured values.
- Multi-instance, fast flags, lowest graphics quality, window tiling and data folders live on one Tools page.
- Every account launch can be staggered, and batch launches can be confirmed first.

## Design

The visual system follows the owner's design vault rather than a generic dark dashboard:

- Near-black ground `#08080A`, bone ink `#E9E6E0`, one accent at a time, status colours only for status.
- Separations are hairlines and small tonal steps, with no gradients, glows or shadows on resting surfaces.
- Words use a grotesque; monospaced type is reserved for measured values such as user IDs, job IDs, ping, FPS and counts.
- Functional text is at least 11 px and running text at least 12 px.
- Every custom control has hover, keyboard focus and active states, and supports Tab, Space and Enter.

## Requirements

- Windows 10 or 11, x64.
- Roblox Player installed for the Windows user that runs the app.
- Microsoft Edge WebView2 runtime for the embedded browser. It ships with current Windows and with Microsoft Edge; if it is missing, the app still runs and the browser window explains what to install.
- .NET 9 SDK to build from source.

## Build

```bat
build.bat
```

or

```bat
dotnet publish "Roblox Account Manager Reimagined.sln" -c Release -r win-x64 --self-contained false -o dist
```

The only NuGet dependency is `Microsoft.Web.WebView2`. Everything else uses the base class library.

## Data

Everything is per Windows user, under `%APPDATA%\RobloxAccountManagerReimagined`:

- `accounts.json`: sessions protected with DPAPI, plus tags and notes.
- `settings.json`: launch, refresh, accent and recent-place preferences.
- `avatars\`: cached headshots.
- `browser\`: the disposable WebView2 profile.

Roblox's own files are only touched by the optional fast-flag and graphics-quality tools, which keep a `.ram-backup` beside anything they rewrite.

## Layout

```
src/RamReimagined/
  Core/        Roblox endpoints, launcher, DPAPI store, settings, paths
  UI/          Theme, shell, browser window, dialogs, custom controls
  UI/Pages/    Accounts, Servers, Tools, Settings, About
```

Legacy upstream sources are kept in the repository history and in the upstream project for reference.

## License

GPL-3.0, inherited from the original project. See `LICENSE`.

## Credits

Original project and account-manager design by [ic3w0lf22](https://github.com/ic3w0lf22). This fork rebuilds the client and UI while keeping the same purpose and licence.
