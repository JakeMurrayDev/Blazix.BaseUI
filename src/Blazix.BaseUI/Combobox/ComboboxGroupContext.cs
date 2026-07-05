namespace Blazix.BaseUI.Combobox;

internal sealed class ComboboxGroupContext
{
    public string? LabelId { get; set; }
    public Action? StateChanged { get; set; }

    public void SetLabelId(string? labelId)
    {
        LabelId = labelId;
        StateChanged?.Invoke();
    }
}
