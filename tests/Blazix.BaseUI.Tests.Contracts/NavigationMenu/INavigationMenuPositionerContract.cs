namespace Blazix.BaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuPositionerContract
{
    Task RendersDivByDefault();
    Task ForwardsAdditionalAttributes();
    Task HasRolePresentation();
    Task HasDataSide();
    Task HasDataAlign();
    Task IsInertWhenClosed();
    Task AppliesClassValue();
    Task RequiresContext();
}
