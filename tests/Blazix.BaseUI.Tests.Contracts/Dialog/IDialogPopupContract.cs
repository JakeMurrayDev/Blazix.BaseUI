namespace Blazix.BaseUI.Tests.Contracts.Dialog;

public interface IDialogPopupContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task KeepMountedTrue_DialogStaysMounted();
    Task KeepMountedFalse_DialogUnmounts();
    Task HasRoleDialog();
    Task HasAriaModalTrueWhenModal();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task HasDataNestedWhenNested();
    Task HasTabIndexNegativeOne();
}
