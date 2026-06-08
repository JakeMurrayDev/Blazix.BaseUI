using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Blazix.BaseUI.Tests.Toolbar;

public class ToolbarRootTests : BunitContext, IToolbarRootContract
{
    public ToolbarRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupToolbarModule(JSInterop);
        Services.AddLogging();
    }

    private RenderFragment CreateToolbarRoot(
        bool disabled = false,
        Orientation orientation = Orientation.Horizontal,
        bool loopFocus = true,
        Func<ToolbarRootState, string>? classValue = null,
        Func<ToolbarRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<ToolbarRoot>(0);
            var seq = 1;
            builder.AddAttribute(seq++, "Disabled", disabled);
            builder.AddAttribute(seq++, "Orientation", orientation);
            builder.AddAttribute(seq++, "LoopFocus", loopFocus);
            if (classValue is not null)
                builder.AddAttribute(seq++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(seq++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(seq++, "AdditionalAttributes", additionalAttributes);
            if (childContent is not null)
                builder.AddAttribute(seq++, "ChildContent", childContent);
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateToolbarRootWithButton(
        bool disabled = false,
        Orientation orientation = Orientation.Horizontal,
        bool buttonDisabled = false)
    {
        return CreateToolbarRoot(
            disabled: disabled,
            orientation: orientation,
            childContent: inner =>
            {
                inner.OpenComponent<ToolbarButton>(0);
                inner.AddAttribute(1, "Disabled", buttonDisabled);
                inner.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Test Button")));
                inner.CloseComponent();
            });
    }

    // Rendering

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateToolbarRoot());
        var element = cut.Find("[role='toolbar']");
        element.TagName.ShouldBe("DIV");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRenderFragment()
    {
        var fragment = (RenderFragment)(builder =>
        {
            builder.OpenComponent<ToolbarRoot>(0);
            builder.AddAttribute(1, "Render", (RenderFragment<RenderProps<ToolbarRootState>>)(props => b =>
            {
                b.OpenElement(0, "section");
                b.AddMultipleAttributes(1, props.Attributes);
                b.AddContent(2, props.ChildContent);
                b.CloseElement();
            }));
            builder.CloseComponent();
        });

        var cut = Render(fragment);
        cut.Find("section[role='toolbar']").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateToolbarRoot(
            childContent: b => b.AddContent(0, "Hello Toolbar")));
        cut.Markup.ShouldContain("Hello Toolbar");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateToolbarRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-custom", "value" },
                { "aria-label", "My Toolbar" }
            }));
        var element = cut.Find("[role='toolbar']");
        element.GetAttribute("data-custom").ShouldBe("value");
        element.GetAttribute("aria-label").ShouldBe("My Toolbar");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateToolbarRoot(classValue: _ => "custom-class"));
        var element = cut.Find("[role='toolbar']");
        element.GetAttribute("class").ShouldContain("custom-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateToolbarRoot(styleValue: _ => "gap: 8px"));
        var element = cut.Find("[role='toolbar']");
        element.GetAttribute("style").ShouldContain("gap: 8px");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateToolbarRoot(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "class", "static-class" }
            }));
        var element = cut.Find("[role='toolbar']");
        var classAttr = element.GetAttribute("class");
        classAttr.ShouldContain("static-class");
        classAttr.ShouldContain("dynamic-class");
        return Task.CompletedTask;
    }

    // ARIA

    [Fact]
    public Task HasRoleToolbar()
    {
        var cut = Render(CreateToolbarRoot());
        var element = cut.Find("[role='toolbar']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaOrientationHorizontalByDefault()
    {
        var cut = Render(CreateToolbarRoot());
        var element = cut.Find("[role='toolbar']");
        element.GetAttribute("aria-orientation").ShouldBe("horizontal");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaOrientationVerticalWhenVertical()
    {
        var cut = Render(CreateToolbarRoot(orientation: Orientation.Vertical));
        var element = cut.Find("[role='toolbar']");
        element.GetAttribute("aria-orientation").ShouldBe("vertical");
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataOrientationHorizontalByDefault()
    {
        var cut = Render(CreateToolbarRoot());
        var element = cut.Find("[role='toolbar']");
        element.GetAttribute("data-orientation").ShouldBe("horizontal");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientationVerticalWhenVertical()
    {
        var cut = Render(CreateToolbarRoot(orientation: Orientation.Vertical));
        var element = cut.Find("[role='toolbar']");
        element.GetAttribute("data-orientation").ShouldBe("vertical");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateToolbarRoot(disabled: true));
        var element = cut.Find("[role='toolbar']");
        element.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataDisabledWhenNotDisabled()
    {
        var cut = Render(CreateToolbarRoot());
        var element = cut.Find("[role='toolbar']");
        element.HasAttribute("data-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // State cascading

    [Fact]
    public Task ClassValueReceivesToolbarRootState()
    {
        ToolbarRootState? capturedState = null;
        var cut = Render(CreateToolbarRoot(
            classValue: state =>
            {
                capturedState = state;
                return "test";
            }));

        capturedState.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ClassValueReceivesDisabledTrue()
    {
        ToolbarRootState? capturedState = null;
        var cut = Render(CreateToolbarRoot(
            disabled: true,
            classValue: state =>
            {
                capturedState = state;
                return "test";
            }));

        capturedState.ShouldNotBeNull();
        capturedState!.Disabled.ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ClassValueReceivesOrientationVertical()
    {
        ToolbarRootState? capturedState = null;
        var cut = Render(CreateToolbarRoot(
            orientation: Orientation.Vertical,
            classValue: state =>
            {
                capturedState = state;
                return "test";
            }));

        capturedState.ShouldNotBeNull();
        capturedState!.Orientation.ShouldBe(Orientation.Vertical);
        return Task.CompletedTask;
    }

    // Element reference

    [Fact]
    public Task ExposesElementReference()
    {
        var cut = Render(CreateToolbarRoot());
        var component = cut.FindComponent<ToolbarRoot>();
        component.Instance.Element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // Context cascading

    [Fact]
    public Task CascadesDisabledToButton()
    {
        var cut = Render(CreateToolbarRootWithButton(disabled: true));
        var button = cut.Find("button");
        button.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task CascadesOrientationToButton()
    {
        var cut = Render(CreateToolbarRootWithButton(orientation: Orientation.Vertical));
        var button = cut.Find("button");
        button.GetAttribute("data-orientation").ShouldBe("vertical");
        return Task.CompletedTask;
    }
}
