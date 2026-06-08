namespace Blazix.BaseUI.Tests.Contracts.Dialog;

public interface IDialogPortalContract
{
    Task RendersChildrenWhenOpen();
    Task DoesNotRenderWhenClosed();
    Task KeepMountedTrue_StaysMounted();
}
