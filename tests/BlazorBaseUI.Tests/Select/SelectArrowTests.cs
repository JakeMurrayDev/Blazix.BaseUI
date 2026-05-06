using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Select;

public class SelectArrowTests : BunitContext, ISelectArrowContract
{
    public SelectArrowTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSelectModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private const string ArrowSelector = "[data-testid='arrow']";

    private static RenderFragment CreateArrowInSelect(
        bool defaultOpen = true,
        bool alignItemWithTrigger = false,
        RenderFragment<RenderProps<SelectArrowState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<SelectArrowState, string>? classValue = null,
        Func<SelectArrowState, string>? styleValue = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "T")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "AlignItemWithTrigger", alignItemWithTrigger);
                innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<SelectArrow>(0);
                        var attrIndex = 1;
                        if (render is not null)
                            popupBuilder.AddAttribute(attrIndex++, "Render", render);
                        if (classValue is not null)
                            popupBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                        if (styleValue is not null)
                            popupBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                        if (additionalAttributes is not null)
                            popupBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);
                        popupBuilder.AddAttribute(attrIndex, "data-testid", "arrow");
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
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateArrowInSelect());

        var arrow = cut.Find(ArrowSelector);
        arrow.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<SelectArrowState>> render = props => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateArrowInSelect(render: render));

        var arrow = cut.Find(ArrowSelector);
        arrow.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateArrowInSelect(
            additionalAttributes: new Dictionary<string, object>
            {
                { "aria-label", "Arrow" },
                { "data-custom", "custom-value" }
            }
        ));

        var arrow = cut.Find(ArrowSelector);
        arrow.GetAttribute("aria-label").ShouldBe("Arrow");
        arrow.GetAttribute("data-custom").ShouldBe("custom-value");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreateArrowInSelect(
            classValue: state => state.Open ? "open-class" : "closed-class"
        ));

        var arrow = cut.Find(ArrowSelector);
        arrow.GetAttribute("class")!.ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreateArrowInSelect(
            styleValue: _ => "color: red"
        ));

        var arrow = cut.Find(ArrowSelector);
        arrow.GetAttribute("style")!.ShouldContain("color: red");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaHiddenTrue()
    {
        var cut = Render(CreateArrowInSelect());

        var arrow = cut.Find(ArrowSelector);
        arrow.GetAttribute("aria-hidden").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task EmitsDataSide()
    {
        var cut = Render(CreateArrowInSelect());

        var arrow = cut.Find(ArrowSelector);
        arrow.HasAttribute("data-side").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task EmitsDataAlign()
    {
        var cut = Render(CreateArrowInSelect());

        var arrow = cut.Find(ArrowSelector);
        arrow.HasAttribute("data-align").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task EmitsDataOpenWhenOpen()
    {
        var cut = Render(CreateArrowInSelect(defaultOpen: true));

        var arrow = cut.Find(ArrowSelector);
        arrow.HasAttribute("data-open").ShouldBeTrue();
        arrow.HasAttribute("data-closed").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task EmitsDataUncenteredWhenArrowUncentered()
    {
        var cut = Render(CreateArrowInSelect(defaultOpen: true));

        var positionerComp = cut.FindComponent<SelectPositioner>();
        positionerComp.Instance.OnPositionUpdated("bottom", "center", false, true);

        cut.FindComponent<SelectArrow>().Render();

        var arrow = cut.Find(ArrowSelector);
        arrow.HasAttribute("data-uncentered").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotEmitDataAnchorHidden()
    {
        var cut = Render(CreateArrowInSelect(defaultOpen: true));

        var positionerComp = cut.FindComponent<SelectPositioner>();
        positionerComp.Instance.OnPositionUpdated("bottom", "center", true, false);

        cut.FindComponent<SelectArrow>().Render();

        var arrow = cut.Find(ArrowSelector);
        arrow.HasAttribute("data-anchor-hidden").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderWhenAlignItemWithTriggerActive()
    {
        var cut = Render(CreateArrowInSelect(defaultOpen: true, alignItemWithTrigger: true));

        cut.FindAll(ArrowSelector).Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RegistersArrowElementWithPositioner()
    {
        var cut = Render(CreateArrowInSelect(defaultOpen: true));

        var arrowComp = cut.FindComponent<SelectArrow>();
        arrowComp.Instance.Element.ShouldNotBeNull();

        var positionerContext = cut.FindComponent<SelectPositioner>().Instance.positionerContext;
        var registered = positionerContext.GetArrowElement();
        registered.ShouldNotBeNull();
        registered!.Value.Id.ShouldBe(arrowComp.Instance.Element!.Value.Id);

        return Task.CompletedTask;
    }
}
