namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>
/// Locates the gitignored base-ui checkout, which lives at the main repository
/// root and is therefore absent from git worktrees.
/// </summary>
public static class BaseUiLocator
{
    /// <summary>
    /// Locates the base-ui checkout.
    /// </summary>
    /// <returns>The absolute path, or <see langword="null"/> when not present.</returns>
    public static string? TryLocate()
    {
        var overridePath = Environment.GetEnvironmentVariable("PARITY_BASE_UI_PATH");
        if (!string.IsNullOrEmpty(overridePath))
        {
            return Directory.Exists(overridePath) ? overridePath : null;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        for (var i = 0; i < 12 && dir is not null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, ".base-ui");
            if (Directory.Exists(Path.Combine(candidate, "packages", "react")))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Locates the base-ui checkout, throwing when it is absent.
    /// </summary>
    /// <returns>The absolute path to the checkout.</returns>
    public static string Locate()
        => TryLocate() ?? throw new DirectoryNotFoundException(
            "Could not locate the .base-ui checkout. It is gitignored and lives at the main " +
            "repository root, so it is absent from git worktrees. Set PARITY_BASE_UI_PATH to " +
            "its absolute path, or clear PARITY_LIVE to use committed baselines instead.");
}
