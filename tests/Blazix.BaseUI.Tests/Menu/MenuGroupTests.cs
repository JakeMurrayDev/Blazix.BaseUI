namespace Blazix.BaseUI.Tests.Menu;

public class MenuGroupTests : BunitContext, IMenuGroupContract
{
    public MenuGroupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateMenuGroup(
        RenderFragment? childContent = null,
        Func<MenuGroupState, string?>? classValue = null,
        Func<MenuGroupState, string?>? styleValue = null,
        RenderFragment<RenderProps<MenuGroupState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<MenuGroup>(0);

            if (childContent is not null)
                builder.AddAttribute(1, "ChildContent", childContent);

            if (classValue is not null)
                builder.AddAttribute(2, "ClassValue", classValue);

            if (styleValue is not null)
                builder.AddAttribute(3, "StyleValue", styleValue);

            if (render is not null)
                builder.AddAttribute(4, "Render", render);

            if (additionalAttributes is not null)
                builder.AddMultipleAttributes(5, additionalAttributes);

            builder.CloseComponent();
        };
    }

    private RenderFragment CreateMenuGroupWithLabel(string labelText = "Test group")
    {
        return builder =>
        {
            builder.OpenComponent<MenuGroup>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(groupBuilder =>
            {
                groupBuilder.OpenComponent<MenuGroupLabel>(0);
                groupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, labelText)));
                groupBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateMenuGroup(
            childContent: b => b.AddContent(0, "Content")));

        var group = cut.Find("[role='group']");
        group.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<MenuGroupState>> render = props => builder =>
        {
            builder.OpenElement(0, "section");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback!);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateMenuGroup(
            render: render,
            childContent: b => b.AddContent(0, "Content")));

        var group = cut.Find("[role='group']");
        group.TagName.ShouldBe("SECTION");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var attrs = new Dictionary<string, object> { ["data-testid"] = "my-group" };

        var cut = Render(CreateMenuGroup(
            additionalAttributes: attrs,
            childContent: b => b.AddContent(0, "Content")));

        var group = cut.Find("[role='group']");
        group.GetAttribute("data-testid").ShouldBe("my-group");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateMenuGroup(
            classValue: _ => "custom-class",
            childContent: b => b.AddContent(0, "Content")));

        var group = cut.Find("[role='group']");
        group.ClassList.ShouldContain("custom-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateMenuGroup(
            styleValue: _ => "color: red",
            childContent: b => b.AddContent(0, "Content")));

        var group = cut.Find("[role='group']");
        group.GetAttribute("style")!.ShouldContain("color: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRoleGroup()
    {
        var cut = Render(CreateMenuGroup(
            childContent: b => b.AddContent(0, "Content")));

        var group = cut.Find("[role='group']");
        group.GetAttribute("role").ShouldBe("group");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaLabelledByWhenLabelPresent()
    {
        var cut = Render(CreateMenuGroupWithLabel("Test group"));

        // OnAfterRender sets the label ID, triggering StateHasChanged on the group.
        // A second render picks up aria-labelledby.
        cut.Render();

        var group = cut.Find("[role='group']");
        group.HasAttribute("aria-labelledby").ShouldBeTrue();

        var label = group.QuerySelector("[role='presentation']");
        label.ShouldNotBeNull();

        var labelId = label!.GetAttribute("id")!;
        labelId.ShouldNotBeNullOrEmpty();

        group.GetAttribute("aria-labelledby").ShouldBe(labelId);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ExposesElementReference()
    {
        var cut = Render(CreateMenuGroup(
            childContent: b => b.AddContent(0, "Content")));

        var component = cut.FindComponent<MenuGroup>();
        component.Instance.Element.ShouldNotBeNull();

        return Task.CompletedTask;
    }
}
