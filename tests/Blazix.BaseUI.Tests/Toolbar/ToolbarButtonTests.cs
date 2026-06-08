using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Blazix.BaseUI.Tests.Toolbar;

public class ToolbarButtonTests : BunitContext, IToolbarButtonContract
{
    public ToolbarButtonTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupToolbarModule(JSInterop);
        Services.AddLogging();
    }

    private RenderFragment CreateToolbarButtonInRoot(
        bool rootDisabled = false,
        Orientation rootOrientation = Orientation.Horizontal,
        bool buttonDisabled = false,
        bool focusableWhenDisabled = true,
        bool nativeButton = true,
        Func<ToolbarButtonState, string>? classValue = null,
        Func<ToolbarButtonState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<ToolbarRoot>(0);
            builder.AddAttribute(1, "Disabled", rootDisabled);
            builder.AddAttribute(2, "Orientation", rootOrientation);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<ToolbarButton>(0);
                var seq = 1;
                inner.AddAttribute(seq++, "Disabled", buttonDisabled);
                inner.AddAttribute(seq++, "FocusableWhenDisabled", focusableWhenDisabled);
                inner.AddAttribute(seq++, "NativeButton", nativeButton);
                if (classValue is not null)
                    inner.AddAttribute(seq++, "ClassValue", classValue);
                if (styleValue is not null)
                    inner.AddAttribute(seq++, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    inner.AddAttribute(seq++, "AdditionalAttributes", additionalAttributes);
                if (childContent is not null)
                    inner.AddAttribute(seq++, "ChildContent", childContent);
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateToolbarButtonInGroup(
        bool rootDisabled = false,
        bool groupDisabled = false,
        bool buttonDisabled = false)
    {
        return builder =>
        {
            builder.OpenComponent<ToolbarRoot>(0);
            builder.AddAttribute(1, "Disabled", rootDisabled);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<ToolbarGroup>(0);
                inner.AddAttribute(1, "Disabled", groupDisabled);
                inner.AddAttribute(2, "ChildContent", (RenderFragment)(groupInner =>
                {
                    groupInner.OpenComponent<ToolbarButton>(0);
                    groupInner.AddAttribute(1, "Disabled", buttonDisabled);
                    groupInner.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Group Button")));
                    groupInner.CloseComponent();
                }));
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsButtonByDefault()
    {
        var cut = Render(CreateToolbarButtonInRoot());
        var element = cut.Find("button");
        element.TagName.ShouldBe("BUTTON");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRenderFragment()
    {
        var fragment = (RenderFragment)(builder =>
        {
            builder.OpenComponent<ToolbarRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<ToolbarButton>(0);
                inner.AddAttribute(1, "Render", (RenderFragment<RenderProps<ToolbarButtonState>>)(props => b =>
                {
                    b.OpenElement(0, "span");
                    b.AddMultipleAttributes(1, props.Attributes);
                    b.AddContent(2, props.ChildContent);
                    b.CloseElement();
                }));
                inner.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Custom")));
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var cut = Render(fragment);
        cut.Find("span[type='button']").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateToolbarButtonInRoot(
            childContent: b => b.AddContent(0, "Click Me")));
        cut.Markup.ShouldContain("Click Me");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateToolbarButtonInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-custom", "value" }
            }));
        var element = cut.Find("button");
        element.GetAttribute("data-custom").ShouldBe("value");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateToolbarButtonInRoot(classValue: _ => "btn-class"));
        var element = cut.Find("button");
        element.GetAttribute("class").ShouldContain("btn-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateToolbarButtonInRoot(styleValue: _ => "color: red"));
        var element = cut.Find("button");
        element.GetAttribute("style").ShouldContain("color: red");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateToolbarButtonInRoot(
            classValue: _ => "dynamic",
            additionalAttributes: new Dictionary<string, object> { { "class", "static" } }));
        var element = cut.Find("button");
        var classAttr = element.GetAttribute("class");
        classAttr.ShouldContain("static");
        classAttr.ShouldContain("dynamic");
        return Task.CompletedTask;
    }

    // Native button attributes

    [Fact]
    public Task NativeButton_HasTypeButton()
    {
        var cut = Render(CreateToolbarButtonInRoot(nativeButton: true));
        var element = cut.Find("button");
        element.GetAttribute("type").ShouldBe("button");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_HasDisabledAttributeWhenDisabled()
    {
        var cut = Render(CreateToolbarButtonInRoot(
            buttonDisabled: true, focusableWhenDisabled: false, nativeButton: true));
        var element = cut.Find("button");
        element.HasAttribute("disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_HasAriaDisabledWhenDisabledAndFocusable()
    {
        var cut = Render(CreateToolbarButtonInRoot(
            buttonDisabled: true, focusableWhenDisabled: true, nativeButton: true));
        var element = cut.Find("button");
        element.GetAttribute("aria-disabled").ShouldBe("true");
        element.HasAttribute("disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_DoesNotHaveRoleButton()
    {
        var cut = Render(CreateToolbarButtonInRoot(nativeButton: true));
        var element = cut.Find("button");
        element.HasAttribute("role").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // Non-native button attributes

    [Fact]
    public Task NonNativeButton_RendersAsDivElement()
    {
        var cut = Render(CreateToolbarButtonInRoot(nativeButton: false));
        var element = cut.Find("[role='button']");
        element.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_HasRoleButton()
    {
        var cut = Render(CreateToolbarButtonInRoot(nativeButton: false));
        var element = cut.Find("[role='button']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_DoesNotHaveTypeButton()
    {
        var cut = Render(CreateToolbarButtonInRoot(nativeButton: false));
        var element = cut.Find("[role='button']");
        element.HasAttribute("type").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_HasAriaDisabledWhenDisabled()
    {
        var cut = Render(CreateToolbarButtonInRoot(
            nativeButton: false, buttonDisabled: true));
        var element = cut.Find("[role='button']");
        element.GetAttribute("aria-disabled").ShouldBe("true");
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataOrientationFromRoot()
    {
        var cut = Render(CreateToolbarButtonInRoot(rootOrientation: Orientation.Vertical));
        var element = cut.Find("button");
        element.GetAttribute("data-orientation").ShouldBe("vertical");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateToolbarButtonInRoot(buttonDisabled: true));
        var element = cut.Find("button");
        element.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataDisabledWhenNotDisabled()
    {
        var cut = Render(CreateToolbarButtonInRoot());
        var element = cut.Find("button");
        element.HasAttribute("data-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataFocusableByDefault()
    {
        var cut = Render(CreateToolbarButtonInRoot());
        var element = cut.Find("button");
        element.HasAttribute("data-focusable").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataFocusableWhenFocusableWhenDisabledFalse()
    {
        var cut = Render(CreateToolbarButtonInRoot(focusableWhenDisabled: false));
        var element = cut.Find("button");
        element.HasAttribute("data-focusable").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // Disabled cascading

    [Fact]
    public Task InheritsDisabledFromRoot()
    {
        var cut = Render(CreateToolbarButtonInRoot(rootDisabled: true));
        var element = cut.Find("button");
        element.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task InheritsDisabledFromGroup()
    {
        var cut = Render(CreateToolbarButtonInGroup(groupDisabled: true));
        var element = cut.Find("button");
        element.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task OwnDisabledTakesPrecedence()
    {
        var cut = Render(CreateToolbarButtonInRoot(buttonDisabled: true));
        var element = cut.Find("button");
        element.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    // State cascading

    [Fact]
    public Task ClassValueReceivesToolbarButtonState()
    {
        ToolbarButtonState? capturedState = null;
        var cut = Render(CreateToolbarButtonInRoot(
            buttonDisabled: true,
            rootOrientation: Orientation.Vertical,
            focusableWhenDisabled: true,
            classValue: state =>
            {
                capturedState = state;
                return "test";
            }));

        capturedState.ShouldNotBeNull();
        capturedState!.Disabled.ShouldBeTrue();
        capturedState!.Orientation.ShouldBe(Orientation.Vertical);
        capturedState!.Focusable.ShouldBeTrue();
        return Task.CompletedTask;
    }

    // Validation

    [Fact]
    public Task ThrowsWhenNotInsideToolbarRoot()
    {
        Should.Throw<InvalidOperationException>(() =>
        {
            Render(builder =>
            {
                builder.OpenComponent<ToolbarButton>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Orphan")));
                builder.CloseComponent();
            });
        });

        return Task.CompletedTask;
    }

}
