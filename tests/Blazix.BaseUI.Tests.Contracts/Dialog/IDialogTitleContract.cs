namespace Blazix.BaseUI.Tests.Contracts.Dialog;

public interface IDialogTitleContract
{
    Task RendersAsH2ByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task GeneratesIdForAriaLabelledBy();
}
