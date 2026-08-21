namespace Blazix.BaseUI.Autocomplete;

internal sealed class AutocompleteGroupContext
{
    private readonly RegisteredIdOwner labelIdOwner = new();

    public string? LabelId { get; set; }
    public Action? StateChanged { get; set; }

    public void SetLabelId(object owner, string? labelId)
    {
        if (!labelIdOwner.ShouldApply(owner, labelId)) return;

        LabelId = labelId;
        StateChanged?.Invoke();
    }
}
