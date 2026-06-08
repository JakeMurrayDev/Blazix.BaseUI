namespace Blazix.BaseUI.Tests.Contracts.Accordion;

public interface IAccordionTriggerContract
{
    Task RendersAsButtonByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasAriaExpandedFalseWhenClosed();
    Task HasAriaExpandedTrueWhenOpen();
    Task HasAriaControlsWhenOpen();
    Task HasDataPanelOpenWhenOpen();
    Task HasDataDisabledWhenDisabled();
    Task HasDataIndexAndOrientation();
    Task HasTypeButtonWhenNativeButton();
    Task HasRoleButtonWhenNotNativeButton();
    Task TogglesOnClick();
    Task DisabledTriggerIgnoresClick();
}
