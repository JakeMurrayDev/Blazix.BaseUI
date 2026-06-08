namespace Blazix.BaseUI.Tests.Contracts.Popover;

public interface IPopoverTitleContract
{
    Task RendersAsH2ByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task SetsAriaLabelledByOnPopup();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
}
