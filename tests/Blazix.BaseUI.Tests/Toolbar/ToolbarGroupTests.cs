using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Blazix.BaseUI.Tests.Toolbar;

public class ToolbarGroupTests : BunitContext, IToolbarGroupContract
{
    public ToolbarGroupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupToolbarModule(JSInterop);
        Services.AddLogging();
    }

    private RenderFragment CreateToolbarGroupInRoot(
        bool rootDisabled = false,
        Orientation rootOrientation = Orientation.Horizontal,
        bool groupDisabled = false,
        Func<ToolbarRootState, string>? classValue = null,
        Func<ToolbarRootState, string>? styleValue = null,
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
                inner.OpenComponent<ToolbarGroup>(0);
                var seq = 1;
                inner.AddAttribute(seq++, "Disabled", groupDisabled);
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

    private RenderFragment CreateToolbarGroupWithButton(
        bool rootDisabled = false,
        bool groupDisabled = false)
    {
        return CreateToolbarGroupInRoot(
            rootDisabled: rootDisabled,
            groupDisabled: groupDisabled,
            childContent: inner =>
            {
                inner.OpenComponent<ToolbarButton>(0);
                inner.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Group Button")));
                inner.CloseComponent();
            });
    }

    // Rendering

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateToolbarGroupInRoot());
        var element = cut.Find("[role='group']");
        element.TagName.ShouldBe("DIV");
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
                inner.OpenComponent<ToolbarGroup>(0);
                inner.AddAttribute(1, "Render", (RenderFragment<RenderProps<ToolbarRootState>>)(props => b =>
                {
                    b.OpenElement(0, "section");
                    b.AddMultipleAttributes(1, props.Attributes);
                    b.AddContent(2, props.ChildContent);
                    b.CloseElement();
                }));
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var cut = Render(fragment);
        cut.Find("section[role='group']").ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateToolbarGroupInRoot(
            childContent: b => b.AddContent(0, "Group Content")));
        cut.Markup.ShouldContain("Group Content");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateToolbarGroupInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-custom", "value" }
            }));
        var element = cut.Find("[role='group']");
        element.GetAttribute("data-custom").ShouldBe("value");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateToolbarGroupInRoot(classValue: _ => "group-class"));
        var element = cut.Find("[role='group']");
        element.GetAttribute("class").ShouldContain("group-class");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateToolbarGroupInRoot(styleValue: _ => "gap: 4px"));
        var element = cut.Find("[role='group']");
        element.GetAttribute("style").ShouldContain("gap: 4px");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateToolbarGroupInRoot(
            classValue: _ => "dynamic",
            additionalAttributes: new Dictionary<string, object> { { "class", "static" } }));
        var element = cut.Find("[role='group']");
        var classAttr = element.GetAttribute("class");
        classAttr.ShouldContain("static");
        classAttr.ShouldContain("dynamic");
        return Task.CompletedTask;
    }

    // ARIA

    [Fact]
    public Task HasRoleGroup()
    {
        var cut = Render(CreateToolbarGroupInRoot());
        var element = cut.Find("[role='group']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataOrientationFromRoot()
    {
        var cut = Render(CreateToolbarGroupInRoot(rootOrientation: Orientation.Vertical));
        var element = cut.Find("[role='group']");
        element.GetAttribute("data-orientation").ShouldBe("vertical");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateToolbarGroupInRoot(groupDisabled: true));
        var element = cut.Find("[role='group']");
        element.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataDisabledWhenNotDisabled()
    {
        var cut = Render(CreateToolbarGroupInRoot());
        var element = cut.Find("[role='group']");
        element.HasAttribute("data-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenRootDisabled()
    {
        var cut = Render(CreateToolbarGroupInRoot(rootDisabled: true));
        var element = cut.Find("[role='group']");
        element.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    // Disabled cascading

    [Fact]
    public Task CascadesDisabledToChildren()
    {
        var cut = Render(CreateToolbarGroupWithButton(groupDisabled: true));
        var button = cut.Find("button");
        button.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    // State cascading

    [Fact]
    public Task ClassValueReceivesToolbarRootState()
    {
        ToolbarRootState? capturedState = null;
        var cut = Render(CreateToolbarGroupInRoot(
            groupDisabled: true,
            rootOrientation: Orientation.Vertical,
            classValue: state =>
            {
                capturedState = state;
                return "test";
            }));

        capturedState.ShouldNotBeNull();
        capturedState!.Disabled.ShouldBeTrue();
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
                builder.OpenComponent<ToolbarGroup>(0);
                builder.CloseComponent();
            });
        });

        return Task.CompletedTask;
    }

}
