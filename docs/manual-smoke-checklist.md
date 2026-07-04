# Manual Smoke Checklist (real Windows host)

Run after install, on a Windows 10/11 x64 machine. Covers what CI can't (kernel driver, service).

- [ ] Fresh VM without WinpkFilter: Dashboard shows driver + runtime as missing; Start is blocked.
- [ ] Install WinpkFilter + VC++ runtime; click Recheck → both show installed, "Ready to start".
- [ ] Add a proxy rule (chrome → 127.0.0.1:1080, TCP+UDP); Save → app-config.json updated correctly.
- [ ] Click Start → UAC prompt (app already elevated), service installs + starts; status pill → Running.
- [ ] Route chrome through a real SOCKS5 proxy; confirm traffic is proxied.
- [ ] Add an exclude + toggle Bypass LAN; Save; restart service; confirm behavior.
- [ ] Logs view streams new lines; level filter + search work; Open logs folder opens Explorer.
- [ ] Toggle "Start on boot"; reboot; confirm service auto-starts.
- [ ] Stop, then Uninstall service; status pill → Not installed.
- [ ] Edit an unknown field in app-config.json by hand; Save from UI; confirm the unknown field survives.
