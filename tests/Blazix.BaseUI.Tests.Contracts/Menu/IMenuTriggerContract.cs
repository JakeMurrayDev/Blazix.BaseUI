namespace Blazix.BaseUI.Tests.Contracts.Menu;

public interface IMenuTriggerContract
{
    Task RendersAsButtonByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task HasAriaHaspopupMenu();
    Task HasAriaExpandedFalseWhenClosed();
    Task HasAriaExpandedTrueWhenOpen();
    Task HasDataPopupOpenWhenOpen();
    Task HasDisabledWhenDisabled();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task ToggleMenuOnPointerDown();
    Task DoesNotToggleWhenDisabled();
    Task RequiresContext();
    Task CloseDelayDefaultsToZero();
    Task HandleBasedTriggerRegistersOnRender();
    Task HandleBasedTriggerUnregistersOnDispose();
}
