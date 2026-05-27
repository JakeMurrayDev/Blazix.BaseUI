using System.Text;

namespace BlazorBaseUI.Playwright.Tests.Infrastructure;

public enum TestRenderMode
{
    Server,
    Wasm
}

public static class RenderModeExtensions
{
    public static string GetTestPath(this TestRenderMode mode, string basePath)
    {
        // Convert /tests/collapsible to /tests/collapsible/server or /tests/collapsible/wasm
        return mode switch
        {
            TestRenderMode.Server => $"{basePath.TrimEnd('/')}/server",
            TestRenderMode.Wasm => $"{basePath.TrimEnd('/')}/wasm",
            _ => $"{basePath.TrimEnd('/')}/server"
        };
    }
}

public sealed class TestPageUrlBuilder
{
    private readonly string baseAddress;
    private readonly string path;
    private readonly TestRenderMode renderMode;
    private readonly Dictionary<string, string> queryParams = new();

    public TestPageUrlBuilder(string baseAddress, string basePath, TestRenderMode renderMode)
    {
        this.baseAddress = baseAddress.TrimEnd('/');
        this.path = renderMode.GetTestPath(basePath);
        this.renderMode = renderMode;
    }

    public TestPageUrlBuilder WithKeepMounted(bool value)
    {
        queryParams["keepMounted"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithHiddenUntilFound(bool value)
    {
        queryParams["hiddenUntilFound"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithAnimated(bool value)
    {
        queryParams["animated"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithDisabled(bool value)
    {
        queryParams["disabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithDefaultOpen(bool value)
    {
        queryParams["defaultOpen"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithControlledExternal(bool value)
    {
        queryParams["controlledExternal"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithCustomPanelId(string? id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            queryParams["panelId"] = id;
        }
        return this;
    }

    public TestPageUrlBuilder WithImageSrc(string src)
    {
        queryParams["imageSrc"] = src;
        return this;
    }

    public TestPageUrlBuilder WithFallbackDelay(int? delayMs)
    {
        if (delayMs.HasValue)
        {
            queryParams["fallbackDelay"] = delayMs.Value.ToString();
        }
        return this;
    }

    public TestPageUrlBuilder WithForceError(bool value)
    {
        queryParams["forceError"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowFallback(bool value)
    {
        queryParams["showFallback"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithCustomClass(string className)
    {
        queryParams["customClass"] = className;
        return this;
    }

    public TestPageUrlBuilder WithCustomStyle(string style)
    {
        queryParams["customStyle"] = style;
        return this;
    }

    public TestPageUrlBuilder WithAs(string tag)
    {
        queryParams["as"] = tag;
        return this;
    }

    // Menu-specific parameters
    public TestPageUrlBuilder WithModal(bool value)
    {
        queryParams["modal"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithOrientation(string orientation)
    {
        queryParams["orientation"] = orientation;
        return this;
    }

    public TestPageUrlBuilder WithLoopFocus(bool value)
    {
        queryParams["loopFocus"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithOpenOnHover(bool value)
    {
        queryParams["openOnHover"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithOpenDelay(int delayMs)
    {
        queryParams["openDelay"] = delayMs.ToString();
        return this;
    }

    public TestPageUrlBuilder WithCloseDelay(int delayMs)
    {
        queryParams["closeDelay"] = delayMs.ToString();
        return this;
    }

    public TestPageUrlBuilder WithShowCheckbox(bool value)
    {
        queryParams["showCheckbox"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowRadioGroup(bool value)
    {
        queryParams["showRadioGroup"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowSubmenu(bool value)
    {
        queryParams["showSubmenu"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithCloseParentOnEsc(bool value)
    {
        queryParams["closeParentOnEsc"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithHighlightItemOnHover(bool value)
    {
        queryParams["highlightItemOnHover"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowTextNavItems(bool value)
    {
        queryParams["showTextNavItems"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowCountInTrigger(bool value)
    {
        queryParams["showCountInTrigger"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowNestedSubmenu(bool value)
    {
        queryParams["showNestedSubmenu"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithDirection(string direction)
    {
        queryParams["direction"] = direction;
        return this;
    }

    // Accordion-specific parameters
    public TestPageUrlBuilder WithMultiple(bool value)
    {
        queryParams["multiple"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithHorizontal(bool value)
    {
        queryParams["horizontal"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithRootDisabled(bool value)
    {
        queryParams["rootDisabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithItem1Disabled(bool value)
    {
        queryParams["item1Disabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithItem2Disabled(bool value)
    {
        queryParams["item2Disabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithItem3Disabled(bool value)
    {
        queryParams["item3Disabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithUseCustomValues(bool value)
    {
        queryParams["useCustomValues"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithUseCustomPanelId(bool value)
    {
        queryParams["useCustomPanelId"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowThirdItem(bool value)
    {
        queryParams["showThirdItem"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowFourthItem(bool value)
    {
        queryParams["showFourthItem"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowInput(bool value)
    {
        queryParams["showInput"] = value.ToString().ToLowerInvariant();
        return this;
    }

    // Slider-specific parameters
    public TestPageUrlBuilder WithDefaultSliderValue(double value)
    {
        queryParams["defaultValue"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return this;
    }

    public TestPageUrlBuilder WithDefaultSliderValues(params double[] values)
    {
        queryParams["defaultValues"] = string.Join(",", values.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        return this;
    }

    public TestPageUrlBuilder WithMin(double value)
    {
        queryParams["min"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return this;
    }

    public TestPageUrlBuilder WithMax(double value)
    {
        queryParams["max"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return this;
    }

    public TestPageUrlBuilder WithStep(double value)
    {
        queryParams["step"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return this;
    }

    public TestPageUrlBuilder WithLargeStep(double value)
    {
        queryParams["largeStep"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return this;
    }

    public TestPageUrlBuilder WithMinStepsBetweenValues(int value)
    {
        queryParams["minStepsBetweenValues"] = value.ToString();
        return this;
    }

    public TestPageUrlBuilder WithReadOnly(bool value)
    {
        queryParams["readOnly"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithRequired(bool value)
    {
        queryParams["required"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithThumbCollisionBehavior(string behavior)
    {
        queryParams["thumbCollisionBehavior"] = behavior;
        return this;
    }

    public TestPageUrlBuilder WithShowRangeSlider(bool value)
    {
        queryParams["showRangeSlider"] = value.ToString().ToLowerInvariant();
        return this;
    }

    // Switch-specific parameters
    public TestPageUrlBuilder WithDefaultChecked(bool value)
    {
        queryParams["defaultChecked"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithChecked(bool value)
    {
        queryParams["checked"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithNativeButton(bool value)
    {
        queryParams["nativeButton"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithSwitchName(string name)
    {
        queryParams["name"] = name;
        return this;
    }

    public TestPageUrlBuilder WithSwitchValue(string value)
    {
        queryParams["value"] = value;
        return this;
    }

    public TestPageUrlBuilder WithUncheckedValue(string value)
    {
        queryParams["uncheckedValue"] = value;
        return this;
    }

    public TestPageUrlBuilder WithShowLabel(bool value)
    {
        queryParams["showLabel"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowWrappingLabel(bool value)
    {
        queryParams["showWrappingLabel"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowForm(bool value)
    {
        queryParams["showForm"] = value.ToString().ToLowerInvariant();
        return this;
    }

    // Checkbox-specific parameters
    public TestPageUrlBuilder WithIndeterminate(bool value)
    {
        queryParams["indeterminate"] = value.ToString().ToLowerInvariant();
        return this;
    }

    // CheckboxGroup-specific parameters
    public TestPageUrlBuilder WithGroupDefaultValue(params string[] values)
    {
        queryParams["defaultValue"] = string.Join(",", values);
        return this;
    }

    public TestPageUrlBuilder WithGroupValue(params string[] values)
    {
        queryParams["value"] = string.Join(",", values);
        return this;
    }

    public TestPageUrlBuilder WithShowParent(bool value)
    {
        queryParams["showParent"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithGroupName(string name)
    {
        queryParams["name"] = name;
        return this;
    }

    // Popover-specific parameters
    public TestPageUrlBuilder WithShowClose(bool value)
    {
        queryParams["showClose"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowArrow(bool value)
    {
        queryParams["showArrow"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowTitle(bool value)
    {
        queryParams["showTitle"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowDescription(bool value)
    {
        queryParams["showDescription"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowBackdrop(bool value)
    {
        queryParams["showBackdrop"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithOutsideAboveBackdrop(bool value)
    {
        queryParams["outsideAboveBackdrop"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithSide(string side)
    {
        queryParams["side"] = side;
        return this;
    }

    public TestPageUrlBuilder WithAlign(string align)
    {
        queryParams["align"] = align;
        return this;
    }

    public TestPageUrlBuilder WithSideOffset(int value)
    {
        queryParams["sideOffset"] = value.ToString();
        return this;
    }

    public TestPageUrlBuilder WithAlignOffset(int value)
    {
        queryParams["alignOffset"] = value.ToString();
        return this;
    }

    public TestPageUrlBuilder WithShowFocusableContent(bool value)
    {
        queryParams["showFocusableContent"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithPopoverUseInitialFocus(bool value)
    {
        queryParams["useInitialFocus"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithPopoverUseFinalFocus(bool value)
    {
        queryParams["useFinalFocus"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithDisableInitialFocus(bool value)
    {
        queryParams["disableInitialFocus"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithLayout(string layout)
    {
        queryParams["layout"] = layout;
        return this;
    }

    public TestPageUrlBuilder WithParentModal(bool value)
    {
        queryParams["parentModal"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithChildModal(bool value)
    {
        queryParams["childModal"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithUseHandle(bool value)
    {
        queryParams["useHandle"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithUseViewport(bool value)
    {
        queryParams["useViewport"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowPayload(bool value)
    {
        queryParams["showPayload"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowProgrammaticButtons(bool value)
    {
        queryParams["showProgrammaticButtons"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithDefaultTriggerId(string triggerId)
    {
        queryParams["defaultTriggerId"] = triggerId;
        return this;
    }

    // Tooltip-specific parameters
    public TestPageUrlBuilder WithDelay(int delayMs)
    {
        queryParams["delay"] = delayMs.ToString();
        return this;
    }

    public TestPageUrlBuilder WithDisableHoverablePopup(bool value)
    {
        queryParams["disableHoverablePopup"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithFlexLayout(bool value)
    {
        queryParams["flexLayout"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithMultiTrigger(bool value)
    {
        queryParams["multiTrigger"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithProviderGroup(bool value)
    {
        queryParams["providerGroup"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithProviderTimeout(int timeoutMs)
    {
        queryParams["providerTimeout"] = timeoutMs.ToString();
        return this;
    }

    public TestPageUrlBuilder WithStyledArrow(bool value)
    {
        queryParams["styledArrow"] = value.ToString().ToLowerInvariant();
        return this;
    }

    // Field/Form-specific parameters
    public TestPageUrlBuilder WithValidationMode(string mode)
    {
        queryParams["validationMode"] = mode;
        return this;
    }

    public TestPageUrlBuilder WithFieldName(string name)
    {
        queryParams["fieldName"] = name;
        return this;
    }

    public TestPageUrlBuilder WithValidationDebounceTime(int ms)
    {
        queryParams["validationDebounceTime"] = ms.ToString();
        return this;
    }

    public TestPageUrlBuilder WithShowSecondField(bool value)
    {
        queryParams["showSecondField"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowError(bool value)
    {
        queryParams["showError"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowFieldValidity(bool value)
    {
        queryParams["showFieldValidity"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithMatchValidity(string match)
    {
        queryParams["matchValidity"] = match;
        return this;
    }

    public TestPageUrlBuilder WithCustomValidation(string validation)
    {
        queryParams["customValidation"] = validation;
        return this;
    }

    public TestPageUrlBuilder WithShowActionsRef(bool value)
    {
        queryParams["showActionsRef"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithErrors(string errors)
    {
        queryParams["errors"] = errors;
        return this;
    }

    public TestPageUrlBuilder WithTestScenario(string scenario)
    {
        queryParams["scenario"] = scenario;
        return this;
    }

    public TestPageUrlBuilder WithShowFieldItem(bool value)
    {
        queryParams["showFieldItem"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithFieldItemType(string type)
    {
        queryParams["fieldItemType"] = type;
        return this;
    }

    public TestPageUrlBuilder WithNativeLabel(bool value)
    {
        queryParams["nativeLabel"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithNoValidate(bool value)
    {
        queryParams["noValidate"] = value.ToString().ToLowerInvariant();
        return this;
    }

    // Dialog-specific parameters
    public TestPageUrlBuilder WithShowNestedDialog(bool value)
    {
        queryParams["showNestedDialog"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowViewport(bool value)
    {
        queryParams["showViewport"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowActionsButtons(bool value)
    {
        queryParams["showActionsButtons"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithUseInitialFocus(bool value)
    {
        queryParams["useInitialFocus"] = value.ToString().ToLowerInvariant();
        return this;
    }

    // Drawer-specific parameters
    public TestPageUrlBuilder WithSnapPoints(bool value)
    {
        queryParams["snapPoints"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithSnapToSequentialPoints(bool value)
    {
        queryParams["snapToSequentialPoints"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithSwipeDirection(string value)
    {
        queryParams["swipeDirection"] = value;
        return this;
    }

    public TestPageUrlBuilder WithSwipeAreaDisabled(bool value)
    {
        queryParams["swipeAreaDisabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    // RadioGroup-specific parameters
    public TestPageUrlBuilder WithRadioDefaultValue(string value)
    {
        queryParams["defaultValue"] = value;
        return this;
    }

    public TestPageUrlBuilder WithRadioValue(string value)
    {
        queryParams["value"] = value;
        return this;
    }

    public TestPageUrlBuilder WithRadioName(string name)
    {
        queryParams["name"] = name;
        return this;
    }

    // Button-specific parameters
    public TestPageUrlBuilder WithButtonFocusableWhenDisabled(bool value)
    {
        queryParams["buttonFocusableWhenDisabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithButtonTabIndex(int value)
    {
        queryParams["buttonTabIndex"] = value.ToString();
        return this;
    }

    // Progress-specific parameters
    public TestPageUrlBuilder WithProgressValue(double? value)
    {
        if (value.HasValue)
            queryParams["value"] = value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        else
            queryParams["indeterminate"] = "true";
        return this;
    }

    public TestPageUrlBuilder WithProgressFormat(string format)
    {
        queryParams["format"] = format;
        return this;
    }

    public TestPageUrlBuilder WithLocale(string locale)
    {
        queryParams["locale"] = locale;
        return this;
    }

    public TestPageUrlBuilder WithShowProgressLabel(bool value)
    {
        queryParams["showLabel"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowProgressValue(bool value)
    {
        queryParams["showValue"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithLabelText(string text)
    {
        queryParams["labelText"] = text;
        return this;
    }

    // Toggle-specific parameters
    public TestPageUrlBuilder WithToggleDefaultPressed(bool value)
    {
        queryParams["defaultPressed"] = value.ToString().ToLowerInvariant();
        return this;
    }

    // ToggleGroup-specific parameters
    public TestPageUrlBuilder WithToggleGroupDefaultValue(params string[] values)
    {
        queryParams["defaultValue"] = string.Join(",", values);
        return this;
    }

    public TestPageUrlBuilder WithToggleItem2Disabled(bool value)
    {
        queryParams["item2Disabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    // Toolbar-specific parameters
    public TestPageUrlBuilder WithShowGroup(bool value)
    {
        queryParams["showGroup"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowLink(bool value)
    {
        queryParams["showLink"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowSeparator(bool value)
    {
        queryParams["showSeparator"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithGroupDisabled(bool value)
    {
        queryParams["groupDisabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithToolbarButton2Disabled(bool value)
    {
        queryParams["button2Disabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithToolbarFocusableWhenDisabled(bool value)
    {
        queryParams["focusableWhenDisabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    // Tabs-specific parameters
    public TestPageUrlBuilder WithActivateOnFocus(bool value)
    {
        queryParams["activateOnFocus"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithTab1Disabled(bool value)
    {
        queryParams["tab1Disabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithTab2Disabled(bool value)
    {
        queryParams["tab2Disabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithTab3Disabled(bool value)
    {
        queryParams["tab3Disabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithTabsDefaultValue(string value)
    {
        queryParams["defaultValue"] = value;
        return this;
    }

    public TestPageUrlBuilder WithShowIndicator(bool value)
    {
        queryParams["showIndicator"] = value.ToString().ToLowerInvariant();
        return this;
    }

    // Meter-specific parameters
    public TestPageUrlBuilder WithMeterValue(double value)
    {
        queryParams["value"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return this;
    }

    public TestPageUrlBuilder WithMeterFormat(string format)
    {
        queryParams["format"] = format;
        return this;
    }

    public TestPageUrlBuilder WithShowMeterLabel(bool value)
    {
        queryParams["showLabel"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowMeterValue(bool value)
    {
        queryParams["showValue"] = value.ToString().ToLowerInvariant();
        return this;
    }

    // NumberField-specific parameters
    public TestPageUrlBuilder WithNumberFieldDefaultValue(double value)
    {
        queryParams["defaultValue"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return this;
    }

    public TestPageUrlBuilder WithNumberFieldAllowWheelScrub(bool value)
    {
        queryParams["allowWheelScrub"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithNumberFieldSnapOnStep(bool value)
    {
        queryParams["snapOnStep"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithNumberFieldName(string name)
    {
        queryParams["name"] = name;
        return this;
    }

    public TestPageUrlBuilder WithNumberFieldShowForm(bool value)
    {
        queryParams["showForm"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithNumberFieldShowScrubArea(bool value)
    {
        queryParams["showScrubArea"] = value.ToString().ToLowerInvariant();
        return this;
    }

    // ContextMenu-specific parameters
    public TestPageUrlBuilder WithContextMenuShowSubmenu(bool value)
    {
        queryParams["showSubmenu"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithContextMenuShowNestedContextMenu(bool value)
    {
        queryParams["showNestedContextMenu"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithContextMenuShowPopupGap(bool value)
    {
        queryParams["showPopupGap"] = value.ToString().ToLowerInvariant();
        return this;
    }

    // Navigation Menu

    public TestPageUrlBuilder WithNavDefaultValue(string value)
    {
        queryParams["defaultValue"] = value;
        return this;
    }

    public TestPageUrlBuilder WithNavDelay(int value)
    {
        queryParams["delay"] = value.ToString();
        return this;
    }

    public TestPageUrlBuilder WithNavCloseDelay(int value)
    {
        queryParams["closeDelay"] = value.ToString();
        return this;
    }

    public TestPageUrlBuilder WithNavOrientation(string value)
    {
        queryParams["orientation"] = value;
        return this;
    }

    public TestPageUrlBuilder WithNavDisableItem1(bool value)
    {
        queryParams["disableItem1"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithShowNestedNav(bool value)
    {
        queryParams["showNestedNav"] = value.ToString().ToLowerInvariant();
        return this;
    }

    // Select-specific parameters
    public TestPageUrlBuilder WithSelectDefaultValue(string value)
    {
        queryParams["defaultValue"] = value;
        return this;
    }

    public TestPageUrlBuilder WithSelectValue(string value)
    {
        queryParams["value"] = value;
        return this;
    }

    public TestPageUrlBuilder WithSelectMultiple(bool value)
    {
        queryParams["multiple"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithSelectName(string name)
    {
        queryParams["selectName"] = name;
        return this;
    }

    // Autocomplete-specific parameters
    public TestPageUrlBuilder WithAutocompleteDefaultValue(string value)
    {
        queryParams["defaultValue"] = value;
        return this;
    }

    public TestPageUrlBuilder WithAutocompleteMode(string value)
    {
        queryParams["mode"] = value;
        return this;
    }

    public TestPageUrlBuilder WithAutocompleteAutoHighlight(string value)
    {
        queryParams["autoHighlight"] = value;
        return this;
    }

    public TestPageUrlBuilder WithAutocompleteFilterDisabled(bool value)
    {
        queryParams["filterDisabled"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public TestPageUrlBuilder WithAutocompleteInputInsidePopup(bool value)
    {
        queryParams["inputInsidePopup"] = value.ToString().ToLowerInvariant();
        return this;
    }

    public string Build()
    {
        var sb = new StringBuilder();
        sb.Append(baseAddress);
        sb.Append(path);

        if (queryParams.Count > 0)
        {
            sb.Append('?');
            var first = true;
            foreach (var (key, value) in queryParams)
            {
                if (!first) sb.Append('&');
                sb.Append(Uri.EscapeDataString(key));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(value));
                first = false;
            }
        }

        return sb.ToString();
    }
}
