namespace Blazix.BaseUI.Combobox;

internal sealed class ComboboxChipsContext
{
    private readonly Dictionary<object, ChipIndexEntry> chipIndices = new();
    private int nextIndex;
    private int renderVersion;

    public void BeginRender()
    {
        renderVersion++;
        nextIndex = 0;
    }

    public int ResolveIndex(object chip)
    {
        if (chipIndices.TryGetValue(chip, out var entry) && entry.RenderVersion == renderVersion)
        {
            return entry.Index;
        }

        var index = nextIndex;
        nextIndex++;
        chipIndices[chip] = new ChipIndexEntry(index, renderVersion);
        return index;
    }

    public void Unregister(object chip)
    {
        chipIndices.Remove(chip);
    }

    private readonly record struct ChipIndexEntry(int Index, int RenderVersion);
}
