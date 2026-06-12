namespace Blazix.BaseUI.Docs.Client.Services;

public sealed class TocEntry(string id, string title, int level)
{
    public string Id { get; } = id;

    public string Title { get; } = title;

    public int Level { get; } = level;
}

public sealed class TocContext
{
    private readonly List<TocEntry> entries = [];

    public IReadOnlyList<TocEntry> Entries => entries;

    public event Action? Changed;

    public void Register(TocEntry entry)
    {
        entries.Add(entry);
        Changed?.Invoke();
    }

    public void Unregister(TocEntry entry)
    {
        if (entries.Remove(entry))
        {
            Changed?.Invoke();
        }
    }

    public void Clear()
    {
        if (entries.Count == 0)
        {
            return;
        }

        entries.Clear();
        Changed?.Invoke();
    }
}
