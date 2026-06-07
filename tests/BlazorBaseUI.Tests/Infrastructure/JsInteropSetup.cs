using Bunit;

namespace BlazorBaseUI.Tests.Infrastructure;

public static class JsInteropSetup
{
    private const string AvatarImageModule = "./_content/BlazorBaseUI/blazor-baseui-avatar-image.js";

    public static void SetupLoadedImage(BunitJSInterop jsInterop)
    {
        jsInterop.SetupModule(AvatarImageModule)
            .Setup<string>("loadImage", _ => true)
            .SetResult("loaded");
    }

    public static void SetupErrorImage(BunitJSInterop jsInterop)
    {
        jsInterop.SetupModule(AvatarImageModule)
            .Setup<string>("loadImage", _ => true)
            .SetResult("error");
    }

    public static void SetupIdleImage(BunitJSInterop jsInterop)
    {
        jsInterop.SetupModule(AvatarImageModule)
            .Setup<string>("loadImage", _ => true)
            .SetResult("idle");
    }

    private const string CollapsiblePanelModule = "./_content/BlazorBaseUI/blazor-baseui-collapsible.js";

    public static void SetupCollapsiblePanel(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(CollapsiblePanelModule);
        module.SetupVoid("initialize", _ => true);
        module.SetupVoid("open", _ => true);
        module.SetupVoid("close", _ => true);
        module.SetupVoid("updateDimensions", _ => true);
        module.SetupVoid("dispose", _ => true);
    }

    private const string MenuModule = "./_content/BlazorBaseUI/blazor-baseui-menu.js";

    public static BunitJSModuleInterop SetupMenuModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(MenuModule);
        module.SetupVoid("initializeRoot", _ => true).SetVoidResult();
        module.SetupVoid("disposeRoot", _ => true).SetVoidResult();
        module.SetupVoid("setRootOpen", _ => true).SetVoidResult();
        module.SetupVoid("updateRoot", _ => true).SetVoidResult();
        module.SetupVoid("closeMenubarSiblingRoots", _ => true).SetVoidResult();
        module.SetupVoid("setTriggerElement", _ => true).SetVoidResult();
        module.SetupVoid("setPopupElement", _ => true).SetVoidResult();
        module.SetupVoid("setActiveIndex", _ => true).SetVoidResult();
        module.SetupVoid("initializeHoverInteraction", _ => true).SetVoidResult();
        module.SetupVoid("disposeHoverInteraction", _ => true).SetVoidResult();
        module.SetupVoid("initializeMenubarTrigger", _ => true).SetVoidResult();
        module.SetupVoid("disposeMenubarTrigger", _ => true).SetVoidResult();
        module.SetupVoid("updateHoverInteractionFloatingElement", _ => true).SetVoidResult();
        module.SetupVoid("setHoverInteractionOpen", _ => true).SetVoidResult();
        module.SetupVoid("setInternalBackdrop", _ => true).SetVoidResult();
        module.Setup<string?>("initializePositioner", _ => true).SetResult("positioner-id");
        module.SetupVoid("updatePosition", _ => true).SetVoidResult();
        module.SetupVoid("disposePositioner", _ => true).SetVoidResult();
        module.SetupVoid("initializeViewport", _ => true).SetVoidResult();
        module.SetupVoid("disposeViewport", _ => true).SetVoidResult();
        module.SetupVoid("initializeAutoResize", _ => true).SetVoidResult();
        module.SetupVoid("disposeAutoResize", _ => true).SetVoidResult();
        module.SetupVoid("onViewportTriggerChange", _ => true).SetVoidResult();
        return module;
    }

    private const string MenuBarModule = "./_content/BlazorBaseUI/blazor-baseui-menubar.js";

    public static void SetupMenuBarModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(MenuBarModule);
        module.SetupVoid("initMenuBar", _ => true);
        module.SetupVoid("updateMenuBar", _ => true);
        module.SetupVoid("updateScrollLock", _ => true);
        module.SetupVoid("registerItem", _ => true);
        module.SetupVoid("unregisterItem", _ => true);
        module.SetupVoid("disposeMenuBar", _ => true);
    }

    private const string AccordionTriggerModule = "./_content/BlazorBaseUI/blazor-baseui-accordion-trigger.js";

    public static void SetupAccordionTrigger(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(AccordionTriggerModule);
        module.SetupVoid("initialize", _ => true);
        module.SetupVoid("updateConfig", _ => true);
        module.SetupVoid("dispose", _ => true);
    }

    public static void SetupAccordionModules(BunitJSInterop jsInterop)
    {
        SetupAccordionTrigger(jsInterop);
        SetupCollapsiblePanel(jsInterop);
    }

    private const string SliderModule = "./_content/BlazorBaseUI/blazor-baseui-slider.js";
    private const string SliderMinModule = "./_content/BlazorBaseUI/blazor-baseui-slider.min.js";

    public static void SetupSliderModule(BunitJSInterop jsInterop)
    {
        SetupSliderModulePath(SliderModule);
        SetupSliderModulePath(SliderMinModule);

        void SetupSliderModulePath(string path)
        {
            var module = jsInterop.SetupModule(path);
            module.SetupVoid("initialize", _ => true).SetVoidResult();
            module.SetupVoid("dispose", _ => true).SetVoidResult();
            module.SetupVoid("startDrag", _ => true).SetVoidResult();
            module.SetupVoid("stopDrag", _ => true).SetVoidResult();
            module.SetupVoid("setPointerCapture", _ => true).SetVoidResult();
            module.SetupVoid("focusThumbInput", _ => true).SetVoidResult();
            module.SetupVoid("registerPointerGuard", _ => true).SetVoidResult();
            module.SetupVoid("unregisterPointerGuard", _ => true).SetVoidResult();
            module.SetupVoid("blurActiveElement", _ => true).SetVoidResult();
            module.Setup<object?>("getThumbRect", _ => true).SetResult(null);
            module.SetupVoid("syncInsetPositions", _ => true).SetVoidResult();
            module.SetupVoid("observeInsetResize", _ => true).SetVoidResult();
            module.SetupVoid("unobserveInsetResize", _ => true).SetVoidResult();
            module.SetupVoid("registerThumbInput", _ => true).SetVoidResult();
            module.SetupVoid("unregisterThumbInput", _ => true).SetVoidResult();
        }
    }

    private const string SwitchModule = "./_content/BlazorBaseUI/blazor-baseui-switch.js";
    private const string SwitchMinModule = "./_content/BlazorBaseUI/blazor-baseui-switch.min.js";

    public static void SetupSwitchModule(BunitJSInterop jsInterop)
    {
        SetupSwitchModulePath(SwitchModule);
        SetupSwitchModulePath(SwitchMinModule);

        void SetupSwitchModulePath(string path)
        {
            var module = jsInterop.SetupModule(path);
            module.SetupVoid("initialize", _ => true).SetVoidResult();
            module.SetupVoid("dispose", _ => true).SetVoidResult();
            module.SetupVoid("updateState", _ => true).SetVoidResult();
            module.SetupVoid("setInputChecked", _ => true).SetVoidResult();
            module.SetupVoid("focus", _ => true).SetVoidResult();
        }
    }

    private const string CheckboxModule = "./_content/BlazorBaseUI/blazor-baseui-checkbox.js";
    private const string CheckboxMinModule = "./_content/BlazorBaseUI/blazor-baseui-checkbox.min.js";
    private const string AnimationsMinModule = "./_content/BlazorBaseUI/blazor-baseui-animations.min.js";

    public static void SetupCheckboxModule(BunitJSInterop jsInterop)
    {
        SetupCheckboxModulePath(CheckboxModule);
        SetupCheckboxModulePath(CheckboxMinModule);

        var animationsModule = jsInterop.SetupModule(AnimationsMinModule);
        animationsModule.SetupVoid("applyStartingStyle", _ => true).SetVoidResult();
        animationsModule.SetupVoid("waitForExitTransition", _ => true).SetVoidResult();

        void SetupCheckboxModulePath(string path)
        {
            var module = jsInterop.SetupModule(path);
            module.SetupVoid("initialize", _ => true).SetVoidResult();
            module.SetupVoid("dispose", _ => true).SetVoidResult();
            module.SetupVoid("updateState", _ => true).SetVoidResult();
            module.SetupVoid("setInputChecked", _ => true).SetVoidResult();
            module.SetupVoid("resetState", _ => true).SetVoidResult();
            module.SetupVoid("focus", _ => true).SetVoidResult();
        }
    }

    private const string PopoverModule = "./_content/BlazorBaseUI/blazor-baseui-popover.js";
    private const string PopoverMinModule = "./_content/BlazorBaseUI/blazor-baseui-popover.min.js";

    public static BunitJSModuleInterop SetupPopoverModule(BunitJSInterop jsInterop)
    {
        SetupPopoverModulePath(PopoverModule);
        var minModule = SetupPopoverModulePath(PopoverMinModule);
        return minModule;

        BunitJSModuleInterop SetupPopoverModulePath(string path)
        {
            var module = jsInterop.SetupModule(path);
            module.SetupVoid("initializeRoot", _ => true).SetVoidResult();
            module.SetupVoid("disposeRoot", _ => true).SetVoidResult();
            module.SetupVoid("hydrateRootOpen", _ => true).SetVoidResult();
            module.SetupVoid("setRootOpen", _ => true).SetVoidResult();
            module.SetupVoid("syncTriggerOpenAttributes", _ => true).SetVoidResult();
            module.SetupVoid("setTriggerElement", _ => true).SetVoidResult();
            module.SetupVoid("registerTriggerElement", _ => true).SetVoidResult();
            module.SetupVoid("unregisterTriggerElement", _ => true).SetVoidResult();
            module.SetupVoid("setPositionerElement", _ => true).SetVoidResult();
            module.SetupVoid("setBackdropElement", _ => true).SetVoidResult();
            module.SetupVoid("setPopupElement", _ => true).SetVoidResult();
            module.SetupVoid("initializeHoverInteraction", _ => true).SetVoidResult();
            module.SetupVoid("disposeHoverInteraction", _ => true).SetVoidResult();
            module.SetupVoid("updateHoverInteractionFloatingElement", _ => true).SetVoidResult();
            module.SetupVoid("setHoverInteractionOpen", _ => true).SetVoidResult();
            module.SetupVoid("updateScrollLock", _ => true).SetVoidResult();
            module.SetupVoid("setInternalBackdrop", _ => true).SetVoidResult();
            module.Setup<string?>("initializePositioner", _ => true).SetResult("positioner-id");
            module.SetupVoid("updatePosition", _ => true).SetVoidResult();
            module.SetupVoid("disposePositioner", _ => true).SetVoidResult();
            module.SetupVoid("initializePopup", _ => true).SetVoidResult();
            module.SetupVoid("setInitialFocusElement", _ => true).SetVoidResult();
            module.SetupVoid("setFinalFocusElement", _ => true).SetVoidResult();
            module.SetupVoid("disposePopup", _ => true).SetVoidResult();
            module.SetupVoid("focusElement", _ => true).SetVoidResult();
            module.Setup<string>("handleTriggerPreGuardFocus", _ => true).SetResult("close-previous");
            module.Setup<string>("handleTriggerPostGuardFocus", _ => true).SetResult("handled");
            module.SetupVoid("initializeViewport", _ => true).SetVoidResult();
            module.SetupVoid("disposeViewport", _ => true).SetVoidResult();
            module.SetupVoid("initializeAutoResize", _ => true).SetVoidResult();
            module.SetupVoid("disposeAutoResize", _ => true).SetVoidResult();
            module.SetupVoid("onViewportTriggerChange", _ => true).SetVoidResult();
            return module;
        }
    }

    private const string TooltipModule = "./_content/BlazorBaseUI/blazor-baseui-tooltip.js";
    private const string TooltipMinModule = "./_content/BlazorBaseUI/blazor-baseui-tooltip.min.js";

    public static void SetupTooltipModule(BunitJSInterop jsInterop)
    {
        SetupTooltipModulePath(TooltipModule);
        SetupTooltipModulePath(TooltipMinModule);

        void SetupTooltipModulePath(string path)
        {
            var module = jsInterop.SetupModule(path);
            module.SetupVoid("initializeRoot", _ => true).SetVoidResult();
            module.SetupVoid("disposeRoot", _ => true).SetVoidResult();
            module.SetupVoid("setRootOpen", _ => true).SetVoidResult();
            module.SetupVoid("syncTriggerOpenAttributes", _ => true).SetVoidResult();
            module.SetupVoid("setTriggerElement", _ => true).SetVoidResult();
            module.SetupVoid("setPopupElement", _ => true).SetVoidResult();
            module.SetupVoid("initializeHoverInteraction", _ => true).SetVoidResult();
            module.SetupVoid("disposeHoverInteraction", _ => true).SetVoidResult();
            module.SetupVoid("cancelPendingHoverOpen", _ => true).SetVoidResult();
            module.SetupVoid("updateHoverInteractionDelays", _ => true).SetVoidResult();
            module.Setup<string?>("initializePositioner", _ => true).SetResult("positioner-id");
            module.SetupVoid("updatePosition", _ => true).SetVoidResult();
            module.SetupVoid("disposePositioner", _ => true).SetVoidResult();
            module.SetupVoid("setPositionerId", _ => true).SetVoidResult();
        }
    }

    private const string ToastModule = "./_content/BlazorBaseUI/blazor-baseui-toast.js";
    private const string ToastMinModule = "./_content/BlazorBaseUI/blazor-baseui-toast.min.js";

    public static void SetupToastModule(BunitJSInterop jsInterop)
    {
        SetupToastModulePath(ToastModule);
        SetupToastModulePath(ToastMinModule);

        void SetupToastModulePath(string path)
        {
            var module = jsInterop.SetupModule(path);
            module.SetupVoid("initializeViewport", _ => true).SetVoidResult();
            module.SetupVoid("updateViewport", _ => true).SetVoidResult();
            module.SetupVoid("disposeViewport", _ => true).SetVoidResult();
            module.SetupVoid("handleFocusAfterClose", _ => true).SetVoidResult();
            module.SetupVoid("handleFocusGuard", _ => true).SetVoidResult();
            module.SetupVoid("initializeRoot", _ => true).SetVoidResult();
            module.SetupVoid("updateRoot", _ => true).SetVoidResult();
            module.SetupVoid("disposeRoot", _ => true).SetVoidResult();
            module.SetupVoid("initializeContent", _ => true).SetVoidResult();
            module.SetupVoid("disposeContent", _ => true).SetVoidResult();
            module.Setup<string?>("initializePositioner", _ => true).SetResult("positioner-id");
            module.SetupVoid("updatePosition", _ => true).SetVoidResult();
            module.SetupVoid("disposePositioner", _ => true).SetVoidResult();
        }
    }

    private const string FieldModule = "./_content/BlazorBaseUI/blazor-baseui-field.js";

    public static void SetupFieldModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(FieldModule);
        module.Setup<object?>("getValidityState", _ => true).SetResult(null);
        module.Setup<string>("getValidationMessage", _ => true).SetResult("");
        module.SetupVoid("setCustomValidity", _ => true);
        module.Setup<bool>("checkValidity", _ => true).SetResult(true);
        module.Setup<bool>("reportValidity", _ => true).SetResult(true);
        module.SetupVoid("focusElement", _ => true);
        module.Setup<bool>("isActiveElement", _ => true).SetResult(false);
        module.Setup<object?>("getValue", _ => true).SetResult(null);
        module.SetupVoid("setValue", _ => true);
        module.Setup<string?>("observeValidity", _ => true).SetResult(null);
        module.SetupVoid("disposeObserver", _ => true);
        module.SetupVoid("dispose", _ => true);
    }

    private const string LabelModule = "./_content/BlazorBaseUI/blazor-baseui-label.js";

    public static void SetupLabelModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(LabelModule);
        module.SetupVoid("addLabelMouseDownListener", _ => true);
        module.SetupVoid("removeLabelMouseDownListener", _ => true);
        module.SetupVoid("focusControlById", _ => true);
        module.SetupVoid("focusSliderControl", _ => true);
    }

    private const string DialogModule = "./_content/BlazorBaseUI/blazor-baseui-dialog.js";

    public static void SetupDialogModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(DialogModule);
        module.SetupVoid("initializeRoot", _ => true).SetVoidResult();
        module.SetupVoid("disposeRoot", _ => true).SetVoidResult();
        module.SetupVoid("setRootOpen", _ => true).SetVoidResult();
        module.SetupVoid("setTriggerElement", _ => true).SetVoidResult();
        module.SetupVoid("setPopupElement", _ => true).SetVoidResult();
        module.SetupVoid("initializePopup", _ => true).SetVoidResult();
        module.SetupVoid("setInitialFocusElement", _ => true).SetVoidResult();
        module.SetupVoid("setFinalFocusElement", _ => true).SetVoidResult();
        module.SetupVoid("setBackdropElement", _ => true).SetVoidResult();
        module.SetupVoid("disposePopup", _ => true).SetVoidResult();
    }

    private const string DrawerModule = "./_content/BlazorBaseUI/blazor-baseui-drawer.js";
    private const string DrawerMinModule = "./_content/BlazorBaseUI/blazor-baseui-drawer.min.js";

    public static void SetupDrawerModule(BunitJSInterop jsInterop)
    {
        SetupDrawerModulePath(DrawerModule);
        SetupDrawerModulePath(DrawerMinModule);

        void SetupDrawerModulePath(string path)
        {
            var module = jsInterop.SetupModule(path);
            module.SetupVoid("initializeRootReporter", _ => true).SetVoidResult();
            module.SetupVoid("disposeRootReporter", _ => true).SetVoidResult();
            module.SetupVoid("setRootOpen", _ => true).SetVoidResult();
            module.SetupVoid("initializePopup", _ => true).SetVoidResult();
            module.SetupVoid("updatePopup", _ => true).SetVoidResult();
            module.SetupVoid("disposePopup", _ => true).SetVoidResult();
            module.SetupVoid("setBackdropElement", _ => true).SetVoidResult();
            module.SetupVoid("initializeViewport", _ => true).SetVoidResult();
            module.SetupVoid("updateViewport", _ => true).SetVoidResult();
            module.SetupVoid("disposeViewport", _ => true).SetVoidResult();
            module.SetupVoid("initializeSwipeArea", _ => true).SetVoidResult();
            module.SetupVoid("updateSwipeArea", _ => true).SetVoidResult();
            module.SetupVoid("disposeSwipeArea", _ => true).SetVoidResult();
            module.SetupVoid("initializeIndent", _ => true).SetVoidResult();
            module.SetupVoid("disposeIndent", _ => true).SetVoidResult();
        }
    }

    private const string ButtonModule = "./_content/BlazorBaseUI/blazor-baseui-button.js";

    public static void SetupButtonModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(ButtonModule);
        module.SetupVoid("sync", _ => true);
    }

    private const string ToolbarModule = "./_content/BlazorBaseUI/blazor-baseui-toolbar.js";

    public static void SetupToolbarModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(ToolbarModule);
        module.SetupVoid("initToolbar", _ => true);
        module.SetupVoid("updateToolbar", _ => true);
        module.SetupVoid("registerItem", _ => true);
        module.SetupVoid("unregisterItem", _ => true);
        module.SetupVoid("disposeToolbar", _ => true);
    }

    private const string RadioModule = "./_content/BlazorBaseUI/blazor-baseui-radio.js";

    public static void SetupRadioModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(RadioModule);
        module.SetupVoid("initialize", _ => true);
        module.SetupVoid("dispose", _ => true);
        module.SetupVoid("updateState", _ => true);
        module.SetupVoid("focus", _ => true);
        module.SetupVoid("registerRadio", _ => true);
        module.SetupVoid("unregisterRadio", _ => true);
        module.SetupVoid("navigateToPrevious", _ => true);
        module.SetupVoid("navigateToNext", _ => true);
        module.SetupVoid("initializeGroup", _ => true);
        module.SetupVoid("disposeGroup", _ => true);
        module.Setup<bool>("isBlurWithinGroup", _ => true).SetResult(false);
    }

    private const string TabsModule = "./_content/BlazorBaseUI/blazor-baseui-tabs.js";
    private const string TabsMinModule = "./_content/BlazorBaseUI/blazor-baseui-tabs.min.js";

    public static BunitJSModuleInterop SetupTabsModule(BunitJSInterop jsInterop)
    {
        SetupTabsModulePath(TabsModule);
        return SetupTabsModulePath(TabsMinModule);

        BunitJSModuleInterop SetupTabsModulePath(string path)
        {
            var module = jsInterop.SetupModule(path);
            module.SetupVoid("initializeList", _ => true).SetVoidResult();
            module.SetupVoid("updateList", _ => true).SetVoidResult();
            module.SetupVoid("disposeList", _ => true).SetVoidResult();
            module.SetupVoid("registerTab", _ => true).SetVoidResult();
            module.SetupVoid("unregisterTab", _ => true).SetVoidResult();
            module.SetupVoid("navigateToPrevious", _ => true).SetVoidResult();
            module.SetupVoid("navigateToNext", _ => true).SetVoidResult();
            module.SetupVoid("navigateToFirst", _ => true).SetVoidResult();
            module.SetupVoid("navigateToLast", _ => true).SetVoidResult();
            // getActiveElement returns IJSObjectReference? - handled by loose mode (returns null)
            module.SetupVoid("initializeTab", _ => true).SetVoidResult();
            module.SetupVoid("dispose", _ => true).SetVoidResult();
            module.SetupVoid("focus", _ => true).SetVoidResult();
            module.SetupVoid("startPanelTransition", _ => true).SetVoidResult();
            module.SetupVoid("disposePanel", _ => true).SetVoidResult();
            module.Setup<object?>("getTabPosition", _ => true).SetResult(null);
            module.Setup<object?>("getIndicatorPosition", _ => true).SetResult(null);
            module.SetupVoid("observeResize", _ => true).SetVoidResult();
            module.SetupVoid("unobserveResize", _ => true).SetVoidResult();
            module.Setup<object?>("getFirstEnabledTab", _ => true).SetResult(null);
            return module;
        }
    }

    private const string NumberFieldModule = "./_content/BlazorBaseUI/blazor-baseui-number-field.js";

    public static void SetupNumberFieldModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(NumberFieldModule);
        module.SetupVoid("registerWheelListener", _ => true).SetVoidResult();
        module.SetupVoid("focusInput", _ => true).SetVoidResult();
        module.SetupVoid("startAutoChange", _ => true).SetVoidResult();
        module.SetupVoid("stopAutoChange", _ => true).SetVoidResult();
        module.SetupVoid("dispose", _ => true).SetVoidResult();
        module.SetupVoid("initializeScrubArea", _ => true).SetVoidResult();
        module.Setup<object?>("startScrub", _ => true).SetResult(null);
        module.SetupVoid("disposeScrubArea", _ => true).SetVoidResult();
    }

    private const string NavigationMenuModule = "./_content/BlazorBaseUI/blazor-baseui-navigation-menu.js";

    public static BunitJSModuleInterop SetupNavigationMenuModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(NavigationMenuModule);
        module.SetupVoid("initializeRoot", _ => true).SetVoidResult();
        module.SetupVoid("disposeRoot", _ => true).SetVoidResult();
        module.SetupVoid("setRootValue", _ => true).SetVoidResult();
        module.SetupVoid("setTriggerElement", _ => true).SetVoidResult();
        module.SetupVoid("disposeTriggerElement", _ => true).SetVoidResult();
        module.SetupVoid("setPopupElement", _ => true).SetVoidResult();
        module.SetupVoid("setViewportElement", _ => true).SetVoidResult();
        module.Setup<string?>("initializePositioner", _ => true).SetResult("positioner-id");
        module.SetupVoid("updatePosition", _ => true).SetVoidResult();
        module.SetupVoid("disposePositioner", _ => true).SetVoidResult();
        return module;
    }

    private const string ToggleModule = "./_content/BlazorBaseUI/blazor-baseui-toggle.js";

    public static void SetupToggleModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(ToggleModule);
        module.SetupVoid("initialize", _ => true).SetVoidResult();
        module.SetupVoid("updateState", _ => true).SetVoidResult();
        module.SetupVoid("dispose", _ => true).SetVoidResult();
        module.SetupVoid("initializeGroup", _ => true).SetVoidResult();
        module.SetupVoid("updateGroup", _ => true).SetVoidResult();
        module.SetupVoid("disposeGroup", _ => true).SetVoidResult();
        module.SetupVoid("registerToggle", _ => true).SetVoidResult();
        module.SetupVoid("unregisterToggle", _ => true).SetVoidResult();
        module.SetupVoid("navigateToPrevious", _ => true).SetVoidResult();
        module.SetupVoid("navigateToNext", _ => true).SetVoidResult();
        module.SetupVoid("navigateToFirst", _ => true).SetVoidResult();
        module.SetupVoid("navigateToLast", _ => true).SetVoidResult();
        module.SetupVoid("syncGroupTabIndexes", _ => true).SetVoidResult();
        module.SetupVoid("initializeGroupItem", _ => true).SetVoidResult();
        module.SetupVoid("updateGroupItemOrientation", _ => true).SetVoidResult();
        module.SetupVoid("disposeGroupItem", _ => true).SetVoidResult();
    }

    private const string PreviewCardModule = "./_content/BlazorBaseUI/blazor-baseui-preview-card.js";

    public static void SetupPreviewCardModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(PreviewCardModule);
        module.SetupVoid("initializeRoot", _ => true).SetVoidResult();
        module.SetupVoid("disposeRoot", _ => true).SetVoidResult();
        module.SetupVoid("setRootOpen", _ => true).SetVoidResult();
        module.SetupVoid("setTriggerElement", _ => true).SetVoidResult();
        module.SetupVoid("setPopupElement", _ => true).SetVoidResult();
        module.SetupVoid("initializeHoverInteraction", _ => true).SetVoidResult();
        module.SetupVoid("disposeHoverInteraction", _ => true).SetVoidResult();
        module.SetupVoid("updateHoverInteractionFloatingElement", _ => true).SetVoidResult();
        module.SetupVoid("setHoverInteractionOpen", _ => true).SetVoidResult();
        module.Setup<string?>("initializePositioner", _ => true).SetResult("positioner-id");
        module.SetupVoid("updatePosition", _ => true).SetVoidResult();
        module.SetupVoid("disposePositioner", _ => true).SetVoidResult();
        module.SetupVoid("initializePopup", _ => true).SetVoidResult();
        module.SetupVoid("disposePopup", _ => true).SetVoidResult();
    }

    private const string ContextMenuModule = "./_content/BlazorBaseUI/blazor-baseui-context-menu.js";

    public static void SetupContextMenuModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(ContextMenuModule);
        module.SetupVoid("initializeContextMenu", _ => true).SetVoidResult();
        module.SetupVoid("setBackdropElement", _ => true).SetVoidResult();
        module.SetupVoid("setPositionerElement", _ => true).SetVoidResult();
        module.SetupVoid("setContextMenuDisabled", _ => true).SetVoidResult();
        module.SetupVoid("disposeContextMenu", _ => true).SetVoidResult();
    }

    private const string FloatingModule = "./_content/BlazorBaseUI/blazor-baseui-floating.js";
    private const string FloatingMinModule = "./_content/BlazorBaseUI/blazor-baseui-floating.min.js";

    public static void SetupFocusGuardSafari(BunitJSInterop jsInterop)
    {
        SetupFocusGuardSafariPath(FloatingModule);
        SetupFocusGuardSafariPath(FloatingMinModule);

        void SetupFocusGuardSafariPath(string path)
        {
            var module = jsInterop.SetupModule(path);
            module.Setup<bool>("isSafari").SetResult(true);
        }
    }

    public static void SetupFocusGuardNonSafari(BunitJSInterop jsInterop)
    {
        SetupFocusGuardNonSafariPath(FloatingModule);
        SetupFocusGuardNonSafariPath(FloatingMinModule);

        void SetupFocusGuardNonSafariPath(string path)
        {
            var module = jsInterop.SetupModule(path);
            module.Setup<bool>("isSafari").SetResult(false);
        }
    }

    public static void SetupFloatingTreeModule(BunitJSInterop jsInterop)
    {
        SetupFloatingTreeModulePath(FloatingModule);
        SetupFloatingTreeModulePath(FloatingMinModule);

        void SetupFloatingTreeModulePath(string path)
        {
            var module = jsInterop.SetupModule(path);
            module.SetupVoid("getFloatingTree", _ => true).SetVoidResult();
            module.SetupVoid("disposeFloatingTree", _ => true).SetVoidResult();
        }
    }

    public static void SetupFloatingFocusManagerModule(BunitJSInterop jsInterop, string managerId = "fm-1")
    {
        SetupFloatingFocusManagerModulePath(FloatingModule, managerId);
        SetupFloatingFocusManagerModulePath(FloatingMinModule, managerId);

        void SetupFloatingFocusManagerModulePath(string path, string id)
        {
            var module = jsInterop.SetupModule(path);
            module.Setup<bool>("isSafari").SetResult(false);
            module.Setup<string>("createFloatingFocusManager", _ => true).SetResult(id);
            module.SetupVoid("disposeFloatingFocusManager", _ => true).SetVoidResult();
            module.SetupVoid("updateFloatingFocusManager", _ => true).SetVoidResult();
            module.Setup<string>("initializeFloatingPortal", _ => true).SetResult("portal-id");
            module.SetupVoid("disposeFloatingPortal", _ => true).SetVoidResult();
        }
    }

    public static void SetupFloatingDelayGroupModule(BunitJSInterop jsInterop, string groupId = "dg-1")
    {
        SetupFloatingDelayGroupModulePath(FloatingModule, groupId);
        SetupFloatingDelayGroupModulePath(FloatingMinModule, groupId);

        void SetupFloatingDelayGroupModulePath(string path, string id)
        {
            var module = jsInterop.SetupModule(path);
            module.Setup<System.Text.Json.JsonElement>("createDelayGroup", _ => true)
                .SetResult(System.Text.Json.JsonSerializer.SerializeToElement(new { groupId = id }));
            module.SetupVoid("registerDelayGroupMember", _ => true).SetVoidResult();
            module.SetupVoid("unregisterDelayGroupMember", _ => true).SetVoidResult();
            module.SetupVoid("notifyDelayGroupMemberOpened", _ => true).SetVoidResult();
            module.SetupVoid("notifyDelayGroupMemberClosed", _ => true).SetVoidResult();
            module.SetupVoid("updateDelayGroupOptions", _ => true).SetVoidResult();
            module.SetupVoid("disposeDelayGroup", _ => true).SetVoidResult();
        }
    }

    private const string SelectModule = "./_content/BlazorBaseUI/blazor-baseui-select.js";

    public static void SetupSelectModule(BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule(SelectModule);
        module.SetupVoid("initializeRoot", _ => true).SetVoidResult();
        module.SetupVoid("disposeRoot", _ => true).SetVoidResult();
        module.SetupVoid("setRootOpen", _ => true).SetVoidResult();
        module.SetupVoid("setTriggerElement", _ => true).SetVoidResult();
        module.SetupVoid("setPopupElement", _ => true).SetVoidResult();
        module.SetupVoid("setListElement", _ => true).SetVoidResult();
        module.SetupVoid("setActiveIndex", _ => true).SetVoidResult();
        module.SetupVoid("clearHighlights", _ => true).SetVoidResult();
        module.SetupVoid("focusTrigger", _ => true).SetVoidResult();
        module.SetupVoid("startContinuousScroll", _ => true).SetVoidResult();
        module.SetupVoid("stopContinuousScroll", _ => true).SetVoidResult();
        module.Setup<string?>("initializePositioner", _ => true).SetResult("positioner-id");
        module.SetupVoid("updatePosition", _ => true).SetVoidResult();
        module.SetupVoid("disposePositioner", _ => true).SetVoidResult();
        module.SetupVoid("registerPositioner", _ => true).SetVoidResult();
        module.SetupVoid("unregisterPositioner", _ => true).SetVoidResult();
        module.SetupVoid("initializeTrigger", _ => true).SetVoidResult();
        module.SetupVoid("disposeTrigger", _ => true).SetVoidResult();
        module.Setup<string>("applyScrollLock", _ => true).SetResult("scroll-lock-token");
        module.SetupVoid("releaseScrollLock", _ => true).SetVoidResult();
        module.SetupVoid("initializePopup", _ => true).SetVoidResult();
        module.SetupVoid("disposePopup", _ => true).SetVoidResult();
        module.SetupVoid("beginAlignItemWithTriggerPlacement", _ => true).SetVoidResult();
        module.SetupVoid("handlePopupScroll", _ => true).SetVoidResult();
        module.SetupVoid("clearPopupStyles", _ => true).SetVoidResult();
        module.SetupVoid("attachWindowResizeListener", _ => true).SetVoidResult();
        module.SetupVoid("detachWindowResizeListener", _ => true).SetVoidResult();
        module.SetupVoid("injectScrollbarDisableStyle", _ => true).SetVoidResult();
        module.SetupVoid("setSelectedItemTextElement", _ => true).SetVoidResult();
        module.SetupVoid("setValueElement", _ => true).SetVoidResult();
        module.SetupVoid("setHighlightItemOnHover", _ => true).SetVoidResult();
        module.SetupVoid("setDisabled", _ => true).SetVoidResult();
        module.SetupVoid("setDirection", _ => true).SetVoidResult();
        module.Setup<string?>("getElementText", _ => true).SetResult(null);

        var floating = jsInterop.SetupModule(FloatingModule);
        floating.SetupVoid("clearStyles", _ => true).SetVoidResult();
    }

    private const string ScrollAreaModule = "./_content/BlazorBaseUI/blazor-baseui-scroll-area.js";
    private const string ScrollAreaMinModule = "./_content/BlazorBaseUI/blazor-baseui-scroll-area.min.js";

    public static BunitJSModuleInterop SetupScrollAreaModule(BunitJSInterop jsInterop)
    {
        SetupScrollAreaModulePath(ScrollAreaModule);
        return SetupScrollAreaModulePath(ScrollAreaMinModule);

        BunitJSModuleInterop SetupScrollAreaModulePath(string path)
        {
            var module = jsInterop.SetupModule(path);
            module.SetupVoid("initializeRoot", _ => true).SetVoidResult();
            module.SetupVoid("updateRoot", _ => true).SetVoidResult();
            module.SetupVoid("disposeRoot", _ => true).SetVoidResult();
            module.SetupVoid("registerViewport", _ => true).SetVoidResult();
            module.SetupVoid("unregisterViewport", _ => true).SetVoidResult();
            module.SetupVoid("registerContent", _ => true).SetVoidResult();
            module.SetupVoid("unregisterContent", _ => true).SetVoidResult();
            module.SetupVoid("registerScrollbar", _ => true).SetVoidResult();
            module.SetupVoid("unregisterScrollbar", _ => true).SetVoidResult();
            module.SetupVoid("registerThumb", _ => true).SetVoidResult();
            module.SetupVoid("unregisterThumb", _ => true).SetVoidResult();
            module.SetupVoid("registerCorner", _ => true).SetVoidResult();
            module.SetupVoid("unregisterCorner", _ => true).SetVoidResult();
            return module;
        }
    }
}
