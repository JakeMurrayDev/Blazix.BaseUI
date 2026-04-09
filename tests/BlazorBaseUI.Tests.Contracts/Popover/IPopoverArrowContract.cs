namespace BlazorBaseUI.Tests.Contracts.Popover;

public interface IPopoverArrowContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task HasDataSideAttribute();
}
