namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>
/// Locates the gitignored base-ui checkout, which lives at the main repository
/// root and is therefore absent from git worktrees.
/// </summary>
public static class BaseUiLocator
{
    /// <summary>The directory that distinguishes a base-ui checkout from any other directory.</summary>
    private const string MarkerSubPath = "packages/react";

    /// <summary>
    /// Locates the base-ui checkout.
    /// </summary>
    /// <returns>The absolute path, or <see langword="null"/> when not present.</returns>
    public static string? TryLocate()
    {
        var overridePath = Environment.GetEnvironmentVariable("PARITY_BASE_UI_PATH");
        if (!string.IsNullOrEmpty(overridePath))
        {
            // An override is held to the same standard as the walk-up: merely
            // existing is not enough, or a wrong path is accepted here and only
            // surfaces later as an unrelated failure.
            return IsCheckout(overridePath) ? overridePath : null;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        for (var i = 0; i < 12 && dir is not null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, ".base-ui");
            if (IsCheckout(candidate))
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
    {
        var located = TryLocate();
        if (located is not null)
        {
            return located;
        }

        var overridePath = Environment.GetEnvironmentVariable("PARITY_BASE_UI_PATH");
        if (!string.IsNullOrEmpty(overridePath))
        {
            // Telling the operator to set the variable they just set sends them
            // looking in the wrong place; name the path and what was missing.
            var reason = Directory.Exists(overridePath)
                ? $"it has no '{MarkerSubPath}' directory"
                : "no such directory exists";

            throw new DirectoryNotFoundException(
                $"PARITY_BASE_UI_PATH is set to '{overridePath}', which is not a base-ui " +
                $"checkout: {reason}. Point it at the checkout root, or clear it to fall back " +
                "to searching for .base-ui at the main repository root.");
        }

        throw new DirectoryNotFoundException(
            "Could not locate the .base-ui checkout. It is gitignored and lives at the main " +
            "repository root, so it is absent from git worktrees. Set PARITY_BASE_UI_PATH to " +
            "its absolute path, or clear PARITY_LIVE to use committed baselines instead.");
    }

    private static bool IsCheckout(string path)
        => Directory.Exists(Path.Combine(path, MarkerSubPath));
}
