using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Blazix.BaseUI.Tests.Toolbar;

public class ToolbarLinkTests : BunitContext, IToolbarLinkContract
{
    public ToolbarLinkTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupToolbarModule(JSInterop);
        Services.AddLogging();
    }

    private RenderFragment CreateToolbarLinkInRoot(
        bool rootDisabled = false,
        Orientation rootOrientation = Orientation.Horizontal,
        Func<ToolbarLinkState, string>? classValue = null,
        Func<ToolbarLinkState, string>? styleValue = null,
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
                inner.OpenComponent<ToolbarLink>(0);
                var seq = 1;
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

    // Rendering

    [Fact]
    public Task RendersAsAnchorByDefault()
    {
        var cut = Render(CreateToolbarLinkInRoot(
            childContent: b => b.AddContent(0, "Link")));
        var element = cut.Find("a");
        element.TagName.ShouldBe("A");
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
                inner.OpenComponent<ToolbarLink>(0);
                inner.AddAttribute(1, "Render", (RenderFragment<RenderProps<ToolbarLinkState>>)(props => b =>
                {
                    b.OpenElement(0, "span");
                    b.AddMultipleAttributes(1, props.Attributes);
                    b.AddContent(2, props.ChildContent);
                    b.CloseElement();
                }));
                inner.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Link")));
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var cut = Render(fragment);
        cut.Find("span[data-orientation]").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateToolbarLinkInRoot(
            childContent: b => b.AddContent(0, "My Link")));
        cut.Markup.ShouldContain("My Link");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateToolbarLinkInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "href", "https://example.com" },
                { "data-custom", "value" }
            },
            childContent: b => b.AddContent(0, "Link")));
        var element = cut.Find("a");
        element.GetAttribute("href").ShouldBe("https://example.com");
        element.GetAttribute("data-custom").ShouldBe("value");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateToolbarLinkInRoot(
            classValue: _ => "link-class",
            childContent: b => b.AddContent(0, "Link")));
        var element = cut.Find("a");
        element.GetAttribute("class").ShouldContain("link-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateToolbarLinkInRoot(
            styleValue: _ => "color: blue",
            childContent: b => b.AddContent(0, "Link")));
        var element = cut.Find("a");
        element.GetAttribute("style").ShouldContain("color: blue");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateToolbarLinkInRoot(
            classValue: _ => "dynamic",
            additionalAttributes: new Dictionary<string, object> { { "class", "static" } },
            childContent: b => b.AddContent(0, "Link")));
        var element = cut.Find("a");
        var classAttr = element.GetAttribute("class");
        classAttr.ShouldContain("static");
        classAttr.ShouldContain("dynamic");
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataOrientationFromRoot()
    {
        var cut = Render(CreateToolbarLinkInRoot(
            rootOrientation: Orientation.Vertical,
            childContent: b => b.AddContent(0, "Link")));
        var element = cut.Find("a");
        element.GetAttribute("data-orientation").ShouldBe("vertical");
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataDisabledWhenRootDisabled()
    {
        var cut = Render(CreateToolbarLinkInRoot(
            rootDisabled: true,
            childContent: b => b.AddContent(0, "Link")));
        var element = cut.Find("a");
        element.HasAttribute("data-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // State cascading

    [Fact]
    public Task ClassValueReceivesToolbarLinkState()
    {
        ToolbarLinkState? capturedState = null;
        var cut = Render(CreateToolbarLinkInRoot(
            classValue: state =>
            {
                capturedState = state;
                return "test";
            },
            childContent: b => b.AddContent(0, "Link")));

        capturedState.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ClassValueReceivesOrientationFromRoot()
    {
        ToolbarLinkState? capturedState = null;
        var cut = Render(CreateToolbarLinkInRoot(
            rootOrientation: Orientation.Vertical,
            classValue: state =>
            {
                capturedState = state;
                return "test";
            },
            childContent: b => b.AddContent(0, "Link")));

        capturedState.ShouldNotBeNull();
        capturedState!.Orientation.ShouldBe(Orientation.Vertical);
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
                builder.OpenComponent<ToolbarLink>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Orphan")));
                builder.CloseComponent();
            });
        });

        return Task.CompletedTask;
    }

}
