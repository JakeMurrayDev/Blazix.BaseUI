namespace Blazix.BaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuTriggerContract
{
    Task RendersButtonByDefault();
    Task ForwardsAdditionalAttributes();
    Task HasAriaExpandedFalse();
    Task HasAriaExpandedTrue();
    Task NoAriaControlsWhenClosed();
    Task HasAriaControlsWhenOpen();
    Task HasTabIndex();
    Task NoDataPopupOpenWhenClosed();
    Task HasDataPopupOpenWhenOpen();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task ToggleOnClick();
    Task DoesNotToggleWhenDisabled();
    Task RequiresContext();
    Task UsesAriaDisabledWhenDisabled();
    Task RemainsInTabOrderWhenDisabled();
    Task HasKeyDownHandlerWired();
    Task HasFocusHandlerWired();
    Task HasBlurHandlerWired();
    Task HasPointerDownHandlerWired();
    Task HasMouseMoveHandlerWired();
    Task RendersFocusGuardsWhenActive();
    Task NoFocusGuardsWhenInactive();
    Task RendersAriaOwnsWhenActive();
    Task NoAriaOwnsWhenInactive();
    Task FocusGuardsRenderedAfterTrigger();
}
