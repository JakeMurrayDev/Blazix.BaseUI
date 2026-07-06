namespace Blazix.BaseUI.Tests.Docs;

public class DocsComboboxDemoTests
{
    private const string ComboboxDemoDirectory = "docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Demos/Combobox";

    [Fact]
    public void ComboboxPage_IncludesEverySourceExample()
    {
        var page = ReadRepoFile("docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/ComboboxPage.razor");

        foreach (var heading in SourceExampleHeadings)
        {
            page.ShouldContain($"Title=\"{heading}\"");
        }

        foreach (var variant in SourceExampleVariants)
        {
            page.ShouldContain($"@{variant}");
        }
    }

    [Fact]
    public void ComboboxDemos_ApplyEmptyVisualSpacingToChildrenOnly()
    {
        var demoDirectory = Path.Combine(FindRepoRoot(), ComboboxDemoDirectory);

        foreach (var file in Directory.EnumerateFiles(demoDirectory, "*.razor", SearchOption.AllDirectories))
        {
            var contents = File.ReadAllText(file);
            contents.ShouldNotContain("<ComboboxEmpty class=");

            foreach (var emptyBlock in GetComboboxEmptyBlocks(contents))
            {
                emptyBlock.ShouldContain("<div class=");
            }
        }
    }

    [Fact]
    public void ComboboxDocs_ListEveryUpstreamExampleByName()
    {
        var docs = ReadRepoFile("docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs/Content/Components/combobox.md");

        foreach (var heading in SourceExampleHeadings)
        {
            docs.ShouldContain($"### {heading}");
        }
    }

    private static readonly string[] SourceExampleHeadings =
    [
        "Typed wrapper component",
        "Multiple select",
        "Input inside popup",
        "Grouped",
        "Async search (single)",
        "Async search (multiple)",
        "Creatable",
        "Virtualized",
        "Memoizing items",
    ];

    private static readonly string[] SourceExampleVariants =
    [
        "HeroVariants",
        "MultipleVariants",
        "InputInsidePopupVariants",
        "GroupedVariants",
        "AsyncSingleVariants",
        "AsyncMultipleVariants",
        "CreatableVariants",
        "VirtualizedVariants",
    ];

    private static string ReadRepoFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(FindRepoRoot(), relativePath));
    }

    private static IEnumerable<string> GetComboboxEmptyBlocks(string contents)
    {
        var searchIndex = 0;

        while (true)
        {
            var start = contents.IndexOf("<ComboboxEmpty", searchIndex, StringComparison.Ordinal);
            if (start < 0)
            {
                yield break;
            }

            var end = contents.IndexOf("</ComboboxEmpty>", start, StringComparison.Ordinal);
            end.ShouldBeGreaterThanOrEqualTo(0);

            yield return contents[start..end];

            searchIndex = end + "</ComboboxEmpty>".Length;
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Blazix.BaseUI.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return directory.FullName;
    }
}
