namespace BlazorBaseUI.Tests.Contracts.Radio;

public interface IRadioRootContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();
    Task OverridesBuiltInAttributes();

    // ARIA and role
    Task HasRoleRadio();
    Task HasAriaCheckedFalseByDefault();
    Task HasAriaCheckedTrueWhenChecked();
    Task HasAriaRequiredWhenRequired();
    Task DoesNotHaveAriaRequiredByDefault();
    Task HasTabindexZeroByDefault();
    Task HasTabindexMinusOneWhenDisabled();

    // Disabled
    Task HasDataDisabledWhenDisabled();
    Task HasAriaDisabledWhenDisabled();
    Task DoesNotHaveDataDisabledByDefault();
    Task DoesNotChangeStateWhenClickedDisabled();

    // ReadOnly
    Task HasAriaReadonlyWhenReadOnly();
    Task DoesNotHaveAriaReadonlyByDefault();
    Task DoesNotChangeStateWhenClickedReadOnly();

    // Value prop
    Task DoesNotForwardValuePropToRoot();
    Task AllowsNullValue();
    Task StandaloneEmptyValueIsChecked();

    // Name and hidden input
    Task RendersHiddenRadioInput();
    Task HiddenInputHasCorrectAttributes();
    Task InputHasId();
    Task ExplicitIdAssociatesHiddenInputNotNonNativeRoot();
    Task ExplicitIdLookupIsCaseInsensitive();
    Task CombinesCaseInsensitiveAriaDescribedByWithFieldDescriptions();
    Task StandaloneBlurValidationDoesNotBlockBlurEvent();
    Task NativeButtonUsesExplicitIdOnRootAndOmitsHiddenInputId();
    Task SetsNameOnInputOnly();
    Task SetsValueOnHiddenInput();
    Task NullValueSerializesToEmptyInputValue();
    Task NonJsonValueUsesStringFallbackForHiddenInputValue();
    Task GroupNavigationUsesRegisteredValueInsteadOfSerializedFormFallback();

    // Style hooks (data attributes)
    Task HasDataCheckedWhenChecked();
    Task HasDataUncheckedWhenUnchecked();
    Task HasDataDisabledWhenDisabled_StyleHook();
    Task HasDataReadonlyWhenReadOnly_StyleHook();
    Task HasDataRequiredWhenRequired_StyleHook();
    Task PlacesStyleHooksOnRootAndIndicator();
    Task DataCheckedTogglesOnSelection();

    // Context
    Task CascadesContextToChildren();

    // Element reference
    Task ExposesElementReference();

    // ClassValue/StyleValue state
    Task ClassValueReceivesCorrectState();
    Task StyleValueReceivesCorrectState();

    // Group integration
    Task InheritsDisabledFromGroup();
    Task SetsTabindexBasedOnGroupSelection();
}
