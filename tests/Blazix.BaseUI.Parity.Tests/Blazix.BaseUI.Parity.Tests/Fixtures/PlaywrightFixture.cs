using Microsoft.Playwright;

namespace Blazix.BaseUI.Parity.Tests.Fixtures;

/// <summary>
/// Class-level fixture that creates a browser instance for each test class.
/// Each test class gets its own browser, enabling parallel test execution.
/// Individual tests within a class share the browser but get isolated contexts.
/// </summary>
public sealed class PlaywrightFixture : IAsyncLifetime
{
    /// <summary>Gets the Playwright driver.</summary>
    public IPlaywright Playwright { get; private set; } = null!;

    /// <summary>Gets the browser shared by every test in the class.</summary>
    public IBrowser Browser { get; private set; } = null!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        Console.WriteLine("[PlaywrightFixture] Initializing...");

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        var browserType = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSER")?.ToLower() ?? "chromium";
        var headless = Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADLESS") != "false";

        // Font hinting and subpixel antialiasing vary with the host's display settings,
        // which would make the pixel comparator report differences that are not real.
        var options = new BrowserTypeLaunchOptions
        {
            Headless = headless,
            Args = ["--font-render-hinting=none", "--disable-lcd-text"]
        };

        Browser = browserType switch
        {
            "firefox" => await Playwright.Firefox.LaunchAsync(options),
            "webkit" => await Playwright.Webkit.LaunchAsync(options),
            _ => await Playwright.Chromium.LaunchAsync(options)
        };

        Console.WriteLine($"[PlaywrightFixture] Initialized {browserType} (headless: {headless}). Browser version: {Browser.Version}");
    }

    /// <inheritdoc />
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
