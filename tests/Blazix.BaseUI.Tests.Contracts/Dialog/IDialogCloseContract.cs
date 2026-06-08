namespace Blazix.BaseUI.Tests.Contracts.Dialog;

public interface IDialogCloseContract
{
    Task RendersAsButtonByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task DisabledPreventsClosing();
    Task DisabledCustomElement();
    Task ClosesDialogOnClick();
    Task ClosesWithUndefinedOnClick();
}
