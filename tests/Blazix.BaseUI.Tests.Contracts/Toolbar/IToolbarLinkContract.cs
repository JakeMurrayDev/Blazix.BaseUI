namespace Blazix.BaseUI.Tests.Contracts.Toolbar;

public interface IToolbarLinkContract
{
    // Rendering
    Task RendersAsAnchorByDefault();
    Task RendersWithCustomRenderFragment();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // Data attributes
    Task HasDataOrientationFromRoot();
    Task DoesNotHaveDataDisabledWhenRootDisabled();

    // State cascading
    Task ClassValueReceivesToolbarLinkState();
    Task ClassValueReceivesOrientationFromRoot();

    // Validation
    Task ThrowsWhenNotInsideToolbarRoot();
}
