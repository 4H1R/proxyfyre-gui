# ProxiFyre UI

A modern, [Proxifier](https://www.proxifier.com/)-inspired desktop UI for
[ProxiFyre](https://github.com/wiresock/proxifyre) — the open-source SOCKS5 proxifier for Windows
built on the [Windows Packet Filter](https://github.com/wiresock/ndisapi) driver.

ProxiFyre routes selected applications' traffic through a SOCKS5 proxy. It's powerful, but
configuration lives in a hand-edited `app-config.json` and the service is driven from the command
line. **ProxiFyre UI** puts a clean, native Windows front-end on top: manage proxy rules, control the
service, and read logs — no JSON editing, no terminal.

> Status: early. Core is fully tested; the UI compiles and runs on Windows. See
> [Roadmap](#roadmap).

## Features

- **Proxy rules** — map applications to SOCKS5 endpoints (with optional credentials and TCP/UDP
  selection) through a table editor. Writes ProxiFyre's `app-config.json` directly, preserving any
  fields the UI doesn't manage.
- **Excludes & LAN bypass** — maintain the bypass list and toggle `bypassLan`.
- **Service control** — install / start / stop / restart the ProxiFyre Windows service from a
  persistent status bar. Optional start-on-boot.
- **Live logs** — tail the ProxiFyre log with level filtering and search.
- **Guided setup** — detects missing prerequisites (WinpkFilter driver, VC++ runtime) and links to
  the installers.
- **Dark, Fluent design** — built with WinUI 3.

## Requirements

- Windows 10 (1809+) or Windows 11, x64
- [Windows Packet Filter (WinpkFilter)](https://github.com/wiresock/ndisapi/releases) driver
- Visual C++ 2022 runtime (x64)

The installer is self-contained — the .NET 8 runtime and the Windows App SDK runtime are bundled,
so no separate .NET install is needed. The app detects the driver and VC++ runtime on first launch
and guides you through installing them.

## Install

Grab the latest `ProxiFyreUI-Setup-*.exe` from the
[Releases](https://github.com/4H1R/proxyfyre-gui/releases) page and run it. The installer bundles
`ProxiFyre.exe`; you still need the WinpkFilter driver (the app links to it).

> The installer is currently unsigned, so Windows SmartScreen may warn on first run
> (More info → Run anyway).

## Architecture

Two projects, so as much logic as possible is testable off-Windows and in CI:

| Project | Target | Responsibility |
|---------|--------|----------------|
| `ProxiFyre.Core` | `net8.0` | All logic behind interfaces — config store, service controller, prereq checker, log tailer, path locator — plus the MVVM viewmodels. Fully unit-tested. |
| `ProxiFyre.UI` | `net8.0-windows` (WinUI 3) | Views + Windows-specific implementations (service host, registry/service probes) + DI wiring. |

Windows-only calls sit behind `ISystemProbe` / `IServiceHost`, so the viewmodels and core logic run
anywhere with a .NET 8 SDK. See [`docs/superpowers/specs`](docs/superpowers/specs) for the design.

## Build from source

```powershell
# Core library + tests (any OS with .NET 8 SDK)
dotnet test tests/ProxiFyre.Core.Tests/

# WinUI app (Windows only — needs Visual Studio's MSBuild for the MSIX/PRI tasks)
msbuild src/ProxiFyre.UI/ProxiFyre.UI.csproj -t:Restore -p:Configuration=Release -p:Platform=x64
msbuild src/ProxiFyre.UI/ProxiFyre.UI.csproj -p:Configuration=Release -p:Platform=x64
```

`global.json` pins the .NET 8 SDK. The WinUI app uses Windows App SDK 1.6 and must be built with
full MSBuild — `dotnet build` lacks the MSIX/PRI packaging tasks.

## CI/CD

- **CI** ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)) — every PR and push to `main`
  builds and tests Core, and compiles the WinUI app on `windows-latest`.
- **Release** ([`.github/workflows/release.yml`](.github/workflows/release.yml)) — pushing a `v*`
  tag publishes the app, bundles `ProxiFyre.exe`, builds the Inno Setup installer, and attaches it
  to a GitHub Release.

## Roadmap

- Log-parsed live activity view
- Signed installer / MSIX
- Verified end-to-end release pipeline

Driver-level and service integration behavior is validated manually — see
[`docs/manual-smoke-checklist.md`](docs/manual-smoke-checklist.md).

## Credits & licensing

- [ProxiFyre](https://github.com/wiresock/proxifyre) and
  [Windows Packet Filter](https://github.com/wiresock/ndisapi) by Vadim Smirnov / wiresock —
  bundled/depended-on under their respective licenses.
- This UI: license **TBD** — add a `LICENSE` file (MIT recommended) before wider distribution.
