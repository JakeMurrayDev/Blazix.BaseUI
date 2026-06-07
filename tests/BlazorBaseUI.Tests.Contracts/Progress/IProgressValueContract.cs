namespace BlazorBaseUI.Tests.Contracts.Progress;

public interface IProgressValueContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // ARIA
    Task HasAriaHidden();

    // Content rendering
    Task RendersFormattedValueWhenNoChildContent();
    Task RendersCustomFormattedValue();
    Task ChildContentReceivesFormattedValueAndNumber();
    Task ChildContentReceivesIndeterminateAndNull();
    Task RendersNothingForIndeterminateWhenNoChildContent();

    // Data attributes
    Task HasDataStatusAttribute();

    // Context
    Task ThrowsWhenRenderedWithoutRoot();
}
