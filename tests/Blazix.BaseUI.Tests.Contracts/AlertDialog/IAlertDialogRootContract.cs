namespace Blazix.BaseUI.Tests.Contracts.AlertDialog;

public interface IAlertDialogRootContract
{
    Task RendersChildren();
    Task UsesAlertDialogRole();
    Task IsAlwaysModal();
    Task DoesNotDismissOnOutsidePress();
    Task DoesNotExposeModalParameter();
    Task DoesNotExposeDisablePointerDismissalParameter();
}
