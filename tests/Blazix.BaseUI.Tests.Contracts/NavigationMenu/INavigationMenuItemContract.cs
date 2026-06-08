namespace Blazix.BaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuItemContract
{
    Task RendersLiByDefault();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task RequiresContext();
}
