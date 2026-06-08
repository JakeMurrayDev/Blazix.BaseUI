namespace Blazix.BaseUI.Tests.Contracts.Switch;

public interface ISwitchThumbContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();
    Task DataAttributesCanBeOverridden();

    // Style hooks (data attributes)
    Task HasDataCheckedWhenChecked();
    Task HasDataUncheckedWhenUnchecked();
    Task HasDataDisabledWhenDisabled();
    Task HasDataReadonlyWhenReadOnly();
    Task HasDataRequiredWhenRequired();

    // Context
    Task ReceivesStateFromContext();
    Task ThrowsOutsideRoot();

    // State
    Task ClassValueReceivesCorrectState();
    Task StyleValueReceivesCorrectState();
}
