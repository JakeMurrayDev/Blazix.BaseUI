namespace Blazix.BaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuViewportContract
{
    Task RendersDivByDefault();
    Task ForwardsAdditionalAttributes();
    Task HasGeneratedId();
    Task HasBlurHandlerWired();
    Task AppliesClassValue();
    Task RequiresContext();
}
