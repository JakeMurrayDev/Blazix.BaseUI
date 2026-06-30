namespace Blazix.BaseUI.Tests.Contracts.Menu;

public interface IMenuPopupContract
{
    Task DefaultReturnFocusIsTrue();
    Task FinalFocusNoneDisablesReturnFocus();
    Task FinalFocusDefaultEnablesReturnFocus();
    Task PopupIsLabelledByTrigger();
}
