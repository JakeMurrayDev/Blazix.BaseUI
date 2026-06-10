using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Blazix.BaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Tests;

public class DiagnosticTests : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture playwrightFixture;
    private IBrowserContext? context;
    private IPage page = null!;

    private string ServerAddress => BlazorServerAssemblyFixture.ServerAddress;

    public DiagnosticTests(PlaywrightFixture playwrightFixture)
    {
        this.playwrightFixture = playwrightFixture;
    }

    public async ValueTask InitializeAsync()
    {
        Console.WriteLine($"[Diagnostic] InitializeAsync - ServerAddress: '{ServerAddress}'");
        context = await playwrightFixture.Browser.NewContextAsync();
        page = await context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (page is not null)
        {
            await page.CloseAsync();
        }

        if (context is not null)
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task ServerIsRunning()
    {
        Console.WriteLine($"[Diagnostic] ServerIsRunning test starting");
        Console.WriteLine($"[Diagnostic] ServerAddress: '{ServerAddress}'");

        Assert.False(string.IsNullOrEmpty(ServerAddress), "ServerAddress should not be empty");

        var url = $"{ServerAddress}/tests/collapsible/server";
        Console.WriteLine($"[Diagnostic] Navigating to: {url}");

        var response = await page.GotoAsync(url);

        Console.WriteLine($"[Diagnostic] Response status: {response?.Status}");
        Assert.NotNull(response);
        Assert.True(response.Ok, $"Expected OK response, got {response.Status}");

        var title = await page.TitleAsync();
        Console.WriteLine($"[Diagnostic] Page title: {title}");

        var content = await page.ContentAsync();
        Console.WriteLine($"[Diagnostic] Page content length: {content.Length}");
        Console.WriteLine($"[Diagnostic] Page content (first 500): {content[..Math.Min(500, content.Length)]}");
    }

    [Fact]
    public async Task CanLoadTestPageMultipleTimes()
    {
        var url = $"{ServerAddress}/tests/collapsible/server";

        for (int i = 0; i < 3; i++)
        {
            Console.WriteLine($"[Diagnostic] Iteration {i + 1}");

            var response = await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.Load
            });

            Assert.NotNull(response);
            Assert.True(response.Ok, $"Iteration {i + 1}: Expected OK response, got {response.Status}");

            // Wait for test container
            var testContainer = page.GetByTestId("test-container");
            await testContainer.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Attached,
                Timeout = 10000
            });

            Console.WriteLine($"[Diagnostic] Iteration {i + 1}: Test container found");

            // Check trigger is visible
            var trigger = page.GetByTestId("collapsible-trigger");
            var isVisible = await trigger.IsVisibleAsync();
            Console.WriteLine($"[Diagnostic] Iteration {i + 1}: Trigger visible: {isVisible}");

            Assert.True(isVisible, $"Iteration {i + 1}: Trigger should be visible");
        }
    }
}
