namespace Blazix.BaseUI.Tests.Contracts.Menu;

public interface IMenuGroupContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // ARIA
    Task HasRoleGroup();
    Task SetsAriaLabelledByWhenLabelPresent();

    // Element reference
    Task ExposesElementReference();
}
