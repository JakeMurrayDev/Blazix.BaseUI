# Playwright Test Suite

End-to-end browser tests for Blazix.BaseUI components.

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~CollapsibleServerTests"

# Run specific test
dotnet test --filter "TestName"
```

## Debugging

### Interactive Debugging

Enable headed mode with debug pause points:

```powershell
# PowerShell
$env:PLAYWRIGHT_DEBUG="1"
$env:PLAYWRIGHT_HEADLESS="false"
dotnet test --filter "TestName"
```

```bash
# Bash
PLAYWRIGHT_DEBUG=1 PLAYWRIGHT_HEADLESS=false dotnet test --filter "TestName"
```

In your test, add `await DebugPauseAsync();` where you want to pause for inspection.

### Trace Viewer

All tests generate trace files that can be viewed for post-mortem debugging:

```bash
# View a trace file
npx playwright show-trace tests/Blazix.BaseUI.Playwright.Tests/traces/TestName_timestamp.zip
```

Traces include:
- Screenshots at each step
- DOM snapshots
- Network requests
- Console logs

## Test Generation (Codegen)

Generate test code by recording browser interactions:

```bash
pwsh bin/Debug/net10.0/playwright.ps1 codegen http://localhost:5000
```

## Cross-Browser Testing

Tests run on Chromium by default. Use environment variables for other browsers:

```powershell
# Firefox
$env:PLAYWRIGHT_BROWSER="firefox"
dotnet test

# WebKit (Safari engine)
$env:PLAYWRIGHT_BROWSER="webkit"
dotnet test

# Reset to Chromium
$env:PLAYWRIGHT_BROWSER="chromium"
dotnet test
```

### Install Additional Browsers

```bash
pwsh bin/Debug/net10.0/playwright.ps1 install firefox
pwsh bin/Debug/net10.0/playwright.ps1 install webkit
```

## Environment Variables

| Variable | Values | Default | Description |
|----------|--------|---------|-------------|
| `PLAYWRIGHT_BROWSER` | chromium, firefox, webkit | chromium | Browser engine to use |
| `PLAYWRIGHT_HEADLESS` | true, false | true | Run in headless mode |
| `PLAYWRIGHT_DEBUG` | 0, 1 | 0 | Enable debug pause points |

## Test Structure

- `Infrastructure/` - Base classes and utilities
  - `TestBase.cs` - Base class with navigation, timeouts, and helpers
  - `TestRenderMode.cs` - Enum for Server/WASM modes
  - `TestPageUrlBuilder.cs` - Fluent URL builder for test pages
- `Fixtures/` - xUnit fixtures for browser lifecycle
  - `PlaywrightFixture.cs` - Browser instance management
  - `BlazorTestFixture.cs` - Blazor server management
- `Tests/` - Component test implementations
  - `Collapsible/` - Collapsible component tests

## WASM Timeout Scaling

WASM tests automatically use 3x timeout multiplier to account for:
- WebAssembly runtime download
- .NET assembly initialization
- JS module lazy loading
- Higher interop latency

The `TimeoutMultiplier` property in `TestBase` handles this scaling automatically.
