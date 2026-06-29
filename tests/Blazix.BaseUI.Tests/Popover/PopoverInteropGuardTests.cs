namespace Blazix.BaseUI.Tests.Popover;

public class PopoverInteropGuardTests
{
    [Fact]
    public async Task PopoverInteropCatchFiltersIncludeObjectDisposedException()
    {
        var popoverDirectory = GetRepositoryFile("src", "Blazix.BaseUI", "Popover");
        var sourceFiles = Directory.GetFiles(popoverDirectory, "*.razor");
        var offenders = new List<string>();

        foreach (var sourceFile in sourceFiles)
        {
            var source = await File.ReadAllTextAsync(sourceFile);
            if (source.Contains("catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)", StringComparison.Ordinal))
            {
                offenders.Add(Path.GetFileName(sourceFile));
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
