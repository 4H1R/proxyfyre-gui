# ProxiFyre UI — Design Spec

**Date:** 2026-07-05
**Status:** Approved design, pending implementation plan

## Summary

A modern, Proxifier-inspired desktop UI for [ProxiFyre](https://github.com/wiresock/proxifyre)
(a SOCKS5 proxifier for Windows built on Windows Packet Filter). The UI lets a user install,
configure, and control ProxiFyre without hand-editing JSON or using the command line.

## Goals

- Edit ProxiFyre's `app-config.json` through a graphical interface.
- Control the ProxiFyre Windows service (install / start / stop / uninstall).
- View ProxiFyre logs.
- Guide the user through installing prerequisites (WinpkFilter driver, VC++ runtime) on first run.

## Non-Goals (v1)

- Live per-connection bandwidth / socket table (ProxiFyre exposes no live API — only a log file
  and the config). Deferred; would require upstream ProxiFyre changes or packet-level hooks.
- Log-parsed activity feed. Deferred.
- Child-process run mode. Service mode only for v1 (child-process toggle is a possible later add).
- Cross-platform. Windows 10/11 x64 only.

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Language / framework | C# + WinUI 3 (.NET 8) | Same ecosystem as ProxiFyre; native Windows Fluent design. |
| Scope | Config editor + service control + log viewer | Matches exactly what ProxiFyre exposes. |
| Binary + driver handling | Bundle ProxiFyre.exe + guided prereq setup | Turnkey feel; detect missing driver/runtime on first launch. |
| Run mode | Windows service | Proxying persists after UI closes; boot autostart; ProxiFyre supports it natively. |
| Main window layout | Sidebar + top status bar + bottom log strip (Layout "A") | Closest to Proxifier. |
| Theme | Fluent, dark default | Modern look. |

## Architecture

```
┌─────────────────────────────┐
│   ProxiFyre.UI (WinUI 3)     │  ← user runs this
│   • config editor            │
│   • service controller       │
│   • log viewer               │
└──────────┬───────────────────┘
           │ owns/edits          │ controls (elevated)
           ▼                     ▼
   app-config.json        ProxiFyre Windows service
   (in ProxiFyre dir)     (install/start/stop/uninstall)
           │                     │
           └──── /logs/*.log ◄────┘  ← UI tails these
```

### Modules (each: one job, well-defined interface, independently testable)

- **ConfigStore** — read / write / validate `app-config.json`. Pure model ↔ JSON. Preserves
  unknown/future fields on write. No UI dependency.
- **ServiceController** — wraps ProxiFyre service commands (`install`/`start`/`stop`/`uninstall`),
  queries state (running / stopped / not-installed), handles elevation.
- **PrereqChecker** — detects WinpkFilter driver + VC++ runtime; reports what's missing.
- **LogTailer** — watches `/logs`, streams new lines to the UI.
- **LocatorService** — resolves bundled ProxiFyre.exe, config, and logs paths.
- **UI layer (MVVM)** — thin WinUI views + viewmodels; binds to the modules. Swappable without
  touching logic.

### Elevation

The UI relaunches elevated (UAC) only when a privileged action is needed (service control, driver
install). Config editing requires no elevation.

## Screens (Layout A)

**Top status bar (always visible):** service state pill (`● Running` / `○ Stopped` /
`⚠ Not installed`) + `▶ Start` `⏹ Stop` `↻ Restart`. Buttons trigger elevation as needed.

**Bottom log strip (always visible):** last N lines from `/logs`, auto-scroll; click to expand
into the full Logs view.

**Sidebar sections:**

1. **Dashboard** — service state, prereq health (driver/runtime OK?), active rule count, quick
   start/stop. First run: the prereq setup wizard lives here.
2. **Proxy Rules** — table of rules; each row = appNames + SOCKS5 endpoint + protocols (TCP/UDP) +
   optional credentials. Add / edit / delete. Maps 1:1 to `proxies[]` in config.
3. **Excludes** — editable list of app names that bypass the proxy (`excludes[]`) + `bypassLan`
   toggle.
4. **Logs** — full log viewer: scrollback, level filter (Error/Warning/Info/Debug/All), search,
   open-logs-folder.
5. **Settings** — global `logLevel`, ProxiFyre path, autostart-on-boot toggle (service start type),
   UI theme.

**Save model:** edits stage in memory; explicit **Save** writes `app-config.json`. Changing config
while the service runs prompts "Restart service to apply?" (ProxiFyre reads config at start).

## Data Model

Mirrors ProxiFyre's `app-config.json` exactly.

```csharp
class AppConfig {
  string  LogLevel;          // Error|Warning|Info|Debug|All
  bool    BypassLan;
  List<ProxyRule> Proxies;
  List<string> Excludes;
}
class ProxyRule {
  List<string> AppNames;           // exe names or paths; "" = catch-all
  string  Socks5ProxyEndpoint;     // host:port, required
  string? Username;                // optional
  string? Password;                // optional
  List<string> SupportedProtocols; // TCP, UDP
}
```

Reference `app-config.json` (from ProxiFyre docs):

```json
{
  "logLevel": "Error",
  "bypassLan": true,
  "proxies": [
    {
      "appNames": ["chrome", "firefox"],
      "socks5ProxyEndpoint": "127.0.0.1:1080",
      "username": "optional_username",
      "password": "optional_password",
      "supportedProtocols": ["TCP", "UDP"]
    }
  ],
  "excludes": ["edge", "localservice.exe"]
}
```

- Round-trips 1:1 to `app-config.json`.
- Unknown/future fields preserved on write (don't clobber what the UI doesn't understand).

## Validation (ConfigStore)

- `socks5ProxyEndpoint` must be a valid `host:port`.
- `supportedProtocols` non-empty.
- Warn on empty `appNames` (catch-all) — allowed but flagged.
- Block save on hard errors; surface inline.

## Error Handling

- ProxiFyre.exe / config missing → LocatorService flags it; Dashboard prompts.
- Service command fails (denied elevation, not installed) → clear message + retry; no silent fail.
- Config file locked/corrupt → back up + warn; never lose user data.
- Prereq missing → Dashboard blocks Start and shows a guided fix.
- Credentials live only in `app-config.json` (ProxiFyre's own format, plaintext). The UI surfaces
  that reality and keeps no extra copies.

## Prerequisites (detected by PrereqChecker)

1. Windows Packet Filter (WinpkFilter) driver.
2. Visual Studio 2022 runtime libraries (matching architecture).
3. Windows Firewall rules allowing ProxiFyre.exe (surfaced as guidance).

## Testing

- **Unit:** ConfigStore (parse / write / validate / round-trip + unknown-field preservation),
  PrereqChecker (mock registry/filesystem), ServiceController (mock service API), LogTailer
  (temp files).
- **Viewmodels** testable without the WinUI runtime (MVVM).
- **Manual smoke checklist** for the real service + driver on Windows (kernel driver can't be
  fully mocked).
