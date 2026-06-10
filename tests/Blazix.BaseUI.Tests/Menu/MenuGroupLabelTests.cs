namespace Blazix.BaseUI.Tests.Menu;

public class MenuGroupLabelTests : BunitContext, IMenuGroupLabelContract
{
    public MenuGroupLabelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateMenuGroupWithLabel(
        string? labelText = "Test label",
        Func<MenuGroupLabelState, string?>? classValue = null,
        Func<MenuGroupLabelState, string?>? styleValue = null,
        RenderFragment<RenderProps<MenuGroupLabelState>>? render = null,
        IReadOnlyDictionary<string, object>? labelAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<MenuGroup>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(groupBuilder =>
            {
                groupBuilder.OpenComponent<MenuGroupLabel>(0);

                if (labelText is not null)
                    groupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, labelText)));

                if (classValue is not null)
                    groupBuilder.AddAttribute(2, "ClassValue", classValue);

                if (styleValue is not null)
                    groupBuilder.AddAttribute(3, "StyleValue", styleValue);

                if (render is not null)
                    groupBuilder.AddAttribute(4, "Render", render);

                if (labelAttributes is not null)
                    groupBuilder.AddMultipleAttributes(5, labelAttributes);

                groupBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateMenuGroupWithLabel());

        var label = cut.Find("[role='presentation']");
        label.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<MenuGroupLabelState>> render = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback!);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateMenuGroupWithLabel(render: render));

        var label = cut.Find("[role='presentation']");
        label.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var attrs = new Dictionary<string, object> { ["data-testid"] = "my-label" };

        var cut = Render(CreateMenuGroupWithLabel(labelAttributes: attrs));

        var label = cut.Find("[role='presentation']");
        label.GetAttribute("data-testid").ShouldBe("my-label");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateMenuGroupWithLabel(classValue: _ => "label-class"));

        var label = cut.Find("[role='presentation']");
        label.ClassList.ShouldContain("label-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateMenuGroupWithLabel(styleValue: _ => "font-weight: bold"));

        var label = cut.Find("[role='presentation']");
        label.GetAttribute("style")!.ShouldContain("font-weight: bold");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRolePresentation()
    {
        var cut = Render(CreateMenuGroupWithLabel());

        var label = cut.Find("[role='presentation']");
        label.GetAttribute("role").ShouldBe("presentation");

        return Task.CompletedTask;
    }

    [Fact]
    public Task GeneratesIdAutomatically()
    {
        var cut = Render(CreateMenuGroupWithLabel());

        var label = cut.Find("[role='presentation']");
        var id = label.GetAttribute("id");
        id.ShouldNotBeNullOrEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesProvidedId()
    {
        var attrs = new Dictionary<string, object> { ["id"] = "custom-label-id" };

        var cut = Render(CreateMenuGroupWithLabel(labelAttributes: attrs));

        var label = cut.Find("[role='presentation']");
        label.GetAttribute("id").ShouldBe("custom-label-id");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AssociatesGeneratedIdWithGroupAriaLabelledBy()
    {
        var cut = Render(CreateMenuGroupWithLabel());

        // OnAfterRender sets the label ID, triggering StateHasChanged on the group.
        // A second render picks up aria-labelledby.
        cut.Render();

        var group = cut.Find("[role='group']");
        var label = cut.Find("[role='presentation']");

        var labelId = label.GetAttribute("id")!;
        labelId.ShouldNotBeNullOrEmpty();

        group.GetAttribute("aria-labelledby").ShouldBe(labelId);

        return Task.CompletedTask;
    }

    [Fact]
    public Task AssociatesProvidedIdWithGroupAriaLabelledBy()
    {
        var attrs = new Dictionary<string, object> { ["id"] = "my-group-label" };

        var cut = Render(CreateMenuGroupWithLabel(labelAttributes: attrs));

        // OnAfterRender sets the label ID, triggering StateHasChanged on the group.
        cut.Render();

        var group = cut.Find("[role='group']");
        group.GetAttribute("aria-labelledby").ShouldBe("my-group-label");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ExposesElementReference()
    {
        var cut = Render(CreateMenuGroupWithLabel());

        var component = cut.FindComponent<MenuGroupLabel>();
        component.Instance.Element.ShouldNotBeNull();

        return Task.CompletedTask;
    }
}
