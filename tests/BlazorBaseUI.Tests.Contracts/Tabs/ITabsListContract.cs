namespace BlazorBaseUI.Tests.Contracts.Tabs;

public interface ITabsListContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // ARIA
    Task HasRoleTablist();
    Task DoesNotHaveAriaOrientationWhenHorizontal();
    Task HasAriaOrientationVerticalWhenVertical();
    Task CanBeNamedViaAriaLabel();
    Task CanBeNamedViaAriaLabelledby();

    // Data attributes
    Task HasDataOrientationHorizontal();
    Task HasDataActivationDirection();

    // Element reference
    Task ExposesElementReference();

    // Validation
    Task ThrowsWhenNotInTabsRoot();
}
