namespace Blazix.BaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuBackdropContract
{
    Task RendersDivByDefault();
    Task ForwardsAdditionalAttributes();
    Task HasRolePresentation();
    Task HasDataClosedWhenClosed();
    Task HasDataOpenWhenOpen();
    Task IsHiddenWhenNotMounted();
    Task IsNotHiddenWhenMounted();
    Task HasUserSelectNoneStyle();
    Task HasDataStartingStyleDuringTransition();
    Task HasDataEndingStyleDuringTransition();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task RendersWithCustomRender();
    Task RequiresContext();
}
