# ProxiFyre UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A modern, Proxifier-inspired WinUI 3 desktop app to install, configure, and control ProxiFyre (SOCKS5 proxifier for Windows).

**Architecture:** Two projects. `ProxiFyre.Core` — plain `net8.0` class library holding all logic (config, service control, prereq detection, log tailing) behind interfaces; fully unit-tested. `ProxiFyre.UI` — WinUI 3 (`net8.0-windows10.0.19041.0`) MVVM app that binds to Core. Windows-only Core pieces (service, registry) sit behind interfaces so the rest is CI-testable on `windows-latest`.

**Tech Stack:** C#, .NET 8, WinUI 3 (Windows App SDK), CommunityToolkit.Mvvm, xUnit, GitHub Actions (windows-latest), Inno Setup.

**Environment note:** WinUI 3 builds only on Windows. The dev box for this repo is WSL2 Linux — it cannot compile the solution. All build/test commands below run on **GitHub Actions (windows-latest)** or a local Windows machine. `ProxiFyre.Core` + tests build on any OS with a .NET 8 SDK; the WinUI app does not.

---

## File Structure

```
proxyfyre-gui/
├─ ProxiFyre.sln
├─ src/
│  ├─ ProxiFyre.Core/
│  │  ├─ ProxiFyre.Core.csproj
│  │  ├─ Models/AppConfig.cs         # AppConfig, ProxyRule
│  │  ├─ Config/IConfigStore.cs      # interface
│  │  ├─ Config/ConfigStore.cs       # read/write/validate app-config.json
│  │  ├─ Config/ValidationResult.cs
│  │  ├─ Locate/ILocatorService.cs
│  │  ├─ Locate/LocatorService.cs    # find ProxiFyre.exe, config, logs
│  │  ├─ Service/IServiceController.cs
│  │  ├─ Service/ServiceController.cs # install/start/stop/uninstall + state
│  │  ├─ Service/ServiceState.cs
│  │  ├─ Prereq/IPrereqChecker.cs
│  │  ├─ Prereq/PrereqChecker.cs     # driver + runtime detection
│  │  ├─ Prereq/PrereqStatus.cs
│  │  └─ Logs/LogTailer.cs           # watch /logs, stream lines
│  └─ ProxiFyre.UI/
│     ├─ ProxiFyre.UI.csproj
│     ├─ App.xaml / App.xaml.cs
│     ├─ MainWindow.xaml / .cs       # Layout A shell
│     ├─ Services/AppServices.cs     # DI container wiring
│     ├─ ViewModels/*.cs
│     └─ Views/*.xaml                # Dashboard, Rules, Excludes, Logs, Settings
├─ tests/
│  └─ ProxiFyre.Core.Tests/
│     ├─ ProxiFyre.Core.Tests.csproj
│     └─ *.cs
├─ installer/
│  └─ proxifyre-ui.iss               # Inno Setup script
├─ .github/workflows/
│  ├─ ci.yml                         # build + test on PR/main
│  └─ release.yml                    # v* tag -> installer -> Release
└─ docs/
   └─ manual-smoke-checklist.md
```

Each Core file has one responsibility. Windows-specific calls (Windows service API, registry) live only in `ServiceController` and `PrereqChecker`, both behind interfaces so viewmodels and tests use fakes.

---

## Phase 0 — Scaffolding & CI

### Task 1: Create solution and projects

**Files:**
- Create: `ProxiFyre.sln`, `src/ProxiFyre.Core/ProxiFyre.Core.csproj`, `tests/ProxiFyre.Core.Tests/ProxiFyre.Core.Tests.csproj`

- [ ] **Step 1: Create solution + Core library + test project**

Run:
```bash
dotnet new sln -n ProxiFyre
dotnet new classlib -n ProxiFyre.Core -o src/ProxiFyre.Core -f net8.0
dotnet new xunit  -n ProxiFyre.Core.Tests -o tests/ProxiFyre.Core.Tests -f net8.0
rm src/ProxiFyre.Core/Class1.cs tests/ProxiFyre.Core.Tests/UnitTest1.cs
dotnet sln add src/ProxiFyre.Core/ProxiFyre.Core.csproj
dotnet sln add tests/ProxiFyre.Core.Tests/ProxiFyre.Core.Tests.csproj
dotnet add tests/ProxiFyre.Core.Tests/ProxiFyre.Core.Tests.csproj reference src/ProxiFyre.Core/ProxiFyre.Core.csproj
```

- [ ] **Step 2: Verify it builds**

Run: `dotnet build ProxiFyre.sln`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "chore: scaffold solution, core library, and test project"
```

### Task 2: CI workflow (build + test)

**Files:**
- Create: `.github/workflows/ci.yml`

- [ ] **Step 1: Write the CI workflow**

```yaml
name: CI
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
jobs:
  build-test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Restore
        run: dotnet restore ProxiFyre.sln
      - name: Build Core + Tests
        run: dotnet build tests/ProxiFyre.Core.Tests/ProxiFyre.Core.Tests.csproj -c Release --no-restore
      - name: Test
        run: dotnet test tests/ProxiFyre.Core.Tests/ProxiFyre.Core.Tests.csproj -c Release --no-build --verbosity normal
```

Note: CI builds only Core + tests here. The WinUI app build is added in Task 12 once the UI project exists (a second job), so early PRs aren't blocked by an incomplete UI project.

- [ ] **Step 2: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: build and test core library on windows-latest"
```

---

## Phase 1 — Core library (TDD)

### Task 3: Config models

**Files:**
- Create: `src/ProxiFyre.Core/Models/AppConfig.cs`
- Test: `tests/ProxiFyre.Core.Tests/AppConfigTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using ProxiFyre.Core.Models;
using Xunit;

public class AppConfigTests
{
    [Fact]
    public void NewConfig_HasEmptyCollections_AndDefaults()
    {
        var cfg = new AppConfig();
        Assert.Equal("Error", cfg.LogLevel);
        Assert.True(cfg.BypassLan);
        Assert.Empty(cfg.Proxies);
        Assert.Empty(cfg.Excludes);
    }

    [Fact]
    public void ProxyRule_DefaultsAreEmptyNotNull()
    {
        var rule = new ProxyRule();
        Assert.NotNull(rule.AppNames);
        Assert.NotNull(rule.SupportedProtocols);
        Assert.Equal("", rule.Socks5ProxyEndpoint);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter AppConfigTests`
Expected: FAIL — `AppConfig` / `ProxyRule` not found.

- [ ] **Step 3: Write the models**

```csharp
namespace ProxiFyre.Core.Models;

public sealed class AppConfig
{
    public string LogLevel { get; set; } = "Error";
    public bool BypassLan { get; set; } = true;
    public List<ProxyRule> Proxies { get; set; } = new();
    public List<string> Excludes { get; set; } = new();
}

public sealed class ProxyRule
{
    public List<string> AppNames { get; set; } = new();
    public string Socks5ProxyEndpoint { get; set; } = "";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public List<string> SupportedProtocols { get; set; } = new() { "TCP", "UDP" };
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter AppConfigTests`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(core): add AppConfig and ProxyRule models"
```

### Task 4: ConfigStore — read & parse

**Files:**
- Create: `src/ProxiFyre.Core/Config/IConfigStore.cs`, `src/ProxiFyre.Core/Config/ConfigStore.cs`
- Test: `tests/ProxiFyre.Core.Tests/ConfigStoreReadTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using ProxiFyre.Core.Config;
using Xunit;

public class ConfigStoreReadTests
{
    private static string WriteTemp(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void Read_ParsesAllFields()
    {
        var path = WriteTemp("""
        {
          "logLevel": "Debug",
          "bypassLan": false,
          "proxies": [
            { "appNames": ["chrome","firefox"], "socks5ProxyEndpoint": "127.0.0.1:1080",
              "username": "u", "password": "p", "supportedProtocols": ["TCP","UDP"] }
          ],
          "excludes": ["edge"]
        }
        """);
        var cfg = new ConfigStore().Read(path);
        Assert.Equal("Debug", cfg.LogLevel);
        Assert.False(cfg.BypassLan);
        Assert.Single(cfg.Proxies);
        Assert.Equal("127.0.0.1:1080", cfg.Proxies[0].Socks5ProxyEndpoint);
        Assert.Equal(2, cfg.Proxies[0].AppNames.Count);
        Assert.Equal("u", cfg.Proxies[0].Username);
        Assert.Equal(new[] { "edge" }, cfg.Excludes);
        File.Delete(path);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter ConfigStoreReadTests`
Expected: FAIL — `ConfigStore` not found.

- [ ] **Step 3: Write the interface and reader**

`IConfigStore.cs`:
```csharp
using ProxiFyre.Core.Models;

namespace ProxiFyre.Core.Config;

public interface IConfigStore
{
    AppConfig Read(string path);
    void Write(string path, AppConfig config);
    ValidationResult Validate(AppConfig config);
}
```

`ConfigStore.cs`:
```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using ProxiFyre.Core.Models;

namespace ProxiFyre.Core.Config;

public sealed class ConfigStore : IConfigStore
{
    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AppConfig Read(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppConfig>(json, ReadOpts) ?? new AppConfig();
    }

    public void Write(string path, AppConfig config) => throw new NotImplementedException();
    public ValidationResult Validate(AppConfig config) => throw new NotImplementedException();
}
```

Note: `AppConfig`/`ProxyRule` need `[JsonPropertyName]` matching ProxiFyre's camelCase keys. Add attributes now:
```csharp
// in AppConfig.cs, add: using System.Text.Json.Serialization;
// annotate each property, e.g.:
//   [JsonPropertyName("logLevel")] public string LogLevel ...
//   [JsonPropertyName("bypassLan")] public bool BypassLan ...
//   [JsonPropertyName("proxies")] public List<ProxyRule> Proxies ...
//   [JsonPropertyName("excludes")] public List<string> Excludes ...
//   [JsonPropertyName("appNames")] public List<string> AppNames ...
//   [JsonPropertyName("socks5ProxyEndpoint")] public string Socks5ProxyEndpoint ...
//   [JsonPropertyName("username")] public string? Username ...
//   [JsonPropertyName("password")] public string? Password ...
//   [JsonPropertyName("supportedProtocols")] public List<string> SupportedProtocols ...
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter ConfigStoreReadTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(core): ConfigStore reads app-config.json"
```

### Task 5: ConfigStore — write & preserve unknown fields

**Files:**
- Modify: `src/ProxiFyre.Core/Config/ConfigStore.cs`
- Test: `tests/ProxiFyre.Core.Tests/ConfigStoreWriteTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using System.Text.Json;
using ProxiFyre.Core.Config;
using ProxiFyre.Core.Models;
using Xunit;

public class ConfigStoreWriteTests
{
    private static string TempPath() =>
        Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");

    [Fact]
    public void Write_RoundTrips()
    {
        var store = new ConfigStore();
        var cfg = new AppConfig { LogLevel = "Info", BypassLan = false };
        cfg.Proxies.Add(new ProxyRule { Socks5ProxyEndpoint = "127.0.0.1:1080" });
        var path = TempPath();

        store.Write(path, cfg);
        var back = store.Read(path);

        Assert.Equal("Info", back.LogLevel);
        Assert.False(back.BypassLan);
        Assert.Single(back.Proxies);
        File.Delete(path);
    }

    [Fact]
    public void Write_PreservesUnknownTopLevelFields()
    {
        var path = TempPath();
        File.WriteAllText(path, """
        { "logLevel": "Error", "bypassLan": true, "proxies": [], "excludes": [],
          "futureFlag": "keepme" }
        """);
        var store = new ConfigStore();
        var cfg = store.Read(path);
        cfg.LogLevel = "Debug";

        store.Write(path, cfg);

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal("Debug", doc.RootElement.GetProperty("logLevel").GetString());
        Assert.Equal("keepme", doc.RootElement.GetProperty("futureFlag").GetString());
        File.Delete(path);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter ConfigStoreWriteTests`
Expected: FAIL — `Write` throws `NotImplementedException`.

- [ ] **Step 3: Implement Write with unknown-field merge**

Replace the `Write` method in `ConfigStore.cs`:
```csharp
    private static readonly JsonSerializerOptions WriteOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly HashSet<string> KnownKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "logLevel", "bypassLan", "proxies", "excludes"
    };

    public void Write(string path, AppConfig config)
    {
        // Serialize known model to a JSON object.
        var node = JsonSerializer.SerializeToNode(config, WriteOpts)!.AsObject();

        // Merge any unknown top-level fields from the existing file so we never clobber them.
        if (File.Exists(path))
        {
            var existing = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
            if (existing is not null)
                foreach (var kv in existing)
                    if (!KnownKeys.Contains(kv.Key) && !node.ContainsKey(kv.Key))
                        node[kv.Key] = kv.Value?.DeepClone();
        }

        var tmp = path + ".tmp";
        File.WriteAllText(tmp, node.ToJsonString(WriteOpts));
        File.Copy(tmp, path, overwrite: true);   // write-then-swap: never lose the original on crash
        File.Delete(tmp);
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter ConfigStoreWriteTests`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(core): ConfigStore writes config and preserves unknown fields"
```

### Task 6: ConfigStore — validation

**Files:**
- Create: `src/ProxiFyre.Core/Config/ValidationResult.cs`
- Modify: `src/ProxiFyre.Core/Config/ConfigStore.cs`
- Test: `tests/ProxiFyre.Core.Tests/ConfigStoreValidateTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using ProxiFyre.Core.Config;
using ProxiFyre.Core.Models;
using Xunit;

public class ConfigStoreValidateTests
{
    private static AppConfig Valid()
    {
        var c = new AppConfig();
        c.Proxies.Add(new ProxyRule
        {
            AppNames = { "chrome" },
            Socks5ProxyEndpoint = "127.0.0.1:1080",
            SupportedProtocols = { "TCP" }
        });
        return c;
    }

    [Fact]
    public void Valid_Config_HasNoErrors()
    {
        var r = new ConfigStore().Validate(Valid());
        Assert.True(r.IsValid);
        Assert.Empty(r.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("nohost")]
    [InlineData("127.0.0.1:notaport")]
    [InlineData("127.0.0.1:99999")]
    public void Invalid_Endpoint_IsError(string endpoint)
    {
        var c = Valid();
        c.Proxies[0].Socks5ProxyEndpoint = endpoint;
        var r = new ConfigStore().Validate(c);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("endpoint", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EmptyProtocols_IsError()
    {
        var c = Valid();
        c.Proxies[0].SupportedProtocols.Clear();
        var r = new ConfigStore().Validate(c);
        Assert.False(r.IsValid);
    }

    [Fact]
    public void EmptyAppNames_IsWarningNotError()
    {
        var c = Valid();
        c.Proxies[0].AppNames.Clear();
        var r = new ConfigStore().Validate(c);
        Assert.True(r.IsValid);
        Assert.NotEmpty(r.Warnings);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter ConfigStoreValidateTests`
Expected: FAIL — `ValidationResult` missing / `Validate` throws.

- [ ] **Step 3: Implement ValidationResult and Validate**

`ValidationResult.cs`:
```csharp
namespace ProxiFyre.Core.Config;

public sealed class ValidationResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public bool IsValid => Errors.Count == 0;
}
```

Replace `Validate` in `ConfigStore.cs`:
```csharp
    public ValidationResult Validate(AppConfig config)
    {
        var r = new ValidationResult();
        for (var i = 0; i < config.Proxies.Count; i++)
        {
            var p = config.Proxies[i];
            var label = $"rule #{i + 1}";

            if (!IsValidEndpoint(p.Socks5ProxyEndpoint))
                r.Errors.Add($"{label}: invalid SOCKS5 endpoint '{p.Socks5ProxyEndpoint}' (expected host:port).");
            if (p.SupportedProtocols.Count == 0)
                r.Errors.Add($"{label}: at least one protocol (TCP/UDP) is required.");
            if (p.AppNames.Count == 0 || p.AppNames.All(string.IsNullOrWhiteSpace))
                r.Warnings.Add($"{label}: no app names — this rule matches all apps (catch-all).");
        }
        return r;
    }

    private static bool IsValidEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint)) return false;
        var idx = endpoint.LastIndexOf(':');
        if (idx <= 0 || idx == endpoint.Length - 1) return false;
        var host = endpoint[..idx];
        var portText = endpoint[(idx + 1)..];
        if (string.IsNullOrWhiteSpace(host)) return false;
        return int.TryParse(portText, out var port) && port is > 0 and <= 65535;
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter ConfigStoreValidateTests`
Expected: PASS (all cases).

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(core): ConfigStore validates rules"
```

### Task 7: LocatorService — resolve ProxiFyre paths

**Files:**
- Create: `src/ProxiFyre.Core/Locate/ILocatorService.cs`, `src/ProxiFyre.Core/Locate/LocatorService.cs`
- Test: `tests/ProxiFyre.Core.Tests/LocatorServiceTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using ProxiFyre.Core.Locate;
using Xunit;

public class LocatorServiceTests
{
    [Fact]
    public void Resolve_FromBaseDir_FindsBundledExeAndConfigAndLogs()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(baseDir);
        File.WriteAllText(Path.Combine(baseDir, "ProxiFyre.exe"), "");
        File.WriteAllText(Path.Combine(baseDir, "app-config.json"), "{}");

        var loc = new LocatorService(baseDir);

        Assert.True(loc.ExeExists);
        Assert.Equal(Path.Combine(baseDir, "ProxiFyre.exe"), loc.ExePath);
        Assert.Equal(Path.Combine(baseDir, "app-config.json"), loc.ConfigPath);
        Assert.Equal(Path.Combine(baseDir, "logs"), loc.LogsDir);

        Directory.Delete(baseDir, recursive: true);
    }

    [Fact]
    public void Resolve_MissingExe_ReportsNotFound()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(baseDir);
        var loc = new LocatorService(baseDir);
        Assert.False(loc.ExeExists);
        Directory.Delete(baseDir, recursive: true);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter LocatorServiceTests`
Expected: FAIL — `LocatorService` not found.

- [ ] **Step 3: Implement the locator**

`ILocatorService.cs`:
```csharp
namespace ProxiFyre.Core.Locate;

public interface ILocatorService
{
    string ExePath { get; }
    string ConfigPath { get; }
    string LogsDir { get; }
    bool ExeExists { get; }
    bool ConfigExists { get; }
}
```

`LocatorService.cs`:
```csharp
namespace ProxiFyre.Core.Locate;

public sealed class LocatorService : ILocatorService
{
    public LocatorService(string baseDir)
    {
        ExePath = Path.Combine(baseDir, "ProxiFyre.exe");
        ConfigPath = Path.Combine(baseDir, "app-config.json");
        LogsDir = Path.Combine(baseDir, "logs");
    }

    // Default: alongside the running UI (installer drops ProxiFyre.exe next to the app).
    public LocatorService() : this(AppContext.BaseDirectory) { }

    public string ExePath { get; }
    public string ConfigPath { get; }
    public string LogsDir { get; }
    public bool ExeExists => File.Exists(ExePath);
    public bool ConfigExists => File.Exists(ConfigPath);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter LocatorServiceTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(core): LocatorService resolves ProxiFyre paths"
```

### Task 8: PrereqChecker — detect driver & runtime

Windows-specific probes sit behind an `ISystemProbe` interface so the logic is testable with a fake.

**Files:**
- Create: `src/ProxiFyre.Core/Prereq/PrereqStatus.cs`, `src/ProxiFyre.Core/Prereq/IPrereqChecker.cs`, `src/ProxiFyre.Core/Prereq/PrereqChecker.cs`
- Test: `tests/ProxiFyre.Core.Tests/PrereqCheckerTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using ProxiFyre.Core.Prereq;
using Xunit;

public class PrereqCheckerTests
{
    private sealed class FakeProbe : ISystemProbe
    {
        public bool Driver, Runtime;
        public bool IsWinpkFilterInstalled() => Driver;
        public bool IsVcRuntimeInstalled() => Runtime;
    }

    [Fact]
    public void AllPresent_IsReady()
    {
        var s = new PrereqChecker(new FakeProbe { Driver = true, Runtime = true }).Check();
        Assert.True(s.DriverInstalled);
        Assert.True(s.RuntimeInstalled);
        Assert.True(s.AllSatisfied);
        Assert.Empty(s.Missing);
    }

    [Fact]
    public void MissingDriver_ListsIt()
    {
        var s = new PrereqChecker(new FakeProbe { Driver = false, Runtime = true }).Check();
        Assert.False(s.AllSatisfied);
        Assert.Contains(s.Missing, m => m.Contains("Packet Filter", StringComparison.OrdinalIgnoreCase));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter PrereqCheckerTests`
Expected: FAIL — types not found.

- [ ] **Step 3: Implement status, interface, checker**

`PrereqStatus.cs`:
```csharp
namespace ProxiFyre.Core.Prereq;

public sealed class PrereqStatus
{
    public bool DriverInstalled { get; init; }
    public bool RuntimeInstalled { get; init; }
    public List<string> Missing { get; } = new();
    public bool AllSatisfied => Missing.Count == 0;
}
```

`IPrereqChecker.cs`:
```csharp
namespace ProxiFyre.Core.Prereq;

public interface ISystemProbe
{
    bool IsWinpkFilterInstalled();
    bool IsVcRuntimeInstalled();
}

public interface IPrereqChecker
{
    PrereqStatus Check();
}
```

`PrereqChecker.cs`:
```csharp
namespace ProxiFyre.Core.Prereq;

public sealed class PrereqChecker : IPrereqChecker
{
    private readonly ISystemProbe _probe;
    public PrereqChecker(ISystemProbe probe) => _probe = probe;

    public PrereqStatus Check()
    {
        var driver = _probe.IsWinpkFilterInstalled();
        var runtime = _probe.IsVcRuntimeInstalled();
        var status = new PrereqStatus { DriverInstalled = driver, RuntimeInstalled = runtime };
        if (!driver) status.Missing.Add("Windows Packet Filter (WinpkFilter) driver");
        if (!runtime) status.Missing.Add("Visual C++ 2022 Runtime");
        return status;
    }
}
```

Note: the real `ISystemProbe` implementation (querying the service list / registry for the driver and VC++ runtime) is Windows-only and lives in the UI project as `WindowsSystemProbe` (Task 12). It is not unit-tested in CI; it is covered by the manual smoke checklist.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter PrereqCheckerTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(core): PrereqChecker detects driver and runtime"
```

### Task 9: ServiceController — service commands & state

The actual service calls (`ProxiFyre.exe install/start/stop/uninstall`, querying Windows service state) go through an `IServiceHost` interface. `ServiceController` holds the orchestration logic and is tested with a fake host.

**Files:**
- Create: `src/ProxiFyre.Core/Service/ServiceState.cs`, `src/ProxiFyre.Core/Service/IServiceController.cs`, `src/ProxiFyre.Core/Service/ServiceController.cs`
- Test: `tests/ProxiFyre.Core.Tests/ServiceControllerTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using ProxiFyre.Core.Service;
using Xunit;

public class ServiceControllerTests
{
    private sealed class FakeHost : IServiceHost
    {
        public ServiceState State = ServiceState.NotInstalled;
        public List<string> Calls = new();
        public ServiceState Query() => State;
        public void Install() { Calls.Add("install"); State = ServiceState.Stopped; }
        public void Uninstall() { Calls.Add("uninstall"); State = ServiceState.NotInstalled; }
        public void Start() { Calls.Add("start"); State = ServiceState.Running; }
        public void Stop() { Calls.Add("stop"); State = ServiceState.Stopped; }
    }

    [Fact]
    public void Start_WhenNotInstalled_InstallsThenStarts()
    {
        var host = new FakeHost { State = ServiceState.NotInstalled };
        var ctl = new ServiceController(host);
        ctl.Start();
        Assert.Equal(new[] { "install", "start" }, host.Calls);
        Assert.Equal(ServiceState.Running, ctl.CurrentState);
    }

    [Fact]
    public void Start_WhenStopped_JustStarts()
    {
        var host = new FakeHost { State = ServiceState.Stopped };
        var ctl = new ServiceController(host);
        ctl.Start();
        Assert.Equal(new[] { "start" }, host.Calls);
    }

    [Fact]
    public void Restart_StopsThenStarts()
    {
        var host = new FakeHost { State = ServiceState.Running };
        var ctl = new ServiceController(host);
        ctl.Restart();
        Assert.Equal(new[] { "stop", "start" }, host.Calls);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter ServiceControllerTests`
Expected: FAIL — types not found.

- [ ] **Step 3: Implement state, interfaces, controller**

`ServiceState.cs`:
```csharp
namespace ProxiFyre.Core.Service;

public enum ServiceState { NotInstalled, Stopped, Running, Unknown }
```

`IServiceController.cs`:
```csharp
namespace ProxiFyre.Core.Service;

public interface IServiceHost
{
    ServiceState Query();
    void Install();
    void Uninstall();
    void Start();
    void Stop();
}

public interface IServiceController
{
    ServiceState CurrentState { get; }
    ServiceState Refresh();
    void Start();
    void Stop();
    void Restart();
    void Uninstall();
}
```

`ServiceController.cs`:
```csharp
namespace ProxiFyre.Core.Service;

public sealed class ServiceController : IServiceController
{
    private readonly IServiceHost _host;
    public ServiceController(IServiceHost host)
    {
        _host = host;
        CurrentState = host.Query();
    }

    public ServiceState CurrentState { get; private set; }

    public ServiceState Refresh() => CurrentState = _host.Query();

    public void Start()
    {
        if (CurrentState == ServiceState.NotInstalled) _host.Install();
        _host.Start();
        Refresh();
    }

    public void Stop()
    {
        if (CurrentState == ServiceState.Running) _host.Stop();
        Refresh();
    }

    public void Restart()
    {
        if (CurrentState == ServiceState.Running) _host.Stop();
        _host.Start();
        Refresh();
    }

    public void Uninstall()
    {
        if (CurrentState == ServiceState.Running) _host.Stop();
        _host.Uninstall();
        Refresh();
    }
}
```

Note: the real `IServiceHost` (`WindowsServiceHost`) shells out to `ProxiFyre.exe` with elevation and reads Windows service state via `System.ServiceProcess.ServiceController`. It lives in the UI project (Task 12), is Windows-only, and is covered by the manual smoke checklist.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter ServiceControllerTests`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(core): ServiceController orchestrates service lifecycle"
```

### Task 10: LogTailer — stream new log lines

**Files:**
- Create: `src/ProxiFyre.Core/Logs/LogTailer.cs`
- Test: `tests/ProxiFyre.Core.Tests/LogTailerTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using ProxiFyre.Core.Logs;
using Xunit;

public class LogTailerTests
{
    [Fact]
    public void ReadNew_ReturnsOnlyLinesAppendedSinceLastCall()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".log");
        File.WriteAllText(path, "line1\nline2\n");
        var tailer = new LogTailer(path);

        var first = tailer.ReadNew().ToList();
        Assert.Equal(new[] { "line1", "line2" }, first);

        File.AppendAllText(path, "line3\n");
        var second = tailer.ReadNew().ToList();
        Assert.Equal(new[] { "line3" }, second);

        File.Delete(path);
    }

    [Fact]
    public void ReadNew_MissingFile_ReturnsEmpty()
    {
        var tailer = new LogTailer(Path.Combine(Path.GetTempPath(), "nope.log"));
        Assert.Empty(tailer.ReadNew());
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter LogTailerTests`
Expected: FAIL — `LogTailer` not found.

- [ ] **Step 3: Implement the tailer**

`LogTailer.cs`:
```csharp
using System.Text;

namespace ProxiFyre.Core.Logs;

// Tracks a byte offset into a growing log file and returns only newly appended lines.
public sealed class LogTailer
{
    private readonly string _path;
    private long _offset;

    public LogTailer(string path) => _path = path;

    public IEnumerable<string> ReadNew()
    {
        if (!File.Exists(_path)) yield break;

        using var stream = new FileStream(
            _path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        if (stream.Length < _offset) _offset = 0; // file was truncated/rotated
        stream.Seek(_offset, SeekOrigin.Begin);

        using var reader = new StreamReader(stream, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) is not null)
            yield return line;

        _offset = stream.Position;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter LogTailerTests`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(core): LogTailer streams new log lines"
```

**Decision — viewmodels live in `ProxiFyre.Core`:** so they are unit-tested in CI without the WinUI runtime, viewmodels use `CommunityToolkit.Mvvm` (targets netstandard2.0, works in `net8.0`) and are placed in `src/ProxiFyre.Core/ViewModels/`. They reference only Core interfaces and the MVVM base types — never `Microsoft.UI.Xaml`. WinUI Views bind to them.

- [ ] **Add CommunityToolkit.Mvvm to Core** (needed from Task 13 on)

Run: `dotnet add src/ProxiFyre.Core/ProxiFyre.Core.csproj package CommunityToolkit.Mvvm --version 8.2.2`
Then commit: `git add -A && git commit -m "chore(core): add CommunityToolkit.Mvvm for viewmodels"`

---

## Phase 2 — WinUI app

> These tasks build only on Windows (CI `windows-latest` or a local Windows machine). They are not runnable on the Linux dev box.

### Task 11: Scaffold the WinUI 3 project (unpackaged)

**Files:**
- Create: `src/ProxiFyre.UI/ProxiFyre.UI.csproj`, `src/ProxiFyre.UI/app.manifest`, `src/ProxiFyre.UI/App.xaml`, `src/ProxiFyre.UI/App.xaml.cs`, `src/ProxiFyre.UI/MainWindow.xaml`, `src/ProxiFyre.UI/MainWindow.xaml.cs`

- [ ] **Step 1: Create the csproj**

`ProxiFyre.UI.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <RootNamespace>ProxiFyre.UI</RootNamespace>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <Platforms>x64</Platforms>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <UseWinUI>true</UseWinUI>
    <WindowsPackageType>None</WindowsPackageType>
    <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
    <Nullable>enable</Nullable>
    <EnableDefaultCssItems>false</EnableDefaultCssItems>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.5.240627000" />
    <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.22621.3233" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ProxiFyre.Core\ProxiFyre.Core.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create app.manifest (request admin at launch)**

Because the app controls a Windows service and installs a driver, request elevation on launch (simplest reliable model; the spec's "elevate when needed" is satisfied by launching elevated, and config editing still works elevated).

`app.manifest`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v3">
    <security>
      <requestedPrivileges>
        <requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>
```

- [ ] **Step 3: Create App.xaml / App.xaml.cs / MainWindow**

`App.xaml`:
```xml
<Application
    x:Class="ProxiFyre.UI.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    RequestedTheme="Dark">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

`App.xaml.cs`:
```csharp
using Microsoft.UI.Xaml;

namespace ProxiFyre.UI;

public partial class App : Application
{
    private Window? _window;
    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
```

`MainWindow.xaml` (placeholder shell — Layout A comes in Task 13):
```xml
<Window
    x:Class="ProxiFyre.UI.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <TextBlock Text="ProxiFyre UI" VerticalAlignment="Center" HorizontalAlignment="Center"/>
    </Grid>
</Window>
```

`MainWindow.xaml.cs`:
```csharp
using Microsoft.UI.Xaml;

namespace ProxiFyre.UI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "ProxiFyre";
    }
}
```

- [ ] **Step 4: Add project to solution and build**

Run:
```bash
dotnet sln add src/ProxiFyre.UI/ProxiFyre.UI.csproj
dotnet build src/ProxiFyre.UI/ProxiFyre.UI.csproj -c Release
```
Expected: `Build succeeded` (Windows only).

- [ ] **Step 5: Add a UI build job to CI**

Append to `.github/workflows/ci.yml` under `jobs:`:
```yaml
  build-ui:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Build WinUI app
        run: dotnet build src/ProxiFyre.UI/ProxiFyre.UI.csproj -c Release
```

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(ui): scaffold unpackaged WinUI 3 app + CI build job"
```

### Task 12: Windows implementations + DI container

Provides the real, Windows-only implementations of the Core interfaces and wires up dependency injection. No unit tests (Windows APIs); covered by the manual smoke checklist.

**Files:**
- Create: `src/ProxiFyre.UI/Platform/WindowsServiceHost.cs`, `src/ProxiFyre.UI/Platform/WindowsSystemProbe.cs`, `src/ProxiFyre.UI/Services/AppServices.cs`
- Modify: `src/ProxiFyre.UI/ProxiFyre.UI.csproj` (add `System.ServiceProcess.ServiceController` package), `src/ProxiFyre.UI/App.xaml.cs`

- [ ] **Step 1: Add the ServiceProcess package**

Run: `dotnet add src/ProxiFyre.UI/ProxiFyre.UI.csproj package System.ServiceProcess.ServiceController --version 8.0.1`

- [ ] **Step 2: WindowsServiceHost — shell out to ProxiFyre.exe, read service state**

`WindowsServiceHost.cs`:
```csharp
using System.Diagnostics;
using System.ServiceProcess;
using ProxiFyre.Core.Locate;
using ProxiFyre.Core.Service;

namespace ProxiFyre.UI.Platform;

// ProxiFyre registers its Windows service under this name (verify on the smoke host; adjust if it differs).
public sealed class WindowsServiceHost : IServiceHost
{
    private const string ServiceName = "ProxiFyre";
    private readonly ILocatorService _locator;
    public WindowsServiceHost(ILocatorService locator) => _locator = locator;

    public ServiceState Query()
    {
        try
        {
            using var sc = new System.ServiceProcess.ServiceController(ServiceName);
            return sc.Status switch
            {
                ServiceControllerStatus.Running or ServiceControllerStatus.StartPending
                    => ServiceState.Running,
                ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending
                    => ServiceState.Stopped,
                _ => ServiceState.Unknown
            };
        }
        catch (InvalidOperationException)
        {
            return ServiceState.NotInstalled; // service not found
        }
    }

    public void Install()   => RunExe("install");
    public void Uninstall() => RunExe("uninstall");
    public void Start()     => RunExe("start");
    public void Stop()      => RunExe("stop");

    private void RunExe(string verb)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _locator.ExePath,
            Arguments = verb,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(_locator.ExePath)!
        };
        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to launch ProxiFyre.exe {verb}");
        proc.WaitForExit(30_000);
        if (proc.ExitCode != 0)
            throw new InvalidOperationException(
                $"ProxiFyre.exe {verb} failed (exit {proc.ExitCode}): {proc.StandardError.ReadToEnd()}");
    }
}
```

- [ ] **Step 3: WindowsSystemProbe — detect driver & VC++ runtime**

`WindowsSystemProbe.cs`:
```csharp
using Microsoft.Win32;
using System.ServiceProcess;
using ProxiFyre.Core.Prereq;

namespace ProxiFyre.UI.Platform;

public sealed class WindowsSystemProbe : ISystemProbe
{
    // WinpkFilter installs a kernel service named "ndisrd".
    public bool IsWinpkFilterInstalled()
    {
        try
        {
            using var sc = new System.ServiceProcess.ServiceController("ndisrd");
            _ = sc.Status; // touching Status throws if the service does not exist
            return true;
        }
        catch { return false; }
    }

    // VC++ 2015-2022 x64 runtime writes this registry key with Installed=1.
    public bool IsVcRuntimeInstalled()
    {
        using var key = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64");
        return key?.GetValue("Installed") is int installed && installed == 1;
    }
}
```

- [ ] **Step 4: AppServices — DI container**

`AppServices.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;
using ProxiFyre.Core.Config;
using ProxiFyre.Core.Locate;
using ProxiFyre.Core.Prereq;
using ProxiFyre.Core.Service;
using ProxiFyre.UI.Platform;

namespace ProxiFyre.UI.Services;

public static class AppServices
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ILocatorService>(_ => new LocatorService());
        services.AddSingleton<IConfigStore, ConfigStore>();
        services.AddSingleton<ISystemProbe, WindowsSystemProbe>();
        services.AddSingleton<IPrereqChecker>(sp =>
            new PrereqChecker(sp.GetRequiredService<ISystemProbe>()));
        services.AddSingleton<IServiceHost, WindowsServiceHost>();
        services.AddSingleton<IServiceController>(sp =>
            new ServiceController(sp.GetRequiredService<IServiceHost>()));

        // Viewmodels registered in Task 13+.
        RegisterViewModels(services);

        return services.BuildServiceProvider();
    }

    // Extended in later tasks (partial-like; edit this method as viewmodels are added).
    private static void RegisterViewModels(IServiceCollection services)
    {
    }
}
```

- [ ] **Step 5: Expose the container from App**

Replace `App.xaml.cs` body:
```csharp
using Microsoft.UI.Xaml;
using ProxiFyre.UI.Services;

namespace ProxiFyre.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    private Window? _window;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Services = AppServices.Build();
        _window = new MainWindow();
        _window.Activate();
    }
}
```

- [ ] **Step 6: Build and commit**

Run: `dotnet build src/ProxiFyre.UI/ProxiFyre.UI.csproj -c Release` → Expected: `Build succeeded`.
```bash
git add -A && git commit -m "feat(ui): windows service host, system probe, and DI container"
```

### Task 13: ShellViewModel (TDD) + Layout A window shell

**Files:**
- Create: `src/ProxiFyre.Core/ViewModels/ShellViewModel.cs`
- Test: `tests/ProxiFyre.Core.Tests/ShellViewModelTests.cs`
- Modify: `src/ProxiFyre.UI/MainWindow.xaml`, `src/ProxiFyre.UI/MainWindow.xaml.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using ProxiFyre.Core.Service;
using ProxiFyre.Core.ViewModels;
using Xunit;

public class ShellViewModelTests
{
    private sealed class FakeController : IServiceController
    {
        public ServiceState CurrentState { get; set; } = ServiceState.Stopped;
        public int Starts, Stops;
        public ServiceState Refresh() => CurrentState;
        public void Start() { Starts++; CurrentState = ServiceState.Running; }
        public void Stop() { Stops++; CurrentState = ServiceState.Stopped; }
        public void Restart() { Stops++; Starts++; }
        public void Uninstall() { CurrentState = ServiceState.NotInstalled; }
    }

    [Fact]
    public void StatusText_ReflectsState()
    {
        var vm = new ShellViewModel(new FakeController { CurrentState = ServiceState.Running });
        vm.RefreshState();
        Assert.Equal("Running", vm.StatusText);
        Assert.True(vm.IsRunning);
    }

    [Fact]
    public void StartCommand_StartsService_AndUpdatesStatus()
    {
        var ctl = new FakeController { CurrentState = ServiceState.Stopped };
        var vm = new ShellViewModel(ctl);
        vm.StartCommand.Execute(null);
        Assert.Equal(1, ctl.Starts);
        Assert.Equal("Running", vm.StatusText);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter ShellViewModelTests`
Expected: FAIL — `ShellViewModel` not found.

- [ ] **Step 3: Implement ShellViewModel**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxiFyre.Core.Service;

namespace ProxiFyre.Core.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly IServiceController _controller;

    public ShellViewModel(IServiceController controller)
    {
        _controller = controller;
        RefreshState();
    }

    [ObservableProperty] private string _statusText = "Unknown";
    [ObservableProperty] private bool _isRunning;

    public void RefreshState()
    {
        var state = _controller.Refresh();
        IsRunning = state == ServiceState.Running;
        StatusText = state switch
        {
            ServiceState.Running => "Running",
            ServiceState.Stopped => "Stopped",
            ServiceState.NotInstalled => "Not installed",
            _ => "Unknown"
        };
    }

    [RelayCommand] private void Start() { _controller.Start(); RefreshState(); }
    [RelayCommand] private void Stop() { _controller.Stop(); RefreshState(); }
    [RelayCommand] private void Restart() { _controller.Restart(); RefreshState(); }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter ShellViewModelTests`
Expected: PASS (2 tests).

- [ ] **Step 5: Build the Layout A shell (MainWindow)**

`MainWindow.xaml` — top status bar, `NavigationView` sidebar, bottom log strip:
```xml
<Window
    x:Class="ProxiFyre.UI.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:muxc="using:Microsoft.UI.Xaml.Controls">
    <Grid RowDefinitions="Auto,*,Auto">

        <!-- Top status bar -->
        <Grid Row="0" Padding="12,8" ColumnDefinitions="Auto,*,Auto,Auto,Auto"
              Background="{ThemeResource CardBackgroundFillColorDefaultBrush}">
            <StackPanel Orientation="Horizontal" Spacing="8" VerticalAlignment="Center">
                <Ellipse Width="10" Height="10"
                         Fill="{x:Bind ShellVm.IsRunning, Mode=OneWay, Converter={StaticResource StateBrush}}"/>
                <TextBlock Text="{x:Bind ShellVm.StatusText, Mode=OneWay}" VerticalAlignment="Center"/>
            </StackPanel>
            <Button Grid.Column="2" Content="Start" Margin="4,0"
                    Command="{x:Bind ShellVm.StartCommand}"/>
            <Button Grid.Column="3" Content="Stop" Margin="4,0"
                    Command="{x:Bind ShellVm.StopCommand}"/>
            <Button Grid.Column="4" Content="Restart" Margin="4,0"
                    Command="{x:Bind ShellVm.RestartCommand}"/>
        </Grid>

        <!-- Sidebar + content -->
        <muxc:NavigationView Grid.Row="1" x:Name="Nav"
                             PaneDisplayMode="Left" IsBackButtonVisible="Collapsed"
                             IsSettingsVisible="False"
                             SelectionChanged="Nav_SelectionChanged">
            <muxc:NavigationView.MenuItems>
                <muxc:NavigationViewItem Content="Dashboard"   Tag="dashboard" Icon="Home"     IsSelected="True"/>
                <muxc:NavigationViewItem Content="Proxy Rules" Tag="rules"     Icon="Filter"/>
                <muxc:NavigationViewItem Content="Excludes"    Tag="excludes"  Icon="Remove"/>
                <muxc:NavigationViewItem Content="Logs"        Tag="logs"      Icon="Document"/>
                <muxc:NavigationViewItem Content="Settings"    Tag="settings"  Icon="Setting"/>
            </muxc:NavigationView.MenuItems>
            <Frame x:Name="ContentFrame"/>
        </muxc:NavigationView>

        <!-- Bottom log strip -->
        <Border Grid.Row="2" Height="90" Padding="12,6"
                Background="{ThemeResource LayerFillColorDefaultBrush}"
                BorderThickness="0,1,0,0"
                BorderBrush="{ThemeResource DividerStrokeColorDefaultBrush}">
            <ScrollViewer x:Name="LogStripScroller" VerticalScrollBarVisibility="Auto">
                <TextBlock x:Name="LogStripText" FontFamily="Consolas" FontSize="12"
                           TextWrapping="NoWrap"/>
            </ScrollViewer>
        </Border>
    </Grid>
</Window>
```

`MainWindow.xaml.cs` — resolve VM from DI, wire nav:
```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProxiFyre.Core.ViewModels;
using ProxiFyre.UI.Views;

namespace ProxiFyre.UI;

public sealed partial class MainWindow : Window
{
    public ShellViewModel ShellVm { get; }

    public MainWindow()
    {
        ShellVm = App.Services.GetRequiredService<ShellViewModel>();
        InitializeComponent();
        Title = "ProxiFyre";
        ContentFrame.Navigate(typeof(DashboardPage));
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var tag = (args.SelectedItem as NavigationViewItem)?.Tag as string;
        var page = tag switch
        {
            "dashboard" => typeof(DashboardPage),
            "rules"     => typeof(RulesPage),
            "excludes"  => typeof(ExcludesPage),
            "logs"      => typeof(LogsPage),
            "settings"  => typeof(SettingsPage),
            _           => typeof(DashboardPage)
        };
        ContentFrame.Navigate(page);
    }
}
```

Register `ShellViewModel` — edit `AppServices.RegisterViewModels`:
```csharp
    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddTransient<ProxiFyre.Core.ViewModels.ShellViewModel>();
    }
```

Add the `StateBrush` converter resource (create `src/ProxiFyre.UI/Converters/BoolToBrushConverter.cs` returning green when true, gray when false, and reference it in `App.xaml` resources):
```csharp
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace ProxiFyre.UI.Converters;

public sealed class BoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, string l) =>
        new SolidColorBrush((value is bool b && b) ? Colors.LimeGreen : Colors.Gray);
    public object ConvertBack(object v, Type t, object p, string l) => throw new NotSupportedException();
}
```
In `App.xaml`, inside `<ResourceDictionary>`, add:
```xml
<converters:BoolToBrushConverter x:Key="StateBrush"
    xmlns:converters="using:ProxiFyre.UI.Converters"/>
```

Note: `DashboardPage`, `RulesPage`, `ExcludesPage`, `LogsPage`, `SettingsPage` are created in Tasks 14–18. Until then, comment out the unresolved `ContentFrame.Navigate`/page references or implement Task 14 first — the build won't succeed until at least `DashboardPage` exists. Recommended: proceed straight to Task 14.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(ui): ShellViewModel + Layout A window shell"
```

### Task 14: Dashboard (viewmodel TDD + page)

**Files:**
- Create: `src/ProxiFyre.Core/ViewModels/DashboardViewModel.cs`, `src/ProxiFyre.UI/Views/DashboardPage.xaml(.cs)`
- Test: `tests/ProxiFyre.Core.Tests/DashboardViewModelTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using ProxiFyre.Core.Config;
using ProxiFyre.Core.Locate;
using ProxiFyre.Core.Models;
using ProxiFyre.Core.Prereq;
using ProxiFyre.Core.ViewModels;
using Xunit;

public class DashboardViewModelTests
{
    private sealed class FakeChecker : IPrereqChecker
    {
        public PrereqStatus Status = new() { DriverInstalled = true, RuntimeInstalled = true };
        public PrereqStatus Check() => Status;
    }
    private sealed class FakeStore : IConfigStore
    {
        public AppConfig Config = new();
        public AppConfig Read(string p) => Config;
        public void Write(string p, AppConfig c) { }
        public ValidationResult Validate(AppConfig c) => new();
    }
    private sealed class FakeLocator : ILocatorService
    {
        public string ExePath => "x"; public string ConfigPath => "c"; public string LogsDir => "l";
        public bool ExeExists => true; public bool ConfigExists => true;
    }

    [Fact]
    public void Load_SetsRuleCountAndPrereqReady()
    {
        var store = new FakeStore();
        store.Config.Proxies.Add(new ProxyRule());
        store.Config.Proxies.Add(new ProxyRule());
        var vm = new DashboardViewModel(new FakeChecker(), store, new FakeLocator());
        vm.Load();
        Assert.Equal(2, vm.RuleCount);
        Assert.True(vm.PrereqReady);
        Assert.Empty(vm.MissingPrereqs);
    }

    [Fact]
    public void Load_MissingPrereq_SetsNotReady()
    {
        var checker = new FakeChecker { Status = new PrereqStatus { DriverInstalled = false } };
        checker.Status.Missing.Add("Windows Packet Filter (WinpkFilter) driver");
        var vm = new DashboardViewModel(checker, new FakeStore(), new FakeLocator());
        vm.Load();
        Assert.False(vm.PrereqReady);
        Assert.NotEmpty(vm.MissingPrereqs);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter DashboardViewModelTests`
Expected: FAIL — `DashboardViewModel` not found.

- [ ] **Step 3: Implement DashboardViewModel**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ProxiFyre.Core.Config;
using ProxiFyre.Core.Locate;
using ProxiFyre.Core.Prereq;

namespace ProxiFyre.Core.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IPrereqChecker _checker;
    private readonly IConfigStore _store;
    private readonly ILocatorService _locator;

    public DashboardViewModel(IPrereqChecker checker, IConfigStore store, ILocatorService locator)
    {
        _checker = checker; _store = store; _locator = locator;
    }

    [ObservableProperty] private int _ruleCount;
    [ObservableProperty] private bool _prereqReady;
    public ObservableCollection<string> MissingPrereqs { get; } = new();

    public void Load()
    {
        MissingPrereqs.Clear();
        var status = _checker.Check();
        PrereqReady = status.AllSatisfied;
        foreach (var m in status.Missing) MissingPrereqs.Add(m);

        RuleCount = _locator.ConfigExists ? _store.Read(_locator.ConfigPath).Proxies.Count : 0;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter DashboardViewModelTests`
Expected: PASS (2 tests).

- [ ] **Step 5: Create DashboardPage**

`DashboardPage.xaml`:
```xml
<Page
    x:Class="ProxiFyre.UI.Views.DashboardPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel Padding="24" Spacing="16">
        <TextBlock Text="Dashboard" Style="{ThemeResource TitleTextBlockStyle}"/>
        <Border Padding="16" CornerRadius="8"
                Background="{ThemeResource CardBackgroundFillColorDefaultBrush}">
            <StackPanel Spacing="6">
                <TextBlock Text="Prerequisites" Style="{ThemeResource SubtitleTextBlockStyle}"/>
                <TextBlock Text="All prerequisites installed."
                           Visibility="{x:Bind Vm.PrereqReady, Mode=OneWay}"/>
                <ItemsControl ItemsSource="{x:Bind Vm.MissingPrereqs, Mode=OneWay}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate x:DataType="x:String">
                            <TextBlock Text="{x:Bind}" Foreground="OrangeRed"/>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </StackPanel>
        </Border>
        <Border Padding="16" CornerRadius="8"
                Background="{ThemeResource CardBackgroundFillColorDefaultBrush}">
            <StackPanel Spacing="4">
                <TextBlock Text="Active proxy rules" Style="{ThemeResource SubtitleTextBlockStyle}"/>
                <TextBlock Text="{x:Bind Vm.RuleCount, Mode=OneWay}"
                           Style="{ThemeResource TitleLargeTextBlockStyle}"/>
            </StackPanel>
        </Border>
    </StackPanel>
</Page>
```

`DashboardPage.xaml.cs`:
```csharp
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProxiFyre.Core.ViewModels;

namespace ProxiFyre.UI.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel Vm { get; }
    public DashboardPage()
    {
        Vm = App.Services.GetRequiredService<DashboardViewModel>();
        InitializeComponent();
        Vm.Load();
    }
}
```

Register in `AppServices.RegisterViewModels`:
```csharp
        services.AddTransient<ProxiFyre.Core.ViewModels.DashboardViewModel>();
```

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(ui): dashboard view with prereq + rule summary"
```

### Task 15: Proxy Rules (viewmodel TDD + CRUD page)

**Files:**
- Create: `src/ProxiFyre.Core/ViewModels/RuleItemViewModel.cs`, `src/ProxiFyre.Core/ViewModels/RulesViewModel.cs`, `src/ProxiFyre.UI/Views/RulesPage.xaml(.cs)`
- Test: `tests/ProxiFyre.Core.Tests/RulesViewModelTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using ProxiFyre.Core.Config;
using ProxiFyre.Core.Models;
using ProxiFyre.Core.ViewModels;
using Xunit;

public class RulesViewModelTests
{
    private sealed class CapturingStore : IConfigStore
    {
        public AppConfig Saved = new();
        public AppConfig ToLoad = new();
        public AppConfig Read(string p) => ToLoad;
        public void Write(string p, AppConfig c) => Saved = c;
        public ValidationResult Validate(AppConfig c) => new ConfigStore().Validate(c);
    }

    private static RulesViewModel Make(CapturingStore store) =>
        new(store, configPath: "cfg.json");

    [Fact]
    public void Load_PopulatesRulesFromConfig()
    {
        var store = new CapturingStore();
        store.ToLoad.Proxies.Add(new ProxyRule { Socks5ProxyEndpoint = "127.0.0.1:1080" });
        var vm = Make(store);
        vm.Load();
        Assert.Single(vm.Rules);
        Assert.Equal("127.0.0.1:1080", vm.Rules[0].Endpoint);
    }

    [Fact]
    public void AddRule_ThenSave_WritesConfig()
    {
        var store = new CapturingStore();
        var vm = Make(store);
        vm.Load();
        vm.AddRuleCommand.Execute(null);
        vm.Rules[0].Endpoint = "10.0.0.1:1080";
        vm.Rules[0].AppNamesText = "chrome, firefox";
        vm.Rules[0].Tcp = true; vm.Rules[0].Udp = false;
        vm.SaveCommand.Execute(null);

        Assert.Single(store.Saved.Proxies);
        Assert.Equal("10.0.0.1:1080", store.Saved.Proxies[0].Socks5ProxyEndpoint);
        Assert.Equal(new[] { "chrome", "firefox" }, store.Saved.Proxies[0].AppNames);
        Assert.Equal(new[] { "TCP" }, store.Saved.Proxies[0].SupportedProtocols);
    }

    [Fact]
    public void DeleteRule_RemovesIt()
    {
        var store = new CapturingStore();
        store.ToLoad.Proxies.Add(new ProxyRule { Socks5ProxyEndpoint = "127.0.0.1:1080" });
        var vm = Make(store);
        vm.Load();
        vm.DeleteRuleCommand.Execute(vm.Rules[0]);
        Assert.Empty(vm.Rules);
    }

    [Fact]
    public void Save_WithInvalidEndpoint_SetsErrorAndDoesNotWrite()
    {
        var store = new CapturingStore();
        var vm = Make(store);
        vm.Load();
        vm.AddRuleCommand.Execute(null);
        vm.Rules[0].Endpoint = "bad";
        vm.SaveCommand.Execute(null);
        Assert.False(string.IsNullOrEmpty(vm.ErrorText));
        Assert.Empty(store.Saved.Proxies); // untouched
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter RulesViewModelTests`
Expected: FAIL — types not found.

- [ ] **Step 3: Implement RuleItemViewModel and RulesViewModel**

`RuleItemViewModel.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using ProxiFyre.Core.Models;

namespace ProxiFyre.Core.ViewModels;

public partial class RuleItemViewModel : ObservableObject
{
    [ObservableProperty] private string _appNamesText = "";
    [ObservableProperty] private string _endpoint = "";
    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private bool _tcp = true;
    [ObservableProperty] private bool _udp = true;

    public static RuleItemViewModel FromModel(ProxyRule r) => new()
    {
        AppNamesText = string.Join(", ", r.AppNames),
        Endpoint = r.Socks5ProxyEndpoint,
        Username = r.Username ?? "",
        Password = r.Password ?? "",
        Tcp = r.SupportedProtocols.Contains("TCP"),
        Udp = r.SupportedProtocols.Contains("UDP"),
    };

    public ProxyRule ToModel()
    {
        var rule = new ProxyRule
        {
            AppNames = AppNamesText.Split(',', StringSplitOptions.RemoveEmptyEntries
                        | StringSplitOptions.TrimEntries).ToList(),
            Socks5ProxyEndpoint = Endpoint.Trim(),
            Username = string.IsNullOrWhiteSpace(Username) ? null : Username,
            Password = string.IsNullOrWhiteSpace(Password) ? null : Password,
            SupportedProtocols = new()
        };
        if (Tcp) rule.SupportedProtocols.Add("TCP");
        if (Udp) rule.SupportedProtocols.Add("UDP");
        return rule;
    }
}
```

`RulesViewModel.cs`:
```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxiFyre.Core.Config;

namespace ProxiFyre.Core.ViewModels;

public partial class RulesViewModel : ObservableObject
{
    private readonly IConfigStore _store;
    private readonly string _configPath;

    public RulesViewModel(IConfigStore store, string configPath)
    {
        _store = store; _configPath = configPath;
    }

    public ObservableCollection<RuleItemViewModel> Rules { get; } = new();
    [ObservableProperty] private string _errorText = "";

    public void Load()
    {
        Rules.Clear();
        var cfg = _store.Read(_configPath);
        foreach (var r in cfg.Proxies) Rules.Add(RuleItemViewModel.FromModel(r));
    }

    [RelayCommand] private void AddRule() => Rules.Add(new RuleItemViewModel());

    [RelayCommand] private void DeleteRule(RuleItemViewModel item) => Rules.Remove(item);

    [RelayCommand]
    private void Save()
    {
        ErrorText = "";
        var cfg = _store.Read(_configPath);      // preserve logLevel/bypassLan/excludes/unknown fields
        cfg.Proxies = Rules.Select(r => r.ToModel()).ToList();

        var result = _store.Validate(cfg);
        if (!result.IsValid)
        {
            ErrorText = string.Join(Environment.NewLine, result.Errors);
            return;
        }
        _store.Write(_configPath, cfg);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter RulesViewModelTests`
Expected: PASS (4 tests).

- [ ] **Step 5: Create RulesPage**

`RulesPage.xaml`:
```xml
<Page
    x:Class="ProxiFyre.UI.Views.RulesPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:vm="using:ProxiFyre.Core.ViewModels">
    <Grid Padding="24" RowDefinitions="Auto,*,Auto">
        <StackPanel Orientation="Horizontal" Spacing="12">
            <TextBlock Text="Proxy Rules" Style="{ThemeResource TitleTextBlockStyle}"/>
            <Button Content="Add rule" Command="{x:Bind Vm.AddRuleCommand}"/>
            <Button Content="Save" Style="{ThemeResource AccentButtonStyle}"
                    Command="{x:Bind Vm.SaveCommand}"/>
        </StackPanel>

        <ListView Grid.Row="1" Margin="0,12" ItemsSource="{x:Bind Vm.Rules}">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="vm:RuleItemViewModel">
                    <Border Padding="12" Margin="0,4" CornerRadius="6"
                            Background="{ThemeResource CardBackgroundFillColorDefaultBrush}">
                        <StackPanel Spacing="6">
                            <TextBox Header="Apps (comma-separated)"
                                     Text="{x:Bind AppNamesText, Mode=TwoWay}"/>
                            <TextBox Header="SOCKS5 endpoint (host:port)"
                                     Text="{x:Bind Endpoint, Mode=TwoWay}"/>
                            <StackPanel Orientation="Horizontal" Spacing="12">
                                <TextBox Header="Username" Text="{x:Bind Username, Mode=TwoWay}" Width="160"/>
                                <PasswordBox Header="Password" Password="{x:Bind Password, Mode=TwoWay}" Width="160"/>
                                <CheckBox Content="TCP" IsChecked="{x:Bind Tcp, Mode=TwoWay}" VerticalAlignment="Bottom"/>
                                <CheckBox Content="UDP" IsChecked="{x:Bind Udp, Mode=TwoWay}" VerticalAlignment="Bottom"/>
                            </StackPanel>
                        </StackPanel>
                    </Border>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>

        <TextBlock Grid.Row="2" Foreground="OrangeRed" TextWrapping="Wrap"
                   Text="{x:Bind Vm.ErrorText, Mode=OneWay}"/>
    </Grid>
</Page>
```

Note: a per-row Delete button binding to `DeleteRuleCommand` needs an element-name binding to the page VM; add a `Button Content="Delete"` in the DataTemplate with
`Command="{Binding DataContext.DeleteRuleCommand, ElementName=RootPage}"`
`CommandParameter="{x:Bind}"` and give the `Page` `x:Name="RootPage"`. (Kept out of the template above for readability — add during implementation.)

`RulesPage.xaml.cs`:
```csharp
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProxiFyre.Core.Config;
using ProxiFyre.Core.Locate;
using ProxiFyre.Core.ViewModels;

namespace ProxiFyre.UI.Views;

public sealed partial class RulesPage : Page
{
    public RulesViewModel Vm { get; }
    public RulesPage()
    {
        var store = App.Services.GetRequiredService<IConfigStore>();
        var locator = App.Services.GetRequiredService<ILocatorService>();
        Vm = new RulesViewModel(store, locator.ConfigPath);
        InitializeComponent();
        Vm.Load();
    }
}
```

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(ui): proxy rules CRUD view"
```

### Task 16: Excludes (viewmodel TDD + page)

**Files:**
- Create: `src/ProxiFyre.Core/ViewModels/ExcludesViewModel.cs`, `src/ProxiFyre.UI/Views/ExcludesPage.xaml(.cs)`
- Test: `tests/ProxiFyre.Core.Tests/ExcludesViewModelTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using ProxiFyre.Core.Config;
using ProxiFyre.Core.Models;
using ProxiFyre.Core.ViewModels;
using Xunit;

public class ExcludesViewModelTests
{
    private sealed class Store : IConfigStore
    {
        public AppConfig ToLoad = new();
        public AppConfig Saved = new();
        public AppConfig Read(string p) => ToLoad;
        public void Write(string p, AppConfig c) => Saved = c;
        public ValidationResult Validate(AppConfig c) => new();
    }

    [Fact]
    public void Load_PopulatesExcludesAndBypassLan()
    {
        var store = new Store { ToLoad = new AppConfig { BypassLan = false } };
        store.ToLoad.Excludes.Add("edge");
        var vm = new ExcludesViewModel(store, "cfg.json");
        vm.Load();
        Assert.Contains("edge", vm.Excludes);
        Assert.False(vm.BypassLan);
    }

    [Fact]
    public void AddAndSave_PersistsExcludesAndBypassLan()
    {
        var store = new Store();
        var vm = new ExcludesViewModel(store, "cfg.json");
        vm.Load();
        vm.NewExclude = "chrome.exe";
        vm.AddCommand.Execute(null);
        vm.BypassLan = true;
        vm.SaveCommand.Execute(null);
        Assert.Contains("chrome.exe", store.Saved.Excludes);
        Assert.True(store.Saved.BypassLan);
    }
}
```

- [ ] **Step 2: Run to verify fail**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter ExcludesViewModelTests` → FAIL (type missing).

- [ ] **Step 3: Implement ExcludesViewModel**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxiFyre.Core.Config;

namespace ProxiFyre.Core.ViewModels;

public partial class ExcludesViewModel : ObservableObject
{
    private readonly IConfigStore _store;
    private readonly string _configPath;

    public ExcludesViewModel(IConfigStore store, string configPath)
    {
        _store = store; _configPath = configPath;
    }

    public ObservableCollection<string> Excludes { get; } = new();
    [ObservableProperty] private bool _bypassLan = true;
    [ObservableProperty] private string _newExclude = "";

    public void Load()
    {
        Excludes.Clear();
        var cfg = _store.Read(_configPath);
        foreach (var e in cfg.Excludes) Excludes.Add(e);
        BypassLan = cfg.BypassLan;
    }

    [RelayCommand]
    private void Add()
    {
        var v = NewExclude.Trim();
        if (v.Length > 0 && !Excludes.Contains(v)) Excludes.Add(v);
        NewExclude = "";
    }

    [RelayCommand] private void Remove(string item) => Excludes.Remove(item);

    [RelayCommand]
    private void Save()
    {
        var cfg = _store.Read(_configPath);
        cfg.Excludes = Excludes.ToList();
        cfg.BypassLan = BypassLan;
        _store.Write(_configPath, cfg);
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter ExcludesViewModelTests` → PASS (2 tests).

- [ ] **Step 5: Create ExcludesPage**

`ExcludesPage.xaml`:
```xml
<Page
    x:Class="ProxiFyre.UI.Views.ExcludesPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel Padding="24" Spacing="12">
        <TextBlock Text="Excludes" Style="{ThemeResource TitleTextBlockStyle}"/>
        <ToggleSwitch Header="Bypass LAN (10/8, 172.16/12, 192.168/16)"
                      IsOn="{x:Bind Vm.BypassLan, Mode=TwoWay}"/>
        <StackPanel Orientation="Horizontal" Spacing="8">
            <TextBox PlaceholderText="app name to exclude" Width="240"
                     Text="{x:Bind Vm.NewExclude, Mode=TwoWay}"/>
            <Button Content="Add" Command="{x:Bind Vm.AddCommand}"/>
            <Button Content="Save" Style="{ThemeResource AccentButtonStyle}"
                    Command="{x:Bind Vm.SaveCommand}"/>
        </StackPanel>
        <ListView ItemsSource="{x:Bind Vm.Excludes}"/>
    </StackPanel>
</Page>
```

`ExcludesPage.xaml.cs`:
```csharp
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProxiFyre.Core.Config;
using ProxiFyre.Core.Locate;
using ProxiFyre.Core.ViewModels;

namespace ProxiFyre.UI.Views;

public sealed partial class ExcludesPage : Page
{
    public ExcludesViewModel Vm { get; }
    public ExcludesPage()
    {
        var store = App.Services.GetRequiredService<IConfigStore>();
        var locator = App.Services.GetRequiredService<ILocatorService>();
        Vm = new ExcludesViewModel(store, locator.ConfigPath);
        InitializeComponent();
        Vm.Load();
    }
}
```

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(ui): excludes + bypassLan view"
```

### Task 17: Logs (viewmodel TDD + page)

**Files:**
- Create: `src/ProxiFyre.Core/ViewModels/LogsViewModel.cs`, `src/ProxiFyre.UI/Views/LogsPage.xaml(.cs)`
- Test: `tests/ProxiFyre.Core.Tests/LogsViewModelTests.cs`

- [ ] **Step 1: Write the failing test** (filter logic is the testable part)

```csharp
using ProxiFyre.Core.ViewModels;
using Xunit;

public class LogsViewModelTests
{
    [Fact]
    public void Filter_BySearchText_ReturnsMatchingLines()
    {
        var vm = new LogsViewModel();
        vm.Append("2026 [Info] proxy started for chrome");
        vm.Append("2026 [Error] connect failed 10.0.0.1");
        vm.SearchText = "chrome";
        Assert.Single(vm.FilteredLines);
        Assert.Contains("chrome", vm.FilteredLines[0]);
    }

    [Fact]
    public void Filter_ByLevel_ReturnsMatchingLevel()
    {
        var vm = new LogsViewModel();
        vm.Append("2026 [Info] a");
        vm.Append("2026 [Error] b");
        vm.LevelFilter = "Error";
        Assert.Single(vm.FilteredLines);
        Assert.Contains("[Error]", vm.FilteredLines[0]);
    }

    [Fact]
    public void Filter_All_ReturnsEverything()
    {
        var vm = new LogsViewModel();
        vm.Append("[Info] a"); vm.Append("[Error] b");
        vm.LevelFilter = "All"; vm.SearchText = "";
        Assert.Equal(2, vm.FilteredLines.Count);
    }
}
```

- [ ] **Step 2: Run to verify fail**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter LogsViewModelTests` → FAIL (type missing).

- [ ] **Step 3: Implement LogsViewModel**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProxiFyre.Core.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private readonly List<string> _all = new();

    public ObservableCollection<string> FilteredLines { get; } = new();

    [ObservableProperty] private string _levelFilter = "All";
    [ObservableProperty] private string _searchText = "";

    partial void OnLevelFilterChanged(string value) => Reapply();
    partial void OnSearchTextChanged(string value) => Reapply();

    public void Append(string line)
    {
        _all.Add(line);
        if (Matches(line)) FilteredLines.Add(line);
    }

    private bool Matches(string line)
    {
        if (LevelFilter != "All" && !line.Contains($"[{LevelFilter}]", StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(SearchText)
            && !line.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    private void Reapply()
    {
        FilteredLines.Clear();
        foreach (var l in _all) if (Matches(l)) FilteredLines.Add(l);
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter LogsViewModelTests` → PASS (3 tests).

- [ ] **Step 5: Create LogsPage** — a `DispatcherTimer` polls the newest log file via `LogTailer` and calls `Append`.

`LogsPage.xaml`:
```xml
<Page
    x:Class="ProxiFyre.UI.Views.LogsPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid Padding="24" RowDefinitions="Auto,*">
        <StackPanel Orientation="Horizontal" Spacing="12">
            <TextBlock Text="Logs" Style="{ThemeResource TitleTextBlockStyle}" VerticalAlignment="Center"/>
            <ComboBox Header="Level" SelectedItem="{x:Bind Vm.LevelFilter, Mode=TwoWay}">
                <x:String>All</x:String><x:String>Error</x:String><x:String>Warning</x:String>
                <x:String>Info</x:String><x:String>Debug</x:String>
            </ComboBox>
            <TextBox Header="Search" Width="220" Text="{x:Bind Vm.SearchText, Mode=TwoWay}"/>
            <Button Content="Open logs folder" Click="OpenFolder_Click" VerticalAlignment="Bottom"/>
        </StackPanel>
        <ListView Grid.Row="1" Margin="0,12" ItemsSource="{x:Bind Vm.FilteredLines}"
                  FontFamily="Consolas" FontSize="12"/>
    </Grid>
</Page>
```

`LogsPage.xaml.cs`:
```csharp
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProxiFyre.Core.Locate;
using ProxiFyre.Core.Logs;
using ProxiFyre.Core.ViewModels;

namespace ProxiFyre.UI.Views;

public sealed partial class LogsPage : Page
{
    public LogsViewModel Vm { get; } = new();
    private readonly ILocatorService _locator;
    private LogTailer? _tailer;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    public LogsPage()
    {
        _locator = App.Services.GetRequiredService<ILocatorService>();
        InitializeComponent();
        _timer.Tick += (_, _) => Poll();
        Loaded += (_, _) => _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
    }

    private void Poll()
    {
        var file = NewestLog();
        if (file is null) return;
        _tailer ??= new LogTailer(file);
        foreach (var line in _tailer.ReadNew()) Vm.Append(line);
    }

    private string? NewestLog()
    {
        if (!Directory.Exists(_locator.LogsDir)) return null;
        return new DirectoryInfo(_locator.LogsDir)
            .GetFiles("*.log").OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault()?.FullName;
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (Directory.Exists(_locator.LogsDir))
            Process.Start(new ProcessStartInfo(_locator.LogsDir) { UseShellExecute = true });
    }
}
```

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(ui): logs viewer with level filter and search"
```

### Task 18: Settings (viewmodel TDD + page)

**Files:**
- Create: `src/ProxiFyre.Core/ViewModels/SettingsViewModel.cs`, `src/ProxiFyre.UI/Views/SettingsPage.xaml(.cs)`
- Test: `tests/ProxiFyre.Core.Tests/SettingsViewModelTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using ProxiFyre.Core.Config;
using ProxiFyre.Core.Models;
using ProxiFyre.Core.ViewModels;
using Xunit;

public class SettingsViewModelTests
{
    private sealed class Store : IConfigStore
    {
        public AppConfig ToLoad = new();
        public AppConfig Saved = new();
        public AppConfig Read(string p) => ToLoad;
        public void Write(string p, AppConfig c) => Saved = c;
        public ValidationResult Validate(AppConfig c) => new();
    }

    [Fact]
    public void Load_ReadsLogLevel()
    {
        var store = new Store { ToLoad = new AppConfig { LogLevel = "Debug" } };
        var vm = new SettingsViewModel(store, "cfg.json");
        vm.Load();
        Assert.Equal("Debug", vm.LogLevel);
    }

    [Fact]
    public void Save_PersistsLogLevel_PreservingOtherFields()
    {
        var store = new Store();
        store.ToLoad.Proxies.Add(new ProxyRule { Socks5ProxyEndpoint = "127.0.0.1:1080" });
        var vm = new SettingsViewModel(store, "cfg.json");
        vm.Load();
        vm.LogLevel = "Warning";
        vm.SaveCommand.Execute(null);
        Assert.Equal("Warning", store.Saved.LogLevel);
        Assert.Single(store.Saved.Proxies); // preserved
    }
}
```

- [ ] **Step 2: Run to verify fail**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter SettingsViewModelTests` → FAIL (type missing).

- [ ] **Step 3: Implement SettingsViewModel**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxiFyre.Core.Config;

namespace ProxiFyre.Core.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigStore _store;
    private readonly string _configPath;

    public SettingsViewModel(IConfigStore store, string configPath)
    {
        _store = store; _configPath = configPath;
    }

    public string[] LogLevels { get; } = { "Error", "Warning", "Info", "Debug", "All" };
    [ObservableProperty] private string _logLevel = "Error";

    public void Load() => LogLevel = _store.Read(_configPath).LogLevel;

    [RelayCommand]
    private void Save()
    {
        var cfg = _store.Read(_configPath);
        cfg.LogLevel = LogLevel;
        _store.Write(_configPath, cfg);
    }
}
```

Note: autostart-on-boot (service `StartType = Automatic`) and theme are UI-only concerns wired in the page code-behind (Windows-only), not in the tested viewmodel. Autostart uses `sc.exe config ProxiFyre start= auto` via an elevated process; theme sets `FrameworkElement.RequestedTheme`.

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter SettingsViewModelTests` → PASS (2 tests).

- [ ] **Step 5: Create SettingsPage**

`SettingsPage.xaml`:
```xml
<Page
    x:Class="ProxiFyre.UI.Views.SettingsPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel Padding="24" Spacing="16">
        <TextBlock Text="Settings" Style="{ThemeResource TitleTextBlockStyle}"/>
        <ComboBox Header="Log level" ItemsSource="{x:Bind Vm.LogLevels}"
                  SelectedItem="{x:Bind Vm.LogLevel, Mode=TwoWay}"/>
        <ToggleSwitch x:Name="AutostartSwitch" Header="Start ProxiFyre on boot"
                      Toggled="Autostart_Toggled"/>
        <Button Content="Save" Style="{ThemeResource AccentButtonStyle}"
                Command="{x:Bind Vm.SaveCommand}"/>
    </StackPanel>
</Page>
```

`SettingsPage.xaml.cs`:
```csharp
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProxiFyre.Core.Config;
using ProxiFyre.Core.Locate;
using ProxiFyre.Core.ViewModels;

namespace ProxiFyre.UI.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel Vm { get; }
    public SettingsPage()
    {
        var store = App.Services.GetRequiredService<IConfigStore>();
        var locator = App.Services.GetRequiredService<ILocatorService>();
        Vm = new SettingsViewModel(store, locator.ConfigPath);
        InitializeComponent();
        Vm.Load();
    }

    private void Autostart_Toggled(object sender, RoutedEventArgs e)
    {
        var mode = AutostartSwitch.IsOn ? "auto" : "demand";
        var psi = new ProcessStartInfo("sc.exe", $"config ProxiFyre start= {mode}")
        {
            UseShellExecute = false, CreateNoWindow = true
        };
        try { Process.Start(psi)?.WaitForExit(10_000); } catch { /* surfaced on smoke host */ }
    }
}
```

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(ui): settings view (log level, autostart)"
```

### Task 19: First-run prerequisite guidance

Surfaces missing prerequisites on the Dashboard with links to installers and a Recheck button. UI-only (Windows); logic already lives in `PrereqChecker`/`DashboardViewModel`.

**Files:**
- Modify: `src/ProxiFyre.Core/ViewModels/DashboardViewModel.cs` (add `Recheck`), `src/ProxiFyre.UI/Views/DashboardPage.xaml(.cs)`
- Test: `tests/ProxiFyre.Core.Tests/DashboardViewModelTests.cs` (add recheck test)

- [ ] **Step 1: Add failing test for Recheck**

Append to `DashboardViewModelTests`:
```csharp
    [Fact]
    public void Recheck_ReflectsNewlyInstalledPrereqs()
    {
        var checker = new FakeChecker
        {
            Status = new PrereqStatus { DriverInstalled = false, RuntimeInstalled = true }
        };
        checker.Status.Missing.Add("Windows Packet Filter (WinpkFilter) driver");
        var vm = new DashboardViewModel(checker, new FakeStore(), new FakeLocator());
        vm.Load();
        Assert.False(vm.PrereqReady);

        checker.Status = new PrereqStatus { DriverInstalled = true, RuntimeInstalled = true };
        vm.RecheckCommand.Execute(null);
        Assert.True(vm.PrereqReady);
    }
```

- [ ] **Step 2: Run to verify fail**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter DashboardViewModelTests` → FAIL — `RecheckCommand` missing.

- [ ] **Step 3: Add Recheck command to DashboardViewModel**

Add to `DashboardViewModel` (needs `using CommunityToolkit.Mvvm.Input;`):
```csharp
    [RelayCommand] private void Recheck() => Load();
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/ProxiFyre.Core.Tests/ --filter DashboardViewModelTests` → PASS.

- [ ] **Step 5: Add setup UI to DashboardPage**

In `DashboardPage.xaml`, replace the prerequisites card's inner `StackPanel` with one that shows install links + recheck when not ready:
```xml
<StackPanel Spacing="8">
    <TextBlock Text="Prerequisites" Style="{ThemeResource SubtitleTextBlockStyle}"/>
    <TextBlock Text="All prerequisites installed. Ready to start."
               Foreground="LimeGreen"
               Visibility="{x:Bind Vm.PrereqReady, Mode=OneWay}"/>
    <ItemsControl ItemsSource="{x:Bind Vm.MissingPrereqs, Mode=OneWay}">
        <ItemsControl.ItemTemplate>
            <DataTemplate x:DataType="x:String">
                <TextBlock Text="{x:Bind}" Foreground="OrangeRed"/>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
    <StackPanel Orientation="Horizontal" Spacing="8">
        <HyperlinkButton Content="Get WinpkFilter driver"
            NavigateUri="https://github.com/wiresock/ndisapi/releases"/>
        <HyperlinkButton Content="Get VC++ 2022 runtime"
            NavigateUri="https://aka.ms/vs/17/release/vc_redist.x64.exe"/>
        <Button Content="Recheck" Command="{x:Bind Vm.RecheckCommand}"/>
    </StackPanel>
</StackPanel>
```

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(ui): first-run prerequisite guidance on dashboard"
```

---

## Phase 3 — Packaging & release

### Task 20: Inno Setup installer script

Bundles the published UI + `ProxiFyre.exe`, creates shortcuts. The `ProxiFyre.exe` binary is fetched during release (Task 21) and placed in `publish/` before compiling the installer.

**Files:**
- Create: `installer/proxifyre-ui.iss`

- [ ] **Step 1: Write the Inno Setup script**

`installer/proxifyre-ui.iss`:
```ini
#define AppName "ProxiFyre UI"
#define AppVersion GetEnv("APP_VERSION")
#define PublishDir "..\publish"

[Setup]
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={autopf}\ProxiFyreUI
DefaultGroupName=ProxiFyre UI
OutputDir=..\dist
OutputBaseFilename=ProxiFyreUI-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin

[Files]
; Published WinUI app (self-contained) + bundled ProxiFyre.exe live in publish/
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\ProxiFyre UI"; Filename: "{app}\ProxiFyre.UI.exe"
Name: "{autodesktop}\ProxiFyre UI"; Filename: "{app}\ProxiFyre.UI.exe"

[Run]
Filename: "{app}\ProxiFyre.UI.exe"; Description: "Launch ProxiFyre UI"; \
  Flags: nowait postinstall skipifsilent
```

- [ ] **Step 2: Commit**

```bash
git add installer/proxifyre-ui.iss
git commit -m "build: add Inno Setup installer script"
```

### Task 21: Release workflow (tag → installer → GitHub Release)

**Files:**
- Create: `.github/workflows/release.yml`

- [ ] **Step 1: Write the release workflow**

```yaml
name: Release
on:
  push:
    tags: ['v*']
jobs:
  release:
    runs-on: windows-latest
    permissions:
      contents: write
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Derive version
        id: ver
        shell: pwsh
        run: echo "version=$($env:GITHUB_REF_NAME.TrimStart('v'))" >> $env:GITHUB_OUTPUT

      - name: Publish WinUI app (self-contained x64)
        run: >
          dotnet publish src/ProxiFyre.UI/ProxiFyre.UI.csproj
          -c Release -r win-x64 --self-contained true
          -p:WindowsAppSDKSelfContained=true
          -o publish

      # Bundle ProxiFyre.exe. Pin a known-good ProxiFyre release; update the URL as needed.
      - name: Download ProxiFyre binary
        shell: pwsh
        run: |
          $url = "https://github.com/wiresock/proxifyre/releases/latest/download/ProxiFyre.zip"
          Invoke-WebRequest -Uri $url -OutFile proxifyre.zip
          Expand-Archive proxifyre.zip -DestinationPath publish -Force

      - name: Install Inno Setup
        run: choco install innosetup --no-progress -y

      - name: Build installer
        shell: pwsh
        env:
          APP_VERSION: ${{ steps.ver.outputs.version }}
        run: |
          & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\proxifyre-ui.iss

      - name: Publish GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          files: dist/*.exe
          generate_release_notes: true
```

Note: verify the ProxiFyre release asset name/URL on the smoke host (it may differ from `ProxiFyre.zip`); adjust the download step accordingly. If ProxiFyre's license requires it, document redistribution in the README.

- [ ] **Step 2: Commit**

```bash
git add .github/workflows/release.yml
git commit -m "ci: release workflow builds installer on version tags"
```

### Task 22: Manual smoke-test checklist

**Files:**
- Create: `docs/manual-smoke-checklist.md`

- [ ] **Step 1: Write the checklist**

```markdown
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
```

- [ ] **Step 2: Commit**

```bash
git add docs/manual-smoke-checklist.md
git commit -m "docs: manual smoke-test checklist"
```

---

## Done criteria

- CI green on `main` (Core tests + UI build).
- Tagging `vX.Y.Z` produces a GitHub Release with `ProxiFyreUI-Setup-X.Y.Z.exe`.
- Manual smoke checklist passes on a real Windows host.











