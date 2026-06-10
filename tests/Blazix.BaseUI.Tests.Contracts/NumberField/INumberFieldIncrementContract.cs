namespace Blazix.BaseUI.Tests.Contracts.NumberField;

public interface INumberFieldIncrementContract
{
    // Rendering
    Task RendersAsButtonByDefault();
    Task HasIncreaseLabel();
    Task RendersWithCustomRender();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // Element reference
    Task ExposesElementReference();

    // Basic increment
    Task IncrementsStartingFromZeroOnClick();
    Task IncrementsToOneFromDefaultValueZero();
    Task FirstIncrementAfterExternalControlledUpdate();
    Task OnlyCallsOnValueChangeOncePerIncrement();

    // Press and hold (via JS callback)
    Task IncrementsContinuouslyWhenHoldingPointerDown();
    Task DoesNotIncrementTwiceWithPointerDownAndClick();

    // State
    Task DoesNotIncrementWhenReadOnly();
    Task IncrementsWhenInputIsDirtyNotBlurred_Click();
    Task IncrementsWhenInputIsDirtyNotBlurred_PointerDown();

    // SnapOnStep
    Task SnapOnStep_IncrementsWithoutRoundingWhenFalse();
    Task SnapOnStep_SnapsOnIncrementWhenTrue();
    Task SnapOnStep_IncrementsWithRespectToMinValue();

    // Disabled
    Task DoesNotIncrementWhenRootDisabled();
    Task DoesNotIncrementWhenButtonDisabled();
    Task HasDataDisabledWhenRootDisabled();
    Task HasDataDisabledWhenButtonDisabled();

    // Max boundary
    Task DisabledWhenAtMax();

    // Data attributes
    Task HasDataDisabledWhenDisabled();
    Task HasDataReadOnlyWhenReadOnly();
    Task HasDataRequiredWhenRequired();

    // Native button attributes
    Task NativeButton_HasTypeButton();
    Task NativeButton_HasTabIndexMinusOne();
    Task NativeButton_HasAriaLabelIncrease();
}
