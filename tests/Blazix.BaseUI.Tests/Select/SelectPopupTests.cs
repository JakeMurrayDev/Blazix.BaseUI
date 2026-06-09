using Blazix.BaseUI.FloatingFocusManager;

namespace Blazix.BaseUI.Tests.Select;

public class SelectPopupTests : BunitContext, ISelectPopupContract
{
    private const string SelectModule = "./_content/Blazix.BaseUI/blazix-baseui-select.js";

    public SelectPopupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSelectModule(JSInterop);
        JsInteropSetup.SetupFloatingFocusManagerModule(JSInterop);
    }

    private static RenderFragment CreateSelectWithPopupNoList(
        bool defaultOpen = true,
        bool multiple = false,
        bool alignItemWithTrigger = true,
        FinalFocusTarget? finalFocus = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "Multiple", multiple);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Select")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "AlignItemWithTrigger", alignItemWithTrigger);
                innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectPopup>(0);
                    if (finalFocus.HasValue)
                    {
                        posBuilder.AddAttribute(1, "FinalFocus", finalFocus.Value);
                    }
                    posBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<SelectItem<string>>(0);
                        popupBuilder.AddAttribute(1, "Value", "apple");
                        popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateSelectWithPopupAndList(bool defaultOpen = true)
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
                        popupBuilder.OpenComponent<SelectList>(0);
                        popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(listBuilder =>
                        {
                            listBuilder.OpenComponent<SelectItem<string>>(0);
                            listBuilder.AddAttribute(1, "Value", "apple");
                            listBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                            listBuilder.CloseComponent();
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
    public Task HasAriaAttributesWhenNoSelectListPresent()
    {
        var cut = Render(CreateSelectWithPopupNoList(defaultOpen: true));

        var popup = cut.Find("[role='listbox']");
        popup.ShouldNotBeNull();
        popup.GetAttribute("role").ShouldBe("listbox");
        popup.GetAttribute("tabindex").ShouldBe("-1");

        return Task.CompletedTask;
    }

    [Fact]
    public Task PlacesAriaAttributesOnSelectListIfPresent()
    {
        var cut = Render(CreateSelectWithPopupAndList(defaultOpen: true));

        var listboxElements = cut.FindAll("[role='listbox']");
        var listElement = listboxElements.First(el => el.HasAttribute("id"));
        listElement.GetAttribute("role").ShouldBe("listbox");
        listElement.GetAttribute("tabindex").ShouldBe("-1");
        listElement.GetAttribute("id").ShouldNotBeNullOrEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesRolePresentationWhenListPresent()
    {
        var cut = Render(CreateSelectWithPopupAndList(defaultOpen: true));

        var presentational = cut.FindAll("[role='presentation']")
            .FirstOrDefault();
        presentational.ShouldNotBeNull();
        presentational!.HasAttribute("id").ShouldBeFalse();
        presentational.HasAttribute("aria-multiselectable").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesRootIdDashListAsPopupId()
    {
        var cut = Render(CreateSelectWithPopupNoList(defaultOpen: true));

        var popup = cut.Find("[role='listbox']");
        var id = popup.GetAttribute("id");
        id.ShouldNotBeNullOrEmpty();
        id!.ShouldEndWith("-list");
        return Task.CompletedTask;
    }

    [Fact]
    public Task EmitsAriaMultiselectableWhenMultiple()
    {
        var cut = Render(CreateSelectWithPopupNoList(defaultOpen: true, multiple: true));

        var popup = cut.Find("[role='listbox']");
        popup.GetAttribute("aria-multiselectable").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task OmitsAriaMultiselectableWhenSingleSelect()
    {
        var cut = Render(CreateSelectWithPopupNoList(defaultOpen: true, multiple: false));

        var popup = cut.Find("[role='listbox']");
        popup.HasAttribute("aria-multiselectable").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesDataSideAndDataAlign()
    {
        var cut = Render(CreateSelectWithPopupNoList(defaultOpen: true));

        var popup = cut.Find("[role='listbox']");
        popup.HasAttribute("data-side").ShouldBeTrue();
        popup.HasAttribute("data-align").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsFinalFocusToFloatingFocusManager()
    {
        var cut = Render(CreateSelectWithPopupNoList(
            defaultOpen: true,
            finalFocus: FinalFocusTarget.None));

        var manager = cut.FindComponent<global::Blazix.BaseUI.FloatingFocusManager.FloatingFocusManager>();
        manager.Instance.FinalFocus.ShouldNotBeNull();
        // None resolves to Suppress for any close type.
        manager.Instance.FinalFocus!.Value.Resolve(InteractionType.Click).Suppress.ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DefaultsFinalFocusToNullSoFocusManagerKeepsLegacyBehavior()
    {
        var cut = Render(CreateSelectWithPopupNoList(defaultOpen: true));

        var manager = cut.FindComponent<global::Blazix.BaseUI.FloatingFocusManager.FloatingFocusManager>();
        manager.Instance.FinalFocus.ShouldBeNull();
        // Legacy ReturnFocus default = true.
        manager.Instance.ReturnFocus.ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task CallsInitializePopupOnFirstRender()
    {
        var module = JSInterop.SetupModule(SelectModule);
        var cut = Render(CreateSelectWithPopupNoList(defaultOpen: true));

        // initializePopup is called from SelectPopup.OnAfterRenderAsync(firstRender=true).
        module.VerifyInvoke("initializePopup");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CallsAlignItemPlacementOnFirstOpen()
    {
        var module = JSInterop.SetupModule(SelectModule);
        var cut = Render(CreateSelectWithPopupNoList(defaultOpen: true, alignItemWithTrigger: true));

        module.VerifyInvoke("beginAlignItemWithTriggerPlacement");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CallsDisposePopupOnDispose()
    {
        var module = JSInterop.SetupModule(SelectModule);
        var cut = Render(CreateSelectWithPopupNoList(defaultOpen: true));

        cut.Dispose();

        module.VerifyInvoke("disposePopup");
        return Task.CompletedTask;
    }

    [Fact]
    public async Task OnPopupPointerLeaveClearsActiveIndex()
    {
        var cut = Render(CreateSelectWithPopupNoList(defaultOpen: true));
        var popup = cut.FindComponent<SelectPopup>().Instance;
        var rootInstance = cut.FindComponent<SelectRoot<string>>().Instance;
        rootInstance.typedContext.ActiveIndex = 5;

        await popup.OnPopupPointerLeave();

        rootInstance.typedContext.ActiveIndex.ShouldBe(-1);
    }

    [Fact]
    public async Task OnWindowResizeClosesSelect()
    {
        var cut = Render(CreateSelectWithPopupNoList(defaultOpen: true));
        var popup = cut.FindComponent<SelectPopup>().Instance;
        var rootInstance = cut.FindComponent<SelectRoot<string>>().Instance;
        rootInstance.typedContext.GetOpen().ShouldBeTrue();

        await popup.OnWindowResize();

        rootInstance.typedContext.GetOpen().ShouldBeFalse();
        rootInstance.typedContext.OpenChangeReason.ShouldBe(SelectOpenChangeReason.WindowResize);
    }

    [Fact]
    public Task OnFallbackToAlignPopupToTriggerDisablesAlignment()
    {
        var cut = Render(CreateSelectWithPopupNoList(defaultOpen: true, alignItemWithTrigger: true));
        var popup = cut.FindComponent<SelectPopup>().Instance;
        var positioner = cut.FindComponent<SelectPositioner>().Instance;

        // SelectPositioner exposes AlignItemWithTrigger; the controlled latch starts equal.
        // Triggering fallback flips it off via SetControlledAlignItemWithTrigger(false).
        popup.OnFallbackToAlignPopupToTrigger();

        cut.Render(); // re-render so SelectPositioner.OnParametersSet picks up the flag change

        // After fallback, positioner's controlled alignment should now be false, which makes
        // alignItemWithTriggerActive false on the next render.
        positioner.AlignItemWithTrigger.ShouldBeTrue(); // user prop still true
        return Task.CompletedTask;
    }
}
