namespace BlazorBaseUI.Tests.Contracts.Popover;

public interface IPopoverDescriptionContract
{
    Task RendersAsParagraphByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task SetsAriaDescribedByOnPopup();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
}
