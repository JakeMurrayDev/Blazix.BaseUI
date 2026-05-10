namespace BlazorBaseUI.Tests.Contracts.Popover;

public interface IPopoverBackdropContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task HasDataOpenWhenOpen();
    Task HasPointerEventsNoneWhenHoverOpened();
}
