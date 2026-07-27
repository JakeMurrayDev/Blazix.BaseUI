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

    /// <summary>Gets the built React bundle directory.</summary>
    public static string ReactDist => Path.Combine(HarnessRoot, "react-fixtures", "dist");

    /// <summary>Gets the shared capture script path.</summary>
    public static string SharedScript => Path.Combine(HarnessRoot, "shared", "capture.js");

    /// <summary>Gets the report output directory.</summary>
    public static string ReportDir { get; } =
        Environment.GetEnvironmentVariable("PARITY_REPORT_DIR")
        ?? Path.Combine(HarnessRoot, "parity-report");

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
