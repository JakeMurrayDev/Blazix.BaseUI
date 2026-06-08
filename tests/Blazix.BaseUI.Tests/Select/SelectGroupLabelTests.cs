namespace Blazix.BaseUI.Tests.Select;

public class SelectGroupLabelTests : BunitContext, ISelectGroupLabelContract
{
    public SelectGroupLabelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateSelectGroupWithLabel(
        string? labelText = "Test label",
        Func<SelectGroupLabelState, string?>? classValue = null,
        Func<SelectGroupLabelState, string?>? styleValue = null,
        RenderFragment<RenderProps<SelectGroupLabelState>>? render = null,
        IReadOnlyDictionary<string, object>? labelAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectGroup>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(groupBuilder =>
            {
                groupBuilder.OpenComponent<SelectGroupLabel>(0);

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
        var cut = Render(CreateSelectGroupWithLabel());

        var label = cut.FindComponent<SelectGroupLabel>().Find("div");
        label.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<SelectGroupLabelState>> render = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback!);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateSelectGroupWithLabel(render: render));

        var label = cut.FindComponent<SelectGroupLabel>().Find("span");
        label.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var attrs = new Dictionary<string, object> { ["data-testid"] = "my-label" };

        var cut = Render(CreateSelectGroupWithLabel(labelAttributes: attrs));

        var label = cut.FindComponent<SelectGroupLabel>().Find("div");
        label.GetAttribute("data-testid").ShouldBe("my-label");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateSelectGroupWithLabel(classValue: _ => "label-class"));

        var label = cut.FindComponent<SelectGroupLabel>().Find("div");
        label.ClassList.ShouldContain("label-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateSelectGroupWithLabel(styleValue: _ => "font-weight: bold"));

        var label = cut.FindComponent<SelectGroupLabel>().Find("div");
        label.GetAttribute("style")!.ShouldContain("font-weight: bold");

        return Task.CompletedTask;
    }

    [Fact]
    public Task GeneratesIdAutomatically()
    {
        var cut = Render(CreateSelectGroupWithLabel());

        var label = cut.FindComponent<SelectGroupLabel>().Find("div");
        var id = label.GetAttribute("id");
        id.ShouldNotBeNullOrEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesProvidedId()
    {
        var attrs = new Dictionary<string, object> { ["id"] = "custom-label-id" };

        var cut = Render(CreateSelectGroupWithLabel(labelAttributes: attrs));

        var label = cut.FindComponent<SelectGroupLabel>().Find("div");
        label.GetAttribute("id").ShouldBe("custom-label-id");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AssociatesGeneratedIdWithGroupAriaLabelledBy()
    {
        var cut = Render(CreateSelectGroupWithLabel());

        // OnAfterRender sets the label ID, triggering StateHasChanged on the group.
        // A second render picks up aria-labelledby.
        cut.Render();

        var group = cut.Find("[role='group']");
        var label = cut.FindComponent<SelectGroupLabel>().Find("div");

        var labelId = label.GetAttribute("id")!;
        labelId.ShouldNotBeNullOrEmpty();

        group.GetAttribute("aria-labelledby").ShouldBe(labelId);

        return Task.CompletedTask;
    }

    [Fact]
    public Task AssociatesProvidedIdWithGroupAriaLabelledBy()
    {
        var attrs = new Dictionary<string, object> { ["id"] = "my-group-label" };

        var cut = Render(CreateSelectGroupWithLabel(labelAttributes: attrs));

        // OnAfterRender sets the label ID, triggering StateHasChanged on the group.
        cut.Render();

        var group = cut.Find("[role='group']");
        group.GetAttribute("aria-labelledby").ShouldBe("my-group-label");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ExposesElementReference()
    {
        var cut = Render(CreateSelectGroupWithLabel());

        var component = cut.FindComponent<SelectGroupLabel>();
        component.Instance.Element.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ThrowsWhenNotInsideSelectGroup()
    {
        Should.Throw<InvalidOperationException>(() => Render(builder =>
        {
            builder.OpenComponent<SelectGroupLabel>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Test")));
            builder.CloseComponent();
        }));

        return Task.CompletedTask;
    }

    [Fact]
    public Task CleansUpLabelIdOnDispose()
    {
        var cut = Render(CreateSelectGroupWithLabel());

        // Establish the aria-labelledby association
        cut.Render();

        var group = cut.Find("[role='group']");
        group.HasAttribute("aria-labelledby").ShouldBeTrue();

        // Dispose the label component, which calls SetLabelId(null)
        var labelComponent = cut.FindComponent<SelectGroupLabel>();
        ((IDisposable)labelComponent.Instance).Dispose();

        // The group should re-render without aria-labelledby
        cut.Render();

        group = cut.Find("[role='group']");
        group.HasAttribute("aria-labelledby").ShouldBeFalse();

        return Task.CompletedTask;
    }
}
