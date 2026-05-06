namespace BlazorBaseUI.Tests.Contracts.Select;

public interface ISelectGroupLabelContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // ID and association
    Task GeneratesIdAutomatically();
    Task UsesProvidedId();
    Task AssociatesGeneratedIdWithGroupAriaLabelledBy();
    Task AssociatesProvidedIdWithGroupAriaLabelledBy();

    // Element reference
    Task ExposesElementReference();

    // Context validation
    Task ThrowsWhenNotInsideSelectGroup();

    // Cleanup
    Task CleansUpLabelIdOnDispose();
}
