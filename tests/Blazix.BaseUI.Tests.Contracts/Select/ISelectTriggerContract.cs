namespace Blazix.BaseUI.Tests.Contracts.Select;

public interface ISelectTriggerContract
{
    Task Disabled_CannotInteractWhenDisabled();
    Task Placeholder_ShouldHaveDataPlaceholderWhenNoValue();
    Task Placeholder_ShouldNotHaveDataPlaceholderWhenValueProvided();
    Task StyleHooks_ShouldHaveDataPopupOpenAndPressedWhenOpen();
    Task Required_SetsAriaRequiredAttribute();

    // Disabled
    Task Disabled_DoesNotTogglePopupWhenDisabled();

    // Placeholder
    Task Placeholder_DataPlaceholderWithCustomItemToStringValue();
    Task Placeholder_DataPlaceholderWhenProvidedNullValue();
    Task Placeholder_NoDataPlaceholderWhenMultipleModeHasDefaultValue();

    // NativeButton
    Task NativeButton_RendersTypeButtonByDefault();
    Task NativeButton_FalseDoesNotRenderTypeButton();
    Task NativeButton_FalseRendersTabindex();
    Task NativeButton_FalseRendersTabindexMinusOneWhenDisabled();

    // FieldRoot integration
    Task FieldRoot_HasDataValidWhenValid();
    Task FieldRoot_HasDataInvalidWhenInvalid();
    Task FieldRoot_HasDataTouchedDirtyFilledFocused();
    Task FieldRoot_HasAriaLabelledBy();
    Task FieldRoot_HasAriaDescribedBy();
    Task FieldRoot_HasAriaInvalidWhenFieldInvalid();

    // Keyboard vs pointer tracking
    Task KeyDown_SetsKeyboardActive();
    Task PointerMove_ClearsKeyboardActive();

    // Focus coordination
    Task Focus_SetsForceMount();
    Task Focus_ClosesPopupWhenAlignItemWithTriggerActive();
    Task Focus_DoesNotCloseWhenItIsTheSideEffectOfTheClickThatJustOpenedThePopup();

    // JS-driven callbacks
    Task NotifyCancelOpen_ClosesOpenPopupWithCancelOpenReason();
    Task NotifyRealBlur_InvokesTriggerBlur();

    // Selection mouseup gating
    Task SelectionGating_PlaceholderOnly_OpensBothFlagsAfterSelectedDelay();
    Task SelectionGating_WithSelectedItem_OpensUnselectedThenSelected();
    Task SelectionGating_ClosedResetsFlags();

    // State record
    Task State_ExposesCurrentValue();
}
