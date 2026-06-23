namespace Blazix.BaseUI.Utilities.LabelableProvider;

public sealed record LabelableContext(
    string? ControlId,
    Action<string?> SetControlId,
    string? LabelId,
    Action<string?> SetLabelId,
    List<string> MessageIds,
    Action<string, bool> UpdateMessageIds)
{
    public string? GetAriaDescribedBy() =>
        MessageIds.Count > 0 ? string.Join(" ", MessageIds) : null;
}
