using Blazix.BaseUI.Select;
using Blazix.BaseUI.Tests.Contracts.Select;
using Blazix.BaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Blazix.BaseUI.Tests.Select;

public class SelectItemIndicatorTests : BunitContext, ISelectItemIndicatorContract
{
    public SelectItemIndicatorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSelectModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private RenderFragment CreateSelectWithIndicator(
        string? defaultValue = null,
        bool keepMounted = false,
        Func<SelectItemIndicatorState, string?>? indicatorClassValue = null,
        Func<SelectItemIndicatorState, string?>? indicatorStyleValue = null,
        IReadOnlyDictionary<string, object>? indicatorAdditionalAttributes = null,
        RenderFragment<RenderProps<SelectItemIndicatorState>>? indicatorRender = null,
        RenderFragment? indicatorChildContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultOpen", true);

            if (defaultValue is not null)
                builder.AddAttribute(2, "DefaultValue", defaultValue);

            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Select")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPortal>(10);
                innerBuilder.AddAttribute(11, "KeepMounted", true);
                innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<SelectPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<SelectPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenComponent<SelectItem<string>>(0);
                            popupBuilder.AddAttribute(1, "Value", "apple");
                            popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(itemBuilder =>
                            {
                                itemBuilder.OpenComponent<SelectItemIndicator>(0);
                                var attrIndex = 1;

                                if (keepMounted)
                                    itemBuilder.AddAttribute(attrIndex++, "KeepMounted", true);
                                if (indicatorClassValue is not null)
                                    itemBuilder.AddAttribute(attrIndex++, "ClassValue", indicatorClassValue);
                                if (indicatorStyleValue is not null)
                                    itemBuilder.AddAttribute(attrIndex++, "StyleValue", indicatorStyleValue);
                                if (indicatorAdditionalAttributes is not null)
                                    itemBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", indicatorAdditionalAttributes);
                                if (indicatorRender is not null)
                                    itemBuilder.AddAttribute(attrIndex++, "Render", indicatorRender);
                                if (indicatorChildContent is not null)
                                    itemBuilder.AddAttribute(attrIndex++, "ChildContent", indicatorChildContent);

                                itemBuilder.CloseComponent();
                            }));
                            popupBuilder.CloseComponent();
                        }));
                        posBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    private RenderFragment CreateSelectWithTwoIndicators(string? defaultValue = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultOpen", true);

            if (defaultValue is not null)
                builder.AddAttribute(2, "DefaultValue", defaultValue);

            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
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
                        popupBuilder.OpenComponent<SelectItem<string>>(0);
                        popupBuilder.AddAttribute(1, "Value", "apple");
                        popupBuilder.AddAttribute(2, "data-testid", "apple-item");
                        popupBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(itemBuilder =>
                        {
                            itemBuilder.OpenComponent<SelectItemText>(0);
                            itemBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                            itemBuilder.CloseComponent();

                            itemBuilder.OpenComponent<SelectItemIndicator>(10);
                            itemBuilder.AddAttribute(11, "data-testid", "apple-indicator");
                            itemBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, "A")));
                            itemBuilder.CloseComponent();
                        }));
                        popupBuilder.CloseComponent();

                        popupBuilder.OpenComponent<SelectItem<string>>(10);
                        popupBuilder.AddAttribute(11, "Value", "banana");
                        popupBuilder.AddAttribute(12, "data-testid", "banana-item");
                        popupBuilder.AddAttribute(13, "ChildContent", (RenderFragment)(itemBuilder =>
                        {
                            itemBuilder.OpenComponent<SelectItemText>(0);
                            itemBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Banana")));
                            itemBuilder.CloseComponent();

                            itemBuilder.OpenComponent<SelectItemIndicator>(10);
                            itemBuilder.AddAttribute(11, "data-testid", "banana-indicator");
                            itemBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, "B")));
                            itemBuilder.CloseComponent();
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

    // Rendering tests

    [Fact]
    public Task RendersAsSpanByDefault()
    {
        var cut = Render(CreateSelectWithIndicator(
            defaultValue: "apple",
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<SelectItemIndicatorState>> renderAsDiv = props => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateSelectWithIndicator(
            defaultValue: "apple",
            indicatorRender: renderAsDiv,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateSelectWithIndicator(
            defaultValue: "apple",
            indicatorAdditionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "indicator" },
                { "aria-label", "Selected indicator" }
            }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("aria-label").ShouldBe("Selected indicator");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateSelectWithIndicator(
            defaultValue: "apple",
            indicatorClassValue: _ => "indicator-class",
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("class").ShouldContain("indicator-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateSelectWithIndicator(
            defaultValue: "apple",
            indicatorStyleValue: _ => "background: green",
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("style").ShouldContain("background: green");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaHiddenTrue()
    {
        var cut = Render(CreateSelectWithIndicator(
            defaultValue: "apple",
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.GetAttribute("aria-hidden").ShouldBe("true");

        return Task.CompletedTask;
    }

    // Default children tests

    [Fact]
    public Task RendersDefaultCheckmarkWhenNoChildContent()
    {
        var cut = Render(CreateSelectWithIndicator(
            defaultValue: "apple",
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.TextContent.ShouldContain("✔️");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersCustomChildContentWhenProvided()
    {
        var cut = Render(CreateSelectWithIndicator(
            defaultValue: "apple",
            indicatorChildContent: (RenderFragment)(b => b.AddContent(0, "SELECTED")),
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.TextContent.ShouldContain("SELECTED");
        indicator.TextContent.ShouldNotContain("✔️");

        return Task.CompletedTask;
    }

    // Visibility tests

    [Fact]
    public Task DoesNotRenderWhenNotSelected()
    {
        var cut = Render(CreateSelectWithIndicator(
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicators = cut.FindAll("[data-testid='indicator']");
        indicators.Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWhenSelected()
    {
        var cut = Render(CreateSelectWithIndicator(
            defaultValue: "apple",
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task NonKeepMountedIndicatorRemainsMountedDuringExitTransitionWhenSelectionChanges()
    {
        var cut = Render(CreateSelectWithTwoIndicators(defaultValue: "apple"));

        cut.FindAll("[data-testid='apple-indicator']").Count.ShouldBe(1);
        cut.FindAll("[data-testid='banana-indicator']").Count.ShouldBe(0);

        var bananaItem = cut.Find("[data-testid='banana-item']");
        await bananaItem.TriggerEventAsync("onmousemove", new MouseEventArgs());
        await bananaItem.TriggerEventAsync("onpointerdown", new PointerEventArgs { PointerType = "mouse" });
        bananaItem.Click();

        // Mirror React's `useTransitionStatus(selected)`: the deselected
        // indicator stays mounted while the exit transition is in flight so
        // consumers can attach `[data-ending]` exit styles. Only after the
        // unmount delay completes does it leave the DOM.
        cut.FindAll("[data-testid='apple-indicator']").Count.ShouldBe(1);
        cut.FindAll("[data-testid='banana-indicator']").Count.ShouldBe(1);
    }

    // KeepMounted tests

    [Fact]
    public Task KeepsIndicatorMountedWhenNotSelected()
    {
        var cut = Render(CreateSelectWithIndicator(
            keepMounted: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task KeepsIndicatorMountedWhenSelected()
    {
        var cut = Render(CreateSelectWithIndicator(
            defaultValue: "apple",
            keepMounted: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    // Data attribute tests (transition style hooks)

    [Fact]
    public Task DoesNotHaveStartingStyleByDefault()
    {
        var cut = Render(CreateSelectWithIndicator(
            defaultValue: "apple",
            keepMounted: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-starting-style").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveEndingStyleByDefault()
    {
        var cut = Render(CreateSelectWithIndicator(
            defaultValue: "apple",
            keepMounted: true,
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var indicator = cut.Find("[data-testid='indicator']");
        indicator.HasAttribute("data-ending-style").ShouldBeFalse();

        return Task.CompletedTask;
    }

    // State callback tests

    [Fact]
    public Task ClassValueReceivesCorrectState()
    {
        SelectItemIndicatorState? capturedState = null;

        var cut = Render(CreateSelectWithIndicator(
            defaultValue: "apple",
            indicatorClassValue: state =>
            {
                capturedState = state;
                return "indicator-class";
            },
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Value.Selected.ShouldBeTrue();
        capturedState.Value.TransitionStatus.ShouldBe(TransitionStatus.Undefined);

        return Task.CompletedTask;
    }

    [Fact]
    public Task StyleValueReceivesCorrectState()
    {
        SelectItemIndicatorState? capturedState = null;

        var cut = Render(CreateSelectWithIndicator(
            defaultValue: "apple",
            indicatorStyleValue: state =>
            {
                capturedState = state;
                return "color: blue";
            },
            indicatorAdditionalAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Value.Selected.ShouldBeTrue();

        return Task.CompletedTask;
    }
}
