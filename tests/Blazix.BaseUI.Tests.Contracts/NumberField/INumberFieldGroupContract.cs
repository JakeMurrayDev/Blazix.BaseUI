namespace Blazix.BaseUI.Tests.Contracts.NumberField;

public interface INumberFieldGroupContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task HasRoleGroup();
    Task RendersWithCustomRender();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // Element reference
    Task ExposesElementReference();

    // Data attributes
    Task HasDataDisabledWhenDisabled();
    Task HasDataReadOnlyWhenReadOnly();
    Task HasDataRequiredWhenRequired();
    Task HasDataScrubbingAttribute();
    Task HasDataTouchedAttribute();
    Task HasDataDirtyAttribute();
    Task HasDataFilledAttribute();
    Task HasDataFocusedAttribute();
}
