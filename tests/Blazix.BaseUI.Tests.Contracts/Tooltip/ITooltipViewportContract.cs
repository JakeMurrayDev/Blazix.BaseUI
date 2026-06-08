namespace Blazix.BaseUI.Tests.Contracts.Tooltip;

public interface ITooltipViewportContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task RendersChildrenInCurrentContainer();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task RequiresContext();
}
