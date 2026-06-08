namespace Blazix.BaseUI.Tests.Contracts.Menu;

public interface IMenuSubmenuTriggerContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task HasAriaHaspopupMenu();
    Task HasAriaExpandedFalseWhenClosed();
    Task HasAriaExpandedTrueWhenOpen();
    Task HasDataPopupOpenWhenOpen();
    Task DoesNotHaveDataPopupOpenWhenClosed();
    Task HasDataDisabledWhenDisabled();
    Task HasAriaDisabledWhenDisabled();
    Task RequiresSubmenuContext();
    Task CloseDelayDefaultsToZero();
    Task HighlightsOnMouseEnter();
    Task DoesNotToggleOnClickWhenOpenOnHover();
}
