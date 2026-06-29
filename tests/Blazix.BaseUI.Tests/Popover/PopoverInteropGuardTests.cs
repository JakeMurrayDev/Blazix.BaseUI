using System.Text.RegularExpressions;

namespace Blazix.BaseUI.Tests.Popover;

public class PopoverInteropGuardTests
{
    // Matches any `catch (...) when (<filter>)` clause so the guard can be asserted
    // semantically on the filter contents instead of on one verbatim clause string
    // (which a reorder or reformat would slip past).
    private static readonly Regex CatchFilterPattern = new(
        @"catch\s*\([^)]*\)\s*when\s*\((?<filter>[^)]*)\)",
        RegexOptions.Singleline | RegexOptions.Compiled);

    [Fact]
    public async Task PopoverInteropCatchFiltersIncludeObjectDisposedException()
    {
        var popoverDirectory = GetRepositoryFile("src", "Blazix.BaseUI", "Popover");
        var sourceFiles = Directory.GetFiles(popoverDirectory, "*.razor");
        var offenders = new List<string>();

        foreach (var sourceFile in sourceFiles)
        {
            var source = await File.ReadAllTextAsync(sourceFile);

            foreach (Match match in CatchFilterPattern.Matches(source))
            {
                var filter = match.Groups["filter"].Value;

                // A teardown guard that catches the JS-interop disconnect exceptions must
                // also tolerate ObjectDisposedException, regardless of type order/format.
                if (filter.Contains("JSDisconnectedException", StringComparison.Ordinal)
                    && !filter.Contains("ObjectDisposedException", StringComparison.Ordinal))
                {
                    offenders.Add(Path.GetFileName(sourceFile));
                    break;
                }
            }
        }

        offenders.ShouldBeEmpty("Popover JS interop teardown guards must also catch ObjectDisposedException.");
    }

    [Fact]
    public async Task PopoverModalTouchScrollLockUsesViewportWidthTolerance()
    {
        var sourceFile = GetRepositoryFile("src", "Blazix.BaseUI", "wwwroot", "blazix-baseui-popover.js");
        var source = await File.ReadAllTextAsync(sourceFile);

        source.ShouldContain("const VIEWPORT_WIDTH_TOLERANCE_PX = 20;");
        source.ShouldContain("positionerWidth >= viewportWidth - VIEWPORT_WIDTH_TOLERANCE_PX");
        source.ShouldNotContain("rootState.interactionType !== 'touch' && !rootState.releaseScrollLock");
    }

    private static string GetRepositoryFile(params string[] pathSegments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Blazix.BaseUI.slnx")))
            {
                var parts = new string[pathSegments.Length + 1];
                parts[0] = directory.FullName;
                Array.Copy(pathSegments, 0, parts, 1, pathSegments.Length);

                return Path.Combine(parts);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate the Blazix.BaseUI repository root.");
    }
}
