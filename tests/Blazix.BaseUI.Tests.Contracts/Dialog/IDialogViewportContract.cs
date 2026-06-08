namespace Blazix.BaseUI.Tests.Contracts.Dialog;

public interface IDialogViewportContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasRolePresentation();
    Task RendersOnlyWhenMounted();
    Task StaysMountedWithKeepMounted();
}
