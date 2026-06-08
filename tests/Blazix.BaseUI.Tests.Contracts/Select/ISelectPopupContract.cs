namespace Blazix.BaseUI.Tests.Contracts.Select;

public interface ISelectPopupContract
{
    Task HasAriaAttributesWhenNoSelectListPresent();
    Task PlacesAriaAttributesOnSelectListIfPresent();
    Task AppliesRolePresentationWhenListPresent();
    Task UsesRootIdDashListAsPopupId();
    Task EmitsAriaMultiselectableWhenMultiple();
    Task OmitsAriaMultiselectableWhenSingleSelect();
    Task AppliesDataSideAndDataAlign();
    Task ForwardsFinalFocusToFloatingFocusManager();
    Task DefaultsFinalFocusToNullSoFocusManagerKeepsLegacyBehavior();
    Task CallsInitializePopupOnFirstRender();
    Task CallsDisposePopupOnDispose();
    Task OnPopupPointerLeaveClearsActiveIndex();
    Task OnWindowResizeClosesSelect();
    Task OnFallbackToAlignPopupToTriggerDisablesAlignment();
}
