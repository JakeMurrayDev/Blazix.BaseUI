namespace Blazix.BaseUI.Tests.Contracts.NumberField;

public interface INumberFieldScrubAreaContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task HasRolePresentation();
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
}
