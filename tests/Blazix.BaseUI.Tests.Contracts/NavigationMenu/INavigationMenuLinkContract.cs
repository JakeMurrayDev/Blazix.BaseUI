namespace Blazix.BaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuLinkContract
{
    Task RendersAnchorByDefault();
    Task ForwardsAdditionalAttributes();
    Task HasAriaCurrentPageWhenActive();
    Task NoAriaCurrentWhenInactive();
    Task HasHref();
    Task AppliesClassValue();
    Task RequiresContext();
    Task HasDataActiveWhenActive();
    Task NoDataActiveWhenInactive();
    Task CloseOnClickClosesMenu();
    Task CloseOnClickFalseKeepsMenuOpen();
    Task NestedCloseOnClickClosesParentMenu();
}
