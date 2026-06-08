using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Blazix.BaseUI.Tests.Toolbar;

public class ToolbarInputTests : BunitContext, IToolbarInputContract
{
    public ToolbarInputTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupToolbarModule(JSInterop);
        Services.AddLogging();
    }

    private RenderFragment CreateToolbarInputInRoot(
        bool rootDisabled = false,
        Orientation rootOrientation = Orientation.Horizontal,
        bool inputDisabled = false,
        bool focusableWhenDisabled = true,
        string? defaultValue = null,
        Func<ToolbarInputState, string>? classValue = null,
        Func<ToolbarInputState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<ToolbarRoot>(0);
            builder.AddAttribute(1, "Disabled", rootDisabled);
            builder.AddAttribute(2, "Orientation", rootOrientation);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<ToolbarInput>(0);
                var seq = 1;
                inner.AddAttribute(seq++, "Disabled", inputDisabled);
                inner.AddAttribute(seq++, "FocusableWhenDisabled", focusableWhenDisabled);
                if (defaultValue is not null)
                    inner.AddAttribute(seq++, "DefaultValue", defaultValue);
                if (classValue is not null)
                    inner.AddAttribute(seq++, "ClassValue", classValue);
                if (styleValue is not null)
                    inner.AddAttribute(seq++, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    inner.AddAttribute(seq++, "AdditionalAttributes", additionalAttributes);
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateToolbarInputInGroup(
        bool rootDisabled = false,
        bool groupDisabled = false,
        bool inputDisabled = false)
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
                    groupInner.OpenComponent<ToolbarInput>(0);
                    groupInner.AddAttribute(1, "Disabled", inputDisabled);
                    groupInner.CloseComponent();
                }));
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsInputByDefault()
    {
        var cut = Render(CreateToolbarInputInRoot());
        var element = cut.Find("input");
        element.TagName.ShouldBe("INPUT");
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
                inner.OpenComponent<ToolbarInput>(0);
                inner.AddAttribute(1, "Render", (RenderFragment<RenderProps<ToolbarInputState>>)(props => b =>
                {
                    b.OpenElement(0, "textarea");
                    b.AddMultipleAttributes(1, props.Attributes);
                    b.CloseElement();
                }));
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var cut = Render(fragment);
        cut.Find("textarea[data-orientation]").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateToolbarInputInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "placeholder", "Search..." },
                { "data-custom", "value" }
            }));
        var element = cut.Find("input");
        element.GetAttribute("placeholder").ShouldBe("Search...");
        element.GetAttribute("data-custom").ShouldBe("value");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateToolbarInputInRoot(classValue: _ => "input-class"));
        var element = cut.Find("input");
        element.GetAttribute("class").ShouldContain("input-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateToolbarInputInRoot(styleValue: _ => "width: 200px"));
        var element = cut.Find("input");
        element.GetAttribute("style").ShouldContain("width: 200px");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateToolbarInputInRoot(
            classValue: _ => "dynamic",
            additionalAttributes: new Dictionary<string, object> { { "class", "static" } }));
        var element = cut.Find("input");
        var classAttr = element.GetAttribute("class");
        classAttr.ShouldContain("static");
        classAttr.ShouldContain("dynamic");
        return Task.CompletedTask;
    }

    // ARIA / Disabled

    [Fact]
    public Task HasAriaDisabledWhenDisabledAndFocusable()
    {
        var cut = Render(CreateToolbarInputInRoot(
            inputDisabled: true, focusableWhenDisabled: true));
        var element = cut.Find("input");
        element.GetAttribute("aria-disabled").ShouldBe("true");
        element.HasAttribute("disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDisabledAttributeWhenDisabledAndNotFocusable()
    {
        var cut = Render(CreateToolbarInputInRoot(
            inputDisabled: true, focusableWhenDisabled: false));
        var element = cut.Find("input");
        element.HasAttribute("disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDisabledWhenNotDisabled()
    {
        var cut = Render(CreateToolbarInputInRoot());
        var element = cut.Find("input");
        element.HasAttribute("disabled").ShouldBeFalse();
        element.HasAttribute("aria-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // DefaultValue

    [Fact]
    public Task RendersWithDefaultValue()
    {
        var cut = Render(CreateToolbarInputInRoot(defaultValue: "hello"));
        var element = cut.Find("input");
        element.GetAttribute("value").ShouldBe("hello");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderValueWhenDefaultValueNull()
    {
        var cut = Render(CreateToolbarInputInRoot());
        var element = cut.Find("input");
        element.HasAttribute("value").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataOrientationFromRoot()
    {
        var cut = Render(CreateToolbarInputInRoot(rootOrientation: Orientation.Vertical));
        var element = cut.Find("input");
        element.GetAttribute("data-orientation").ShouldBe("vertical");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateToolbarInputInRoot(inputDisabled: true));
        var element = cut.Find("input");
        element.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataDisabledWhenNotDisabled()
    {
        var cut = Render(CreateToolbarInputInRoot());
        var element = cut.Find("input");
        element.HasAttribute("data-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataFocusableByDefault()
    {
        var cut = Render(CreateToolbarInputInRoot());
        var element = cut.Find("input");
        element.HasAttribute("data-focusable").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataFocusableWhenFocusableWhenDisabledFalse()
    {
        var cut = Render(CreateToolbarInputInRoot(focusableWhenDisabled: false));
        var element = cut.Find("input");
        element.HasAttribute("data-focusable").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // Disabled cascading

    [Fact]
    public Task InheritsDisabledFromRoot()
    {
        var cut = Render(CreateToolbarInputInRoot(rootDisabled: true));
        var element = cut.Find("input");
        element.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task InheritsDisabledFromGroup()
    {
        var cut = Render(CreateToolbarInputInGroup(groupDisabled: true));
        var element = cut.Find("input");
        element.HasAttribute("data-disabled").ShouldBeTrue();
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
                builder.OpenComponent<ToolbarInput>(0);
                builder.CloseComponent();
            });
        });

        return Task.CompletedTask;
    }

}
