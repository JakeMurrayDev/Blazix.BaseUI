namespace Blazix.BaseUI.Tests.Contracts.Dialog;

public interface IDialogTriggerContract
{
    Task RendersAsButtonByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task DisabledPreventsOpening();
    Task DisabledCustomElement();
    Task HasAriaHasPopupDialog();
    Task HasAriaExpandedFalseWhenClosed();
    Task HasAriaExpandedTrueWhenOpen();
}
