namespace Blazix.BaseUI.Tests.Contracts.Meter;

public interface IMeterValueContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // ARIA
    Task HasAriaHidden();
    Task AdditionalAriaHiddenOverridesDefault();
    Task ThrowsWhenRenderedOutsideRoot();

    // Content rendering
    Task RendersFormattedValueWhenNoChildContent();
    Task RendersCustomFormattedValue();
    Task ChildContentReceivesFormattedValueAndNumber();
}
