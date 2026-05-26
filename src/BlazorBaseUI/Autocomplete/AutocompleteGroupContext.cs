namespace BlazorBaseUI.Autocomplete;

internal sealed class AutocompleteGroupContext
{
    public string? LabelId { get; set; }
    public Action? StateChanged { get; set; }

    public void SetLabelId(string? labelId)
    {
        LabelId = labelId;
        StateChanged?.Invoke();
    }
}
