using System.Text.RegularExpressions;
using Blazix.BaseUI.Parity.Tests.Infrastructure;
using Shouldly;

namespace Blazix.BaseUI.Parity.Tests.Tests.HarnessTests;

/// <summary>
/// Pins the Task 16 floating, toast, menu, and menubar Razor ports to their vendored
/// React Base UI Tailwind demos and published manifest contracts.
/// </summary>
public sealed partial class Task16FloatingMenuFixtureContractTests
{
    private static readonly FixtureSource[] ExpectedFixtures =
    [
        new("dialog/hero", "dialog/demos/hero/tailwind/index.tsx", "Dialog/Hero"),
        new("drawer/hero", "drawer/demos/hero/tailwind/index.tsx", "Drawer/Hero"),
        new("toast/hero", "toast/demos/hero/tailwind/index.tsx", "Toast/Hero"),
        new("tooltip/hero", "tooltip/demos/hero/tailwind/index.tsx", "Tooltip/Hero"),
        new("preview-card/hero", "preview-card/demos/hero/tailwind/index.tsx", "PreviewCard/Hero"),
        new("menu/arrow", "menu/demos/arrow/tailwind/index.tsx", "Menu/Arrow"),
        new("menu/checkbox-items", "menu/demos/checkbox-items/tailwind/index.tsx", "Menu/CheckboxItems"),
        new("menubar/hero", "menubar/demos/hero/tailwind/index.tsx", "Menubar/Hero")
    ];

    [Fact]
    public void AuthorsEveryAssignedFixtureAtTheExactRegistryConventionPath()
    {
        foreach (var fixture in ExpectedFixtures)
        {
            File.Exists(ReactSourcePath(fixture)).ShouldBeTrue($"Pinned React source missing for {fixture.Id}");
            File.Exists(RazorSourcePath(fixture)).ShouldBeTrue($"Razor fixture missing for {fixture.Id}");
        }
    }

    [Fact]
    public void PreservesTheExactUpstreamTailwindClassDefinitions()
    {
        foreach (var fixture in ExpectedFixtures)
        {
            var react = File.ReadAllText(ReactSourcePath(fixture));
            var razor = File.ReadAllText(RazorSourcePath(fixture));

            ExtractReactClasses(react).ShouldBe(
                ExtractRazorClasses(razor),
                $"Tailwind class drift for {fixture.Id}");
        }
    }

    [Fact]
    public void PreservesTheExactInteractivePartAndTextSurface()
    {
        AssertContains("dialog/hero", "<DialogRoot>", "<DialogTrigger", "View notifications",
            "<DialogBackdrop", "<DialogPopup", "<DialogTitle", "Notifications",
            "<DialogDescription", "You are all caught up. Good job!", "<DialogClose", "Close");

        AssertContains("drawer/hero", "<DrawerRoot SwipeDirection=\"DrawerSwipeDirection.Right\">",
            "<DrawerTrigger", "Open drawer", "<DrawerBackdrop", "<DrawerViewport", "<DrawerPopup",
            "<DrawerContent", "<DrawerTitle", "Drawer", "<DrawerDescription",
            "This is a drawer that slides in from the side. You can swipe to dismiss it.",
            "<DrawerClose", "Close");

        AssertContains("toast/hero", "<ToastProvider", "<ToastPortal>", "<ToastViewport",
            "@foreach (var toast in context.Toasts)", "<ToastRoot", "<ToastContent",
            "<ToastTitle", "<ToastDescription", "<ToastClose", "Dismiss", "Create toast",
            "Title = $\"Toast {count} created\"", "Description = \"This is a toast notification.\"");

        AssertContains("tooltip/hero", "<TooltipProvider>", "aria-label=\"Bold\"",
            "aria-label=\"Italic\"", "aria-label=\"Underline\"", "<TooltipPositioner SideOffset=\"11\">",
            "<TooltipPopup", "<TooltipArrow");
        CountOccurrences(ReadRazor("tooltip/hero"), "<TooltipRoot>").ShouldBe(3);

        AssertContains("preview-card/hero", "<PreviewCardRoot>", "The principles of good",
            "href=\"https://en.wikipedia.org/wiki/Typography\"", "typography",
            "remain in the digital age.", "<PreviewCardPositioner SideOffset=\"8\">",
            "<PreviewCardPopup", "<PreviewCardArrow", "width=\"224\"", "height=\"150\"",
            "Station Hofplein signage in Rotterdam, Netherlands");

        AssertContains("menu/arrow", "<MenuRoot", "<MenuTrigger", "Song", "<MenuPositioner",
            "SideOffsetFunction=\"ResolveSideOffset\"", "data.Side == \"top\" ? 12 : 8",
            "<MenuPopup", "<MenuArrow", "Add to Library", "Add to Playlist", "Play Next",
            "Play Last", "Favorite", "Share");
        CountOccurrences(ReadRazor("menu/arrow"), "<Blazix.BaseUI.Separator.Separator").ShouldBe(2);

        AssertContains("menu/checkbox-items", "<MenuRoot", "<MenuTrigger", "Workspace",
            "<MenuCheckboxItem", "<MenuCheckboxItemIndicator", "Minimap", "Search", "Sidebar",
            "private bool showMinimap = true;", "private bool showSearch = true;",
            "private bool showSidebar;");
        CountOccurrences(ReadRazor("menu/checkbox-items"), "<MenuCheckboxItem ").ShouldBe(3);

        AssertContains("menubar/hero", "<MenuBarRoot", "File", "Edit", "View", "Help",
            "Export", "PDF", "PNG", "SVG", "Print", "Cut", "Copy", "Paste", "Zoom In",
            "Zoom Out", "Layout", "Single Page", "Two Pages", "Continuous", "Full Screen");
        CountOccurrences(ReadRazor("menubar/hero"), "<MenuRoot").ShouldBe(4);
        CountOccurrences(ReadRazor("menubar/hero"), "<MenuSubmenuRoot>").ShouldBe(2);
    }

    [Fact]
    public void MatchesTheExactPublishedManifestEntries()
    {
        Task16ManifestContract.AssertExactEntries(
        [
            new("dialog/hero", "dialog/demos/hero/tailwind/index.tsx", "Dialog/Hero",
                "initial|render|",
                "open|animation|click:@trigger=>@popup[state:visible],@trigger[attribute:aria-expanded=true]",
                "close|animation|click:[role='dialog'] button=>@popup[state:detached],@trigger[attribute:aria-expanded=false]"),
            new("drawer/hero", "drawer/demos/hero/tailwind/index.tsx", "Drawer/Hero",
                "initial|render|",
                "open|animation|click:@trigger=>@popup[state:visible],@trigger[attribute:aria-expanded=true]",
                "close|animation|click:[role='dialog'] button=>@popup[state:detached],@trigger[attribute:aria-expanded=false]"),
            new("toast/hero", "toast/demos/hero/tailwind/index.tsx", "Toast/Hero",
                "initial|render|",
                "create-toast|animation|click:button[type='button']=>[role='dialog'][state:visible],[role='dialog'] h2[property:innerText=Toast 1 created]",
                "dismiss-toast|animation|click:[role='dialog'] button=>[role='dialog'][state:detached]"),
            new("tooltip/hero", "tooltip/demos/hero/tailwind/index.tsx", "Tooltip/Hero",
                "initial|render|",
                "show-bold-tooltip|animation|hover:button[aria-label='Bold']=>[role='presentation'] > [data-open][state:visible],[role='presentation'] > [data-open][property:innerText=Bold],button[aria-label='Bold'][attribute:data-popup-open=]",
                "dismiss-tooltip|animation|key:Escape=>[role='presentation'] > [data-open][state:detached]"),
            new("preview-card/hero", "preview-card/demos/hero/tailwind/index.tsx", "PreviewCard/Hero",
                "initial|render|",
                "show-preview|animation|hover:a[href='https://en.wikipedia.org/wiki/Typography']=>img[alt='Station Hofplein signage in Rotterdam, Netherlands'][state:visible],a[href='https://en.wikipedia.org/wiki/Typography'][attribute:data-popup-open=]",
                "dismiss-preview|animation|key:Escape=>img[alt='Station Hofplein signage in Rotterdam, Netherlands'][state:detached]"),
            new("menu/arrow", "menu/demos/arrow/tailwind/index.tsx", "Menu/Arrow",
                "initial|render|",
                "open|animation|click:@trigger=>@popup[state:visible],@trigger[attribute:aria-expanded=true]",
                "arrow-down|render|key:ArrowDown=>@item(0)[focus:equals]",
                "close|animation|key:Escape=>@popup[state:detached],@trigger[attribute:aria-expanded=false]"),
            new("menu/checkbox-items", "menu/demos/checkbox-items/tailwind/index.tsx", "Menu/CheckboxItems",
                "initial|render|",
                "open|animation|click:@trigger=>@popup[state:visible],@trigger[attribute:aria-expanded=true]",
                "toggle-minimap|render|click:[role='menuitemcheckbox']:first-of-type=>[role='menuitemcheckbox']:first-of-type[attribute:aria-checked=false],@popup[state:visible]",
                "close|animation|key:Escape=>@popup[state:detached],@trigger[attribute:aria-expanded=false]"),
            new("menubar/hero", "menubar/demos/hero/tailwind/index.tsx", "Menubar/Hero",
                "initial|render|",
                "open-file|animation|click:[data-parity-root] > div > button:nth-of-type(1)=>[data-parity-root] > div > button:nth-of-type(1)[attribute:aria-expanded=true],[role='menu'][data-open][state:visible]",
                "switch-to-edit|animation|hover:[data-parity-root] > div > button:nth-of-type(2)=>[data-parity-root] > div > button:nth-of-type(1)[attribute:aria-expanded=false],[data-parity-root] > div > button:nth-of-type(2)[attribute:aria-expanded=true],[role='menu'][data-open][state:visible]",
                "close|animation|key:Escape=>[role='menu'][data-open][state:detached],[data-parity-root] > div > button:nth-of-type(2)[attribute:aria-expanded=false]")
        ]);
    }

    private static void AssertContains(string id, params string[] fragments)
    {
        var source = ReadRazor(id);
        foreach (var fragment in fragments)
        {
            source.ShouldContain(fragment);
        }
    }

    private static string ReadRazor(string id)
        => File.ReadAllText(RazorSourcePath(ExpectedFixtures.Single(fixture => fixture.Id == id)));

    private static int CountOccurrences(string source, string value)
        => Regex.Matches(source, Regex.Escape(value), RegexOptions.CultureInvariant).Count;

    private static string[] ExtractReactClasses(string source)
    {
        var constants = ReactSingleQuotedClassConstantRegex().Matches(source)
            .Concat(ReactDoubleQuotedClassConstantRegex().Matches(source))
            .ToDictionary(
                match => match.Groups["name"].Value,
                match => match.Groups["value"].Value,
                StringComparer.Ordinal);

        return ReactClassRegex().Matches(source)
            .Select(match => match.Groups["value"].Value)
            .Concat(ReactClassReferenceRegex().Matches(source)
                .Select(match => constants[match.Groups["name"].Value]))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] ExtractRazorClasses(string source)
    {
        var constants = RazorClassConstantRegex().Matches(source)
            .ToDictionary(
                match => match.Groups["name"].Value,
                match => match.Groups["value"].Value,
                StringComparer.Ordinal);

        return RazorClassRegex().Matches(source)
            .Select(match => match.Groups["value"].Value)
            .Concat(RazorClassReferenceRegex().Matches(source)
                .Select(match => constants[match.Groups["name"].Value]))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static string ReactSourcePath(FixtureSource fixture)
        => Path.Combine(
            RepositoryRoot(), ".base-ui", "docs", "src", "app", "(docs)", "react", "components",
            fixture.React.Replace('/', Path.DirectorySeparatorChar));

    private static string RazorSourcePath(FixtureSource fixture)
        => Path.Combine(
            ParityPaths.HarnessRoot, "Blazix.BaseUI.Parity.Tests.Client", "Fixtures",
            fixture.Blazor.Replace('/', Path.DirectorySeparatorChar) + ".razor");

    private static string RepositoryRoot()
        => Path.GetFullPath(Path.Combine(ParityPaths.HarnessRoot, "..", ".."));

    [GeneratedRegex("className=\\\"(?<value>[^\\\"]+)\\\"")]
    private static partial Regex ReactClassRegex();

    [GeneratedRegex("const\\s+(?<name>\\w*[Cc]lass(?:Name|es)?)\\s*=\\s*'(?<value>[^']+)'")]
    private static partial Regex ReactSingleQuotedClassConstantRegex();

    [GeneratedRegex("const\\s+(?<name>\\w*[Cc]lass(?:Name|es)?)\\s*=\\s*\\\"(?<value>[^\\\"]+)\\\"")]
    private static partial Regex ReactDoubleQuotedClassConstantRegex();

    [GeneratedRegex("className=\\{(?<name>\\w*[Cc]lass(?:Name|es)?)\\}")]
    private static partial Regex ReactClassReferenceRegex();

    [GeneratedRegex("\\bclass=\\\"(?!@)(?<value>[^\\\"]+)\\\"")]
    private static partial Regex RazorClassRegex();

    [GeneratedRegex("const\\s+string\\s+(?<name>\\w*[Cc]lass(?:Name|es)?)\\s*=\\s*\\\"(?<value>[^\\\"]+)\\\"")]
    private static partial Regex RazorClassConstantRegex();

    [GeneratedRegex("\\bclass=\\\"@(?<name>\\w*[Cc]lass(?:Name|es)?)\\\"")]
    private static partial Regex RazorClassReferenceRegex();

    private sealed record FixtureSource(string Id, string React, string Blazor);
}
