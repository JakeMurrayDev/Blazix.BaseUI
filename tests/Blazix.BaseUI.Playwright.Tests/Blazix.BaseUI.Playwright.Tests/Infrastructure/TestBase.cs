using Blazix.BaseUI.Playwright.Tests.Fixtures;
using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Infrastructure;

public abstract class TestBase : IAsyncLifetime
{
    private readonly PlaywrightFixture playwrightFixture;
    private IBrowserContext? context;
    private bool testFailed;

    protected IPage Page { get; private set; } = null!;

    /// <summary>
    /// Gets the server address from the assembly-level fixture.
    /// </summary>
    protected string ServerAddress => BlazorServerAssemblyFixture.ServerAddress;

    protected abstract TestRenderMode RenderMode { get; }

    /// <summary>
    /// Timeout multiplier for WASM mode. WASM requires longer timeouts due to
    /// runtime download and initialization overhead.
    /// </summary>
    protected int TimeoutMultiplier => RenderMode == TestRenderMode.Wasm ? 3 : 1;

    protected TestBase(PlaywrightFixture playwrightFixture)
    {
        this.playwrightFixture = playwrightFixture;
    }

    public async ValueTask InitializeAsync()
    {
        context = await playwrightFixture.Browser.NewContextAsync();

        // Start tracing for debugging CI failures
        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });

        Page = await context.NewPageAsync();

        // Log console messages for debugging
        Page.Console += (_, msg) =>
        {
            if (msg.Type is "error" or "warning")
            {
                Console.WriteLine($"[Browser {msg.Type}]: {msg.Text}");
            }
        };

        // Log page errors
        Page.PageError += (_, error) =>
        {
            Console.WriteLine($"[Page Error]: {error}");
        };

        // Log request failures
        Page.RequestFailed += (_, request) =>
        {
            Console.WriteLine($"[Request Failed]: {request.Url} - {request.Failure}");
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (context is not null)
        {
            var shouldSaveTrace = testFailed ||
                Environment.GetEnvironmentVariable("PLAYWRIGHT_SAVE_ALL_TRACES") == "1";

            if (shouldSaveTrace)
            {
                // Save trace for debugging - traces can be viewed with:
                // npx playwright show-trace traces/TestName_timestamp.zip
                var tracesDir = Environment.GetEnvironmentVariable("PLAYWRIGHT_TRACES_DIR")
                    ?? Path.Combine(Path.GetTempPath(), "blazor-playwright-traces");
                Directory.CreateDirectory(tracesDir);

                var tracePath = Path.Combine(
                    tracesDir,
                    $"{GetType().Name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip");

                await context.Tracing.StopAsync(new TracingStopOptions
                {
                    Path = tracePath
                });

                Console.WriteLine($"[Trace] Saved to: {tracePath}");
            }
            else
            {
                // Discard trace for passing tests
                await context.Tracing.StopAsync();
            }

            await context.CloseAsync();
        }
    }

    /// <summary>
    /// Runs a test action and automatically saves trace on failure.
    /// </summary>
    protected async Task RunTestAsync(Func<Task> testAction)
    {
        try
        {
            await testAction();
        }
        catch
        {
            testFailed = true;
            throw;
        }
    }

    /// <summary>
    /// Marks the current test as failed, ensuring the trace will be saved.
    /// Call this in catch blocks or when detecting failure conditions.
    /// </summary>
    protected void MarkTestAsFailed() => testFailed = true;

    protected TestPageUrlBuilder CreateUrl(string basePath)
    {
        if (string.IsNullOrEmpty(ServerAddress))
        {
            throw new InvalidOperationException(
                "ServerAddress is empty. BlazorServerAssemblyFixture may not have initialized.");
        }

        return new TestPageUrlBuilder(ServerAddress, basePath, RenderMode);
    }

    protected async Task NavigateAsync(string url)
    {
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            throw new ArgumentException(
                $"URL must be absolute. Got: '{url}'. ServerAddress: '{ServerAddress}'", nameof(url));
        }

        Console.WriteLine($"[Debug] Navigating to: {url}");

        var response = await Page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = 30000
        });

        Console.WriteLine($"[Debug] Navigation response status: {response?.Status}");

        if (response is not null && !response.Ok)
        {
            var body = await response.TextAsync();
            // Extract the exception message from the HTML response
            // Look for common exception patterns - prioritize actual error content over CSS
            var patterns = new[]
            {
                "System.InvalidOperationException",
                "System.NullReferenceException",
                "System.ArgumentException",
                "System.Exception",
                "An unhandled exception",
                "<title>",
                "class=\"titleerror\">"
            };
            var exceptionStart = -1;
            foreach (var pattern in patterns)
            {
                exceptionStart = body.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (exceptionStart >= 0) break;
            }

            string extractedError;
            if (exceptionStart >= 0)
            {
                extractedError = body[exceptionStart..Math.Min(exceptionStart + 1500, body.Length)];
                // Strip HTML tags for readability
                extractedError = System.Text.RegularExpressions.Regex.Replace(extractedError, "<[^>]+>", " ");
                extractedError = System.Text.RegularExpressions.Regex.Replace(extractedError, @"\s+", " ").Trim();
            }
            else
            {
                extractedError = body[..Math.Min(2000, body.Length)];
            }

            throw new InvalidOperationException(
                $"Navigation failed with status {response.Status}: {response.StatusText}\nError: {extractedError}");
        }

        await WaitForBlazorAsync();
    }

    protected async Task NavigateAsync(TestPageUrlBuilder builder)
    {
        await NavigateAsync(builder.Build());
    }

    protected virtual async Task WaitForBlazorAsync()
    {
        // For WASM mode without prerendering, the test container won't exist until
        // the WASM runtime loads and the component renders. Use a longer timeout.
        var containerTimeout = RenderMode == TestRenderMode.Wasm ? 60000 : 10000;

        // Wait for the test container to exist
        try
        {
            var testContainer = Page.GetByTestId("test-container");
            await testContainer.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Attached,
                Timeout = containerTimeout
            });
        }
        catch (TimeoutException)
        {
            var content = await Page.ContentAsync();
            Console.WriteLine($"[Debug] Page URL: {Page.Url}");
            Console.WriteLine($"[Debug] Page content: {content[..Math.Min(2000, content.Length)]}");
            throw new TimeoutException($"Test container not found after {containerTimeout}ms. Page may not have loaded correctly.");
        }

        // Wait for Blazor to become interactive
        // This checks for the Blazor circuit/WASM to be ready by waiting for the framework to signal readiness
        await WaitForBlazorInteractiveAsync();
    }

    private async Task WaitForBlazorInteractiveAsync(int timeout = 30000)
    {
        var isWasmMode = RenderMode == TestRenderMode.Wasm;

        // For WASM, we need to wait for the WebAssembly runtime to download and initialize
        // This can take significantly longer than Server mode
        var effectiveTimeout = isWasmMode ? timeout * 2 : timeout;

        // Wait for the data-interactive attribute to be "true"
        // This is set by the test pages using RendererInfo.IsInteractive which is only true
        // when the component is actually interactive (not during prerendering)
        try
        {
            await Page.WaitForFunctionAsync(
                @"() => {
                    const container = document.querySelector('[data-testid=""test-container""]');
                    return container?.getAttribute('data-interactive') === 'true';
                }",
                new PageWaitForFunctionOptions { Timeout = effectiveTimeout });

            Console.WriteLine("[Debug] Component is interactive (data-interactive=true)");
        }
        catch (TimeoutException)
        {
            // Fall back to checking if test container exists without the interactive attribute
            // This supports Server mode test pages that may not have the attribute
            Console.WriteLine("[Debug] data-interactive timeout - checking for test container presence");

            var testContainer = Page.GetByTestId("test-container");
            var isVisible = await testContainer.IsVisibleAsync();

            if (isVisible && !isWasmMode)
            {
                // For Server mode, if the container is visible, it's likely interactive
                Console.WriteLine("[Debug] Server mode: test container visible, proceeding");
                return;
            }

            throw new TimeoutException(
                $"Component did not become interactive within {effectiveTimeout}ms. " +
                "Ensure the test page has data-interactive attribute set.");
        }
    }

    protected async Task WaitForElementAsync(string testId, int timeout = 10000)
    {
        var effectiveTimeout = timeout * TimeoutMultiplier;
        var element = Page.GetByTestId(testId);
        await element.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = effectiveTimeout
        });
    }

    protected ILocator GetByTestId(string testId) => Page.GetByTestId(testId);

    protected async Task ClickTriggerAsync()
    {
        var trigger = GetByTestId("collapsible-trigger");
        await trigger.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });

        // Get current aria-expanded value to know what state we're transitioning to
        var currentExpanded = await trigger.GetAttributeAsync("aria-expanded");
        var expectingOpen = currentExpanded != "true";

        await trigger.ClickAsync();

        // Wait for the state change to complete
        var expectedValue = expectingOpen ? "true" : "false";
        await WaitForAttributeValueAsync(trigger, "aria-expanded", expectedValue);
    }

    protected async Task PressKeyAsync(string key)
    {
        var trigger = GetByTestId("collapsible-trigger");
        await trigger.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync(key);
        await WaitForDelayAsync(100);
    }

    protected async Task WaitForDelayAsync(int baseMs)
    {
        await Page.WaitForTimeoutAsync(baseMs * TimeoutMultiplier);
    }

    protected async Task WaitForAttributeValueAsync(ILocator element, string attribute, string value, int timeout = 5000)
    {
        var effectiveTimeout = timeout * TimeoutMultiplier;
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < effectiveTimeout)
        {
            var currentValue = await element.GetAttributeAsync(attribute);
            if (currentValue == value)
            {
                return;
            }
            await Task.Delay(50);
        }

        throw new TimeoutException($"Attribute '{attribute}' did not reach value '{value}' within {effectiveTimeout}ms");
    }

    protected async Task WaitForAttributeNotValueAsync(ILocator element, string attribute, string notValue, int timeout = 5000)
    {
        var effectiveTimeout = timeout * TimeoutMultiplier;
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < effectiveTimeout)
        {
            var currentValue = await element.GetAttributeAsync(attribute);
            if (currentValue != notValue)
            {
                return;
            }
            await Task.Delay(50);
        }

        throw new TimeoutException($"Attribute '{attribute}' did not change from value '{notValue}' within {effectiveTimeout}ms");
    }

    /// <summary>
    /// Pauses test execution for interactive debugging.
    /// Set PLAYWRIGHT_DEBUG=1 environment variable to enable.
    /// </summary>
    protected async Task DebugPauseAsync()
    {
        if (Environment.GetEnvironmentVariable("PLAYWRIGHT_DEBUG") == "1")
        {
            Console.WriteLine("[Debug] Pausing for interactive inspection. Press 'Resume' in Playwright Inspector.");
            await Page.PauseAsync();
        }
    }
}
