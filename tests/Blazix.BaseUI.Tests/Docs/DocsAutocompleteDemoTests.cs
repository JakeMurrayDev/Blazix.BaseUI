namespace Blazix.BaseUI.Tests.Docs;

public class DocsAutocompleteDemoTests
{
    [Fact]
    public void AutocompletePage_IncludesEverySourceExample()
    {
        var page = ReadRepoFile("docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Pages/AutocompletePage.razor");

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
    public void AutocompleteDemos_ApplyEmptyVisualSpacingToChildrenOnly()
    {
        var root = FindRepoRoot();
        var demoDirectory = Path.Combine(
            root,
            "docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Demos/Autocomplete");

        foreach (var file in Directory.EnumerateFiles(demoDirectory, "*.razor", SearchOption.AllDirectories))
        {
            var contents = File.ReadAllText(file);
            contents.ShouldNotContain("<AutocompleteEmpty class=");
        }
    }

    [Fact]
    public void AutocompleteVirtualizedDemo_UsesTotalSizeForStableInitialScrollerHeight()
    {
        var demo = ReadRepoFile("docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/Components/Demos/Autocomplete/Virtualized/Css/AutocompleteVirtualizedCss.razor");
        var css = ReadRepoFile("docs/Blazix.BaseUI.Docs/Blazix.BaseUI.Docs.Client/wwwroot/demos/autocomplete.css");

        demo.ShouldContain("style=\"@VirtualizedListStyle\"");
        demo.ShouldContain("ItemSize=\"@VirtualizedItemSize\"");
        demo.ShouldContain("Index=\"@entry.Index\"");
        demo.ShouldContain("aria-posinset=\"@(entry.Index + 1)\"");
        demo.ShouldNotContain("IndexOf(item)");
        css.ShouldContain("height: min(22.5rem, var(--total-size));");
        css.ShouldContain("max-height: var(--available-height, 22.5rem);");
        css.ShouldNotContain("height: min(22.5rem, var(--available-height));");
    }

    private static readonly string[] SourceExampleHeadings =
    [
        "Async search",
        "Inline autocomplete",
        "Grouped",
        "Fuzzy matching",
        "Limit results",
        "Auto highlight",
        "Command palette",
        "Grid layout",
        "Virtualized",
    ];

    private static readonly string[] SourceExampleVariants =
    [
        "AsyncVariants",
        "InlineVariants",
        "GroupedVariants",
        "FuzzyMatchingVariants",
        "LimitVariants",
        "AutoHighlightVariants",
        "CommandPaletteVariants",
        "GridVariants",
        "VirtualizedVariants",
    ];

    private static string ReadRepoFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(FindRepoRoot(), relativePath));
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
