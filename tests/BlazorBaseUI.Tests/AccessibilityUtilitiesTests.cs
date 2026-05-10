namespace BlazorBaseUI.Tests;

public class AccessibilityUtilitiesTests
{
    [Fact]
    public Task ApplyButtonAttributes_AddsRoleAndTabindex()
    {
        var attrs = new Dictionary<string, object>();
        AccessibilityUtilities.ApplyButtonAttributes(attrs);
        attrs["role"].ShouldBe("button");
        attrs["tabindex"].ShouldBe(0);
        attrs.ShouldNotContainKey("aria-disabled");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ApplyButtonAttributes_DisabledSetsTabindexMinusOne()
    {
        var attrs = new Dictionary<string, object>();
        AccessibilityUtilities.ApplyButtonAttributes(attrs, disabled: true);
        attrs["role"].ShouldBe("button");
        attrs["aria-disabled"].ShouldBe("true");
        attrs["tabindex"].ShouldBe(-1);
        return Task.CompletedTask;
    }

    [Fact]
    public Task ApplyButtonAttributes_DisabledFocusableKeepsTabindex()
    {
        var attrs = new Dictionary<string, object>();
        AccessibilityUtilities.ApplyButtonAttributes(attrs, disabled: true, focusableWhenDisabled: true);
        attrs["role"].ShouldBe("button");
        attrs["aria-disabled"].ShouldBe("true");
        attrs["tabindex"].ShouldBe(0);
        return Task.CompletedTask;
    }

    [Fact]
    public Task ApplyButtonAttributes_CustomTabIndex()
    {
        var attrs = new Dictionary<string, object>();
        AccessibilityUtilities.ApplyButtonAttributes(attrs, tabIndex: 5);
        attrs["role"].ShouldBe("button");
        attrs["tabindex"].ShouldBe(5);
        return Task.CompletedTask;
    }

    [Fact]
    public Task ApplyNativeButtonAttributes_AddsTypeAndTabindex()
    {
        var attrs = new Dictionary<string, object>();
        AccessibilityUtilities.ApplyNativeButtonAttributes(attrs);
        attrs["type"].ShouldBe("button");
        attrs["tabindex"].ShouldBe(0);
        attrs.ShouldNotContainKey("aria-disabled");
        attrs.ShouldNotContainKey("disabled");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ApplyNativeButtonAttributes_DisabledSetsDisabledAttribute()
    {
        var attrs = new Dictionary<string, object>();
        AccessibilityUtilities.ApplyNativeButtonAttributes(attrs, disabled: true);
        attrs["type"].ShouldBe("button");
        attrs["disabled"].ShouldBe(true);
        attrs.ShouldNotContainKey("aria-disabled");
        attrs.ShouldNotContainKey("tabindex");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ApplyNativeButtonAttributes_DisabledFocusableSetsAriaDisabled()
    {
        var attrs = new Dictionary<string, object>();
        AccessibilityUtilities.ApplyNativeButtonAttributes(attrs, disabled: true, focusableWhenDisabled: true);
        attrs["type"].ShouldBe("button");
        attrs["aria-disabled"].ShouldBe("true");
        attrs["tabindex"].ShouldBe(0);
        attrs.ShouldNotContainKey("disabled");
        return Task.CompletedTask;
    }

    [Fact]
    public Task GetDefaultLabelId_ReturnsConventionBasedId()
    {
        var result = AccessibilityUtilities.GetDefaultLabelId("my-control");
        result.ShouldBe("my-control-label");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ResolveAriaLabelledBy_FieldLabelTakesPrecedence()
    {
        var result = AccessibilityUtilities.ResolveAriaLabelledBy("field-label-1", "local-label-1");
        result.ShouldBe("field-label-1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ResolveAriaLabelledBy_FallsBackToLocalLabel()
    {
        var result = AccessibilityUtilities.ResolveAriaLabelledBy(null, "local-label-1");
        result.ShouldBe("local-label-1");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ResolveAriaLabelledBy_ReturnsNullWhenBothNull()
    {
        var result = AccessibilityUtilities.ResolveAriaLabelledBy(null, null);
        result.ShouldBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ApplyFocusableWhenDisabled_NotDisabledIsNoop()
    {
        var attrs = new Dictionary<string, object>();
        AccessibilityUtilities.ApplyFocusableWhenDisabled(attrs, disabled: false, focusableWhenDisabled: true);
        attrs.ShouldBeEmpty();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ApplyFocusableWhenDisabled_FocusableRemovesDisabledAddsAriaDisabled()
    {
        var attrs = new Dictionary<string, object> { ["disabled"] = true };
        AccessibilityUtilities.ApplyFocusableWhenDisabled(attrs, disabled: true, focusableWhenDisabled: true);
        attrs.ShouldNotContainKey("disabled");
        attrs["aria-disabled"].ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ApplyFocusableWhenDisabled_NativeButtonGetsDisabledAttribute()
    {
        var attrs = new Dictionary<string, object>();
        AccessibilityUtilities.ApplyFocusableWhenDisabled(attrs, disabled: true, focusableWhenDisabled: false, isNativeButton: true);
        attrs["disabled"].ShouldBe(true);
        attrs.ShouldNotContainKey("aria-disabled");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ApplyFocusableWhenDisabled_NonNativeGetsAriaDisabled()
    {
        var attrs = new Dictionary<string, object>();
        AccessibilityUtilities.ApplyFocusableWhenDisabled(attrs, disabled: true, focusableWhenDisabled: false, isNativeButton: false);
        attrs["aria-disabled"].ShouldBe("true");
        attrs.ShouldNotContainKey("disabled");
        return Task.CompletedTask;
    }
}
