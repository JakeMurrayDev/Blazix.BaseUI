namespace BlazorBaseUI.Tests.Contracts.Progress;

public interface IProgressLabelContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasRolePresentation();

    // ID generation
    Task GeneratesAutoId();
    Task UsesProvidedIdFromAdditionalAttributes();

    // Label-root association
    Task NotifiesParentOfLabelId();
    Task UpdatesParentWhenIdChanges();
    Task CleansUpLabelIdOnDispose();

    // Data attributes
    Task HasDataStatusAttribute();

    // Context
    Task ThrowsWhenRenderedWithoutRoot();
}
