namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>
/// Resolves the harness's on-disk layout relative to the test assembly.
/// </summary>
public static class ParityPaths
{
    /// <summary>Gets the <c>tests/Blazix.BaseUI.Parity.Tests</c> directory.</summary>
    public static string HarnessRoot { get; } = ResolveHarnessRoot();

    /// <summary>Gets the manifest directory.</summary>
    public static string Manifest => Path.Combine(HarnessRoot, "manifest");

    /// <summary>Gets the committed baseline directory.</summary>
    public static string Baselines => Path.Combine(HarnessRoot, "baselines");

    /// <summary>Gets the waiver file path.</summary>
    public static string Waivers => Path.Combine(HarnessRoot, "waivers", "waivers.json");

    /// <summary>
    /// Gets the React bundle's build output <em>in the source tree</em>
    /// (<c>react-fixtures/dist</c>), which is what <c>pnpm parity:build</c> writes.
    /// This is not the copy the test host serves: the csproj copies this directory
    /// into the test output directory as <c>react-dist</c>, and the host mounts
    /// <c>Path.Combine(AppContext.BaseDirectory, "react-dist")</c> at <c>/react</c>.
    /// </summary>
    public static string ReactDist => Path.Combine(HarnessRoot, "react-fixtures", "dist");

    /// <summary>Gets the shared capture script path.</summary>
    public static string SharedScript => Path.Combine(HarnessRoot, "shared", "capture.js");

    /// <summary>Gets the report output directory.</summary>
    public static string ReportDir { get; } = ResolveReportDir();

    /// <summary>
    /// Gets the directory a run's screenshots and their diff overlays are written to.
    /// </summary>
    /// <remarks>
    /// Under <see cref="ReportDir"/> rather than beside <see cref="Baselines"/>: these are
    /// run output, not committed data, and putting them here gives them the
    /// <c>PARITY_REPORT_DIR</c> override and the existing <c>parity-report/</c> gitignore
    /// entry for free, so a fresh checkout needs no new directory and no new ignore rule.
    /// Nothing creates the directory on startup — the capturer creates it on its first
    /// write, and the comparator on its first diff.
    /// </remarks>
    public static string Screenshots => Path.Combine(ReportDir, "screenshots");

    private static string ResolveReportDir()
    {
        // On Unix an exported-but-empty variable reads back as "" rather than null,
        // which a bare `??` would accept and scatter reports into the process's
        // working directory.
        var overridePath = Environment.GetEnvironmentVariable("PARITY_REPORT_DIR");

        return !string.IsNullOrEmpty(overridePath)
            ? overridePath
            : Path.Combine(HarnessRoot, "parity-report");
    }

    private static string ResolveHarnessRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        for (var i = 0; i < 12 && dir is not null; i++, dir = dir.Parent)
        {
            if (dir.Name == "Blazix.BaseUI.Parity.Tests" &&
                Directory.Exists(Path.Combine(dir.FullName, "react-fixtures")))
            {
                return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not resolve the parity harness root from " + AppContext.BaseDirectory);
    }
}
