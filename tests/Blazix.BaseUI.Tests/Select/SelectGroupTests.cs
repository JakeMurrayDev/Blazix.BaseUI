namespace Blazix.BaseUI.Tests.Select;

public class SelectGroupTests : BunitContext, ISelectGroupContract
{
    public SelectGroupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSelectModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateSelectWithGroup(bool defaultOpen = true)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Select")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<SelectGroup>(0);
                        popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(groupBuilder =>
                        {
                            groupBuilder.OpenComponent<SelectGroupLabel>(0);
                            groupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Fruits")));
                            groupBuilder.CloseComponent();

                            groupBuilder.OpenComponent<SelectItem<string>>(10);
                            groupBuilder.AddAttribute(11, "Value", "apple");
                            groupBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                            groupBuilder.CloseComponent();
                        }));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task ShouldRenderGroupWithLabel()
    {
        var cut = Render(CreateSelectWithGroup(defaultOpen: true));

        var group = cut.Find("[role='group']");
        group.ShouldNotBeNull();

        var label = group.QuerySelector("[role='presentation']");
        label.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ShouldAssociateLabelWithGroup()
    {
        var cut = Render(CreateSelectWithGroup(defaultOpen: true));

        // After initial render, SelectGroupLabel.OnAfterRender calls SetLabelId,
        // which triggers StateHasChanged on SelectGroup. Force a re-render to
        // pick up the aria-labelledby attribute on the group element.
        cut.Render();

        var group = cut.Find("[role='group']");
        group.HasAttribute("aria-labelledby").ShouldBeTrue();

        var label = group.QuerySelector("[role='presentation']");
        label.ShouldNotBeNull();
        var labelId = label!.GetAttribute("id");
        labelId.ShouldNotBeNullOrEmpty();

        group.GetAttribute("aria-labelledby").ShouldBe(labelId);

        return Task.CompletedTask;
    }
}
