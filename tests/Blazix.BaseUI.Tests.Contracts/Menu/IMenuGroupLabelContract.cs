namespace Blazix.BaseUI.Tests.Contracts.Menu;

public interface IMenuGroupLabelContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // ARIA
    Task HasRolePresentation();
    Task GeneratesIdAutomatically();
    Task UsesProvidedId();
    Task AssociatesGeneratedIdWithGroupAriaLabelledBy();
    Task AssociatesProvidedIdWithGroupAriaLabelledBy();

    // Element reference
    Task ExposesElementReference();
}
