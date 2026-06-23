namespace Blazix.BaseUI.Tests.FocusGuard;

public class FocusGuardTests : BunitContext, IFocusGuardContract
{
    public FocusGuardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static RenderFragment CreateFocusGuard(EventCallback? onFocus = null)
    {
        return builder =>
        {
            builder.OpenComponent<Blazix.BaseUI.Utilities.FocusGuard.FocusGuard>(0);
            if (onFocus.HasValue)
            {
                builder.AddAttribute(1, "OnFocus", onFocus.Value);
            }
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsSpan()
    {
        var cut = Render(CreateFocusGuard());

        var guard = cut.Find("[data-blazix-base-ui-focus-guard]");
        guard.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabIndexZero()
    {
        var cut = Render(CreateFocusGuard());

        var guard = cut.Find("[data-blazix-base-ui-focus-guard]");
        guard.GetAttribute("tabindex")!.ShouldBe("0");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaHiddenTrue()
    {
        var cut = Render(CreateFocusGuard());

        var guard = cut.Find("[data-blazix-base-ui-focus-guard]");
        guard.GetAttribute("aria-hidden")!.ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasFocusGuardDataAttribute()
    {
        var cut = Render(CreateFocusGuard());

        var guard = cut.Find("[data-blazix-base-ui-focus-guard]");
        guard.HasAttribute("data-blazix-base-ui-focus-guard").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasVisuallyHiddenStyles()
    {
        var cut = Render(CreateFocusGuard());

        var guard = cut.Find("[data-blazix-base-ui-focus-guard]");
        var style = guard.GetAttribute("style")!;
        style.ShouldContain("position:fixed");
        style.ShouldContain("overflow:hidden");
        style.ShouldContain("clip-path:inset(50%)");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesOnFocusCallback()
    {
        var invoked = false;
        var onFocus = EventCallback.Factory.Create(this, () => invoked = true);

        var cut = Render(CreateFocusGuard(onFocus: onFocus));

        var guard = cut.Find("[data-blazix-base-ui-focus-guard]");
        guard.TriggerEvent("onfocus", new FocusEventArgs());

        invoked.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRoleButtonOnSafari()
    {
        JsInteropSetup.SetupFocusGuardSafari(JSInterop);

        var cut = Render(CreateFocusGuard());

        var guard = cut.Find("[data-blazix-base-ui-focus-guard]");
        guard.GetAttribute("role")!.ShouldBe("button");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveAriaHiddenOnSafari()
    {
        JsInteropSetup.SetupFocusGuardSafari(JSInterop);

        var cut = Render(CreateFocusGuard());

        var guard = cut.Find("[data-blazix-base-ui-focus-guard]");
        guard.HasAttribute("aria-hidden").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveRoleWhenNotSafari()
    {
        JsInteropSetup.SetupFocusGuardNonSafari(JSInterop);

        var cut = Render(CreateFocusGuard());

        var guard = cut.Find("[data-blazix-base-ui-focus-guard]");
        guard.HasAttribute("role").ShouldBeFalse();

        return Task.CompletedTask;
    }
}
