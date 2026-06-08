namespace Blazix.BaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuListContract
{
    Task RendersUlByDefault();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task HasKeyDownHandlerWhenNotNested();
    Task RequiresContext();
}
