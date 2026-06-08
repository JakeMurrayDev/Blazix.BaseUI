namespace Blazix.BaseUI.Tests.Contracts.Dialog;

public interface IDialogDescriptionContract
{
    Task RendersAsParagraphByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task GeneratesIdForAriaDescribedBy();
}
