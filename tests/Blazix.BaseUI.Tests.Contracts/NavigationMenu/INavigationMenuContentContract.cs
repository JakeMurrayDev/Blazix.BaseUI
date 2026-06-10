namespace Blazix.BaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuContentContract
{
    Task RendersDivByDefault();
    Task ForwardsAdditionalAttributes();
    Task HasDataOpenWhenActive();
    Task HasDataClosedWhenInactiveWithKeepMounted();
    Task DoesNotRenderWhenInactiveWithoutKeepMounted();
    Task AppliesClassValue();
    Task RequiresContext();
}
