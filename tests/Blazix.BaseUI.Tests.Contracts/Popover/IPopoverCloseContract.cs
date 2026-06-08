namespace Blazix.BaseUI.Tests.Contracts.Popover;

public interface IPopoverCloseContract
{
    Task RendersAsButtonByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task HasTypeButtonAttribute();
    Task ClosesPopoverOnClick();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
}
