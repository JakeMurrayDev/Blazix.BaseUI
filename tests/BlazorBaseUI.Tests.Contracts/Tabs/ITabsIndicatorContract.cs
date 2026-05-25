namespace BlazorBaseUI.Tests.Contracts.Tabs;

public interface ITabsIndicatorContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomRender();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // ARIA
    Task HasRolePresentation();

    // Visibility
    Task DoesNotRenderWhenValueIsNull();
    Task RenderBeforeHydrationEmitsScript();

    // Data attributes
    Task HasDataActivationDirection();
    Task HasDataOrientation();

    // Element reference
    Task ExposesElementReference();
}
