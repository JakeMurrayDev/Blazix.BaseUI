namespace Blazix.BaseUI.Tests.Contracts.Menu;

public interface IMenuSubmenuTriggerContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task HasAriaHaspopupMenu();
    Task HasAriaExpandedFalseWhenClosed();
    Task HasAriaExpandedTrueWhenOpen();
    Task OmitsAriaExpandedForVoiceOverWhenOpenedByKeyboard();
    Task KeepsAriaExpandedForVoiceOverWhenOpenedByPointer();
    Task HasDataPopupOpenWhenOpen();
    Task DoesNotHaveDataPopupOpenWhenClosed();
    Task HasDataDisabledWhenDisabled();
    Task HasAriaDisabledWhenDisabled();
    Task RequiresSubmenuContext();
    Task CloseDelayDefaultsToZero();
    Task HighlightsOnMouseEnter();
    Task DoesNotToggleOnClickWhenOpenOnHover();

    Task OpensOnAVirtualPressWhenOpenOnHover();
    Task DoesNotOpenOnAnOrdinaryMousePressWhenOpenOnHover();
}
