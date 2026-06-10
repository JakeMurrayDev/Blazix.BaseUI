namespace Blazix.BaseUI.Tests.Contracts.ContextMenu;

public interface IContextMenuTriggerContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task AppliesTouchCalloutNoneStyle();
    Task TriggerRendersAsDivElement();
    Task AddsPopupOpenDataAttribute();
    Task RemovesPopupOpenDataAttributeWhenClosed();
    Task DoesNotCancelOpenOnMouseUpBefore500ms();
    Task CancelsOpenOnMouseUpAfter500ms();
    Task SetsAnchorFromCursorPosition();
}
