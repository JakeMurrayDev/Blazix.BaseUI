namespace Blazix.BaseUI.Form;

/// <summary>
/// Provides data for the form submit event.
/// </summary>
/// <param name="Values">A dictionary of field names to their current values at the time of submission.</param>
public sealed record FormSubmitEventArgs(IReadOnlyDictionary<string, object?> Values);
