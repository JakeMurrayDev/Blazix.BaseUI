namespace Blazix.BaseUI.Tests.Contracts.Tooltip;

public interface ITooltipPopupContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task HasRoleTooltip();
    Task HasDataSideAttribute();
    Task HasDataAlignAttribute();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task RendersChildren();
    Task RequiresContext();
}
