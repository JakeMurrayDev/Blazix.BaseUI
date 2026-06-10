namespace Blazix.BaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuArrowContract
{
    Task RendersDivByDefault();
    Task ForwardsAdditionalAttributes();
    Task HasAriaHidden();
    Task HasDataSide();
    Task HasDataAlign();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task RendersWithCustomRender();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task HasDataUncenteredWhenUncentered();
    Task RequiresContext();
}
