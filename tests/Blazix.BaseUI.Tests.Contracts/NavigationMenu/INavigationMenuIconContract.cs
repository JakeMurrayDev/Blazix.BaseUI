namespace Blazix.BaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuIconContract
{
    Task RendersSpanByDefault();
    Task HasDefaultArrowContent();
    Task HasAriaHidden();
    Task NoDataPopupOpenWhenClosed();
    Task HasDataPopupOpenWhenOpen();
    Task AppliesClassValue();
    Task RequiresContext();
}
