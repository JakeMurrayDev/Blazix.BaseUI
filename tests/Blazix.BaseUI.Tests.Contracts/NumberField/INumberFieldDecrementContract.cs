namespace Blazix.BaseUI.Tests.Contracts.NumberField;

public interface INumberFieldDecrementContract
{
    // Rendering
    Task RendersAsButtonByDefault();
    Task HasDecreaseLabel();
    Task RendersWithCustomRender();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // Element reference
    Task ExposesElementReference();

    // Basic decrement
    Task DecrementsStartingFromZeroOnClick();
    Task DecrementsToMinusOneFromDefaultValueZero();
    Task FirstDecrementAfterExternalControlledUpdate();
    Task OnlyCallsOnValueChangeOncePerDecrement();

    // Press and hold (via JS callback)
    Task DecrementsContinuouslyWhenHoldingPointerDown();
    Task DoesNotDecrementTwiceWithPointerDownAndClick();

    // State
    Task DoesNotDecrementWhenReadOnly();
    Task DecrementsWhenInputIsDirtyNotBlurred_Click();
    Task DecrementsWhenInputIsDirtyNotBlurred_PointerDown();

    // SnapOnStep
    Task SnapOnStep_DecrementsWithoutRoundingWhenFalse();
    Task SnapOnStep_SnapsOnDecrementWhenTrue();
    Task SnapOnStep_DecrementsWithRespectToMinValue();

    // Disabled
    Task DoesNotDecrementWhenRootDisabled();
    Task DoesNotDecrementWhenButtonDisabled();
    Task HasDataDisabledWhenRootDisabled();
    Task HasDataDisabledWhenButtonDisabled();

    // Min boundary
    Task DisabledWhenAtMin();

    // Data attributes
    Task HasDataDisabledWhenDisabled();
    Task HasDataReadOnlyWhenReadOnly();
    Task HasDataRequiredWhenRequired();

    // Native button attributes
    Task NativeButton_HasTypeButton();
    Task NativeButton_HasTabIndexMinusOne();
    Task NativeButton_HasAriaLabelDecrease();
}
