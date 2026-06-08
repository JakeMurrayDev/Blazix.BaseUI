namespace Blazix.BaseUI.Tests.Contracts.Popover;

public interface IPopoverPositionerContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task HasDataSideAttribute();
    Task HasDataAlignAttribute();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task UpdatesDataSideFromJsCallback();
    Task UpdatesDataAlignFromJsCallback();
    Task SetsDataAnchorHiddenFromJsCallback();
    Task RendersInternalBackdropWhenModalAndPressed();
    Task DoesNotRenderInternalBackdropWhenNotModal();
    Task DoesNotRenderInternalBackdropWhenHoverOpened();
}
