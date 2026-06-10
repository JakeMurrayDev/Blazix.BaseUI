using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Infrastructure;

public static class PlaywrightTimeHelpers
{
    public static async Task InstallFakeTimersAsync(IPage page)
    {
        await page.Clock.InstallAsync(new ClockInstallOptions
        {
            Time = DateTime.UtcNow.ToString("O")
        });
    }

    public static async Task AdvanceTimeAsync(IPage page, int milliseconds)
    {
        await page.Clock.FastForwardAsync(milliseconds);
    }

    public static async Task SetFixedTimeAsync(IPage page, DateTime time)
    {
        await page.Clock.SetFixedTimeAsync(time.ToString("O"));
    }

    public static async Task PauseTimeAsync(IPage page)
    {
        await page.Clock.PauseAtAsync(DateTime.UtcNow.ToString("O"));
    }

    public static async Task ResumeTimeAsync(IPage page)
    {
        await page.Clock.ResumeAsync();
    }
}
