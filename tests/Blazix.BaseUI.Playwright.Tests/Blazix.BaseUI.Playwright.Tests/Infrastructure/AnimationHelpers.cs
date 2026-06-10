using Microsoft.Playwright;

namespace Blazix.BaseUI.Playwright.Tests.Infrastructure;

public static class AnimationHelpers
{
    public static async Task<string> GetCssVariableAsync(this ILocator element, string variableName)
    {
        await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 5000 });
        return await element.EvaluateAsync<string>($@"
            (el) => getComputedStyle(el).getPropertyValue('{variableName}').trim()
        ");
    }

    public static async Task<string?> GetStylePropertyAsync(this ILocator element, string variableName)
    {
        await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 5000 });
        return await element.EvaluateAsync<string?>($@"
            (el) => el.style.getPropertyValue('{variableName}') || null
        ");
    }

    public static async Task WaitForAnimationsAsync(this ILocator element, float timeout = 5000)
    {
        try
        {
            await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = timeout });
            await element.EvaluateAsync(@"
                (el) => Promise.all(el.getAnimations().map(a => a.finished))
            ", new LocatorEvaluateOptions { Timeout = timeout });
        }
        catch (TimeoutException)
        {
            // No animations or element not found - continue
        }
    }

    public static async Task<double> GetHeightAsync(this ILocator element)
    {
        // First check if element exists
        var count = await element.CountAsync();
        if (count == 0)
        {
            Console.WriteLine($"[AnimationHelpers] GetHeightAsync: Element not found in DOM");
            return 0;
        }

        try
        {
            await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 5000 });
            var box = await element.BoundingBoxAsync();
            var height = box?.Height ?? 0;
            Console.WriteLine($"[AnimationHelpers] GetHeightAsync: {height}");
            return height;
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"[AnimationHelpers] GetHeightAsync timeout: {ex.Message}");
            return 0;
        }
    }

    public static async Task<double> GetWidthAsync(this ILocator element)
    {
        var count = await element.CountAsync();
        if (count == 0)
        {
            return 0;
        }

        try
        {
            await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 5000 });
            var box = await element.BoundingBoxAsync();
            return box?.Width ?? 0;
        }
        catch (TimeoutException)
        {
            return 0;
        }
    }

    public static async Task<bool> HasAttributeAsync(this ILocator element, string attribute, float timeout = 5000)
    {
        var count = await element.CountAsync();
        if (count == 0)
        {
            return false;
        }

        try
        {
            await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = timeout });
            return await element.EvaluateAsync<bool>($"(el) => el.hasAttribute('{attribute}')");
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    public static async Task<string?> GetAttributeValueAsync(this ILocator element, string attribute)
    {
        return await element.GetAttributeAsync(attribute);
    }

    public static async Task WaitForAttributeAsync(
        this ILocator element,
        string attribute,
        float timeout = 5000)
    {
        await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = timeout });
        await Assertions.Expect(element).ToHaveAttributeAsync(
            attribute,
            new System.Text.RegularExpressions.Regex(".*"),
            new LocatorAssertionsToHaveAttributeOptions { Timeout = timeout });
    }

    public static async Task WaitForAttributeRemovedAsync(
        this ILocator element,
        string attribute,
        float timeout = 5000)
    {
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeout)
        {
            var remainingTimeout = Math.Max(100, timeout - (float)(DateTime.UtcNow - startTime).TotalMilliseconds);
            if (!await element.HasAttributeAsync(attribute, remainingTimeout))
            {
                return;
            }
            await Task.Delay(50);
        }

        throw new TimeoutException($"Attribute '{attribute}' was not removed within {timeout}ms");
    }

    public static async Task<bool> HasNonZeroDimensionsAsync(this ILocator element)
    {
        var count = await element.CountAsync();
        if (count == 0)
        {
            return false;
        }

        try
        {
            await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached, Timeout = 5000 });
            return await element.EvaluateAsync<bool>(@"
                (el) => {
                    const rect = el.getBoundingClientRect();
                    return rect.height > 0 && rect.width > 0;
                }
            ");
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}
