using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Fixtures;

/// <summary>
/// Class-level fixture that creates a browser instance for each test class.
/// Each test class gets its own browser, enabling parallel test execution.
/// Individual tests within a class share the browser but get isolated contexts.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        Console.WriteLine("[PlaywrightFixture] Initializing...");

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        var browserType = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSER")?.ToLower() ?? "chromium";
        var headless = Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADLESS") != "false";

        var options = new BrowserTypeLaunchOptions { Headless = headless };

        Browser = browserType switch
        {
            "firefox" => await Playwright.Firefox.LaunchAsync(options),
            "webkit" => await Playwright.Webkit.LaunchAsync(options),
            _ => await Playwright.Chromium.LaunchAsync(options)
        };

        Console.WriteLine($"[PlaywrightFixture] Initialized {browserType} (headless: {headless}). Browser version: {Browser.Version}");
    }

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine("[PlaywrightFixture] Disposing...");

        if (Browser is not null)
        {
            await Browser.DisposeAsync();
        }

        Playwright?.Dispose();

        Console.WriteLine("[PlaywrightFixture] Disposed.");
    }
}
