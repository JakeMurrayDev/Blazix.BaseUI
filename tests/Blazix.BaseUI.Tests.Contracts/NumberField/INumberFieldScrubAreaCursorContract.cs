namespace Blazix.BaseUI.Tests.Contracts.NumberField;

public interface INumberFieldScrubAreaCursorContract
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

    // Conditional rendering
    Task DoesNotRenderWhenNotScrubbing();

    // Data attributes
    Task HasDataDisabledWhenDisabled();
    Task HasDataReadOnlyWhenReadOnly();
    Task HasDataRequiredWhenRequired();
}
