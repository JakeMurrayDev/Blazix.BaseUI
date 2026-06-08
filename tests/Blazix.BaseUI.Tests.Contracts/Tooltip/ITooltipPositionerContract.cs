namespace Blazix.BaseUI.Tests.Contracts.Tooltip;

public interface ITooltipPositionerContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task HasRolePresentation();
    Task HasDataSideAttribute();
    Task HasDataAlignAttribute();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task HasHiddenWhenNotMounted();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task CascadesPositionerContext();
    Task RequiresContext();
}
