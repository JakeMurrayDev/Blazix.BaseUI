namespace Blazix.BaseUI.Tests.Contracts.Toolbar;

public interface IToolbarGroupContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRenderFragment();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // ARIA
    Task HasRoleGroup();

    // Data attributes
    Task HasDataOrientationFromRoot();
    Task HasDataDisabledWhenDisabled();
    Task DoesNotHaveDataDisabledWhenNotDisabled();
    Task HasDataDisabledWhenRootDisabled();

    // Disabled cascading
    Task CascadesDisabledToChildren();

    // State cascading
    Task ClassValueReceivesToolbarRootState();

    // Validation
    Task ThrowsWhenNotInsideToolbarRoot();
}
