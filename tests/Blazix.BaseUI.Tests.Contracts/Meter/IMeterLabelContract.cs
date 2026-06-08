namespace Blazix.BaseUI.Tests.Contracts.Meter;

public interface IMeterLabelContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasRolePresentation();
    Task ThrowsWhenRenderedOutsideRoot();

    // ID generation
    Task GeneratesAutoId();
    Task UsesProvidedIdFromAdditionalAttributes();

    // Label-root association
    Task NotifiesParentOfLabelId();
    Task UpdatesParentWhenIdChanges();
    Task CleansUpLabelIdOnDispose();
}
