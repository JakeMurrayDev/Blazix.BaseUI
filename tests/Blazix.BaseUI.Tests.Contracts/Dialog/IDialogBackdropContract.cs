namespace Blazix.BaseUI.Tests.Contracts.Dialog;

public interface IDialogBackdropContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasRolePresentation();
    Task RendersWhenModalFalse();
    Task ForceRenderOnlyRootByDefault();
    Task ForceRenderAllWhenTrue();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
}
