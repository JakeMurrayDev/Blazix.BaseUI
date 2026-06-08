namespace Blazix.BaseUI.Tests.Contracts.Tooltip;

public interface ITooltipTriggerContract
{
    Task RendersAsButtonByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task HasAriaDescribedByWhenOpen();
    Task HasDataPopupOpenWhenOpen();
    Task HasDisabledAttributeWhenDisabled();
    Task DoesNotOpenWhenDisabled();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task RequiresContext();
}
