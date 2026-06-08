namespace Blazix.BaseUI.Tests.Contracts.Select;

public interface ISelectItemTextContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();

    // Selected-item-text-element registration (parity with React SelectItemText.localRef)
    Task RegistersSelectedItemTextElementWhenPopupOpens();
    Task ClearsSelectedItemTextElementOnDispose();
    Task RegistersSelectedByFocusItemTextElement();
    Task StaleOwnerDoesNotClearRootOnDispose();
}
