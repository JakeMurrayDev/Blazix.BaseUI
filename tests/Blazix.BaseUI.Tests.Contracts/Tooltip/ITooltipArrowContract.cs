namespace Blazix.BaseUI.Tests.Contracts.Tooltip;

public interface ITooltipArrowContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task HasAriaHiddenTrue();
    Task HasDataSideAttribute();
    Task HasDataAlignAttribute();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task RequiresContext();
}
