using System.Security.Cryptography;
using System.Text;

namespace Blazix.BaseUI.Parity.Tests.Report;

/// <summary>Paths written by one complete atomic report package.</summary>
/// <param name="Directory">The package directory.</param>
/// <param name="JsonPath">The canonical JSON report path.</param>
/// <param name="HtmlPath">The offline HTML report path.</param>
public sealed record ReportPackageResult(string Directory, string JsonPath, string HtmlPath);

/// <summary>Writes one canonical or filtered diagnostic report as an atomic directory package.</summary>
public sealed class ReportPackageWriter
{
    private const string JsonFileName = "parity-result.json";
    private const string HtmlFileName = "index.html";
    private const string CssFileName = "report.css";

    private readonly Action<string, bool> deleteDirectory;

    /// <summary>Creates the production package writer.</summary>
    public ReportPackageWriter()
        : this(Directory.Delete)
    {
    }

    internal ReportPackageWriter(Action<string, bool> deleteDirectory)
    {
        this.deleteDirectory = deleteDirectory ?? throw new ArgumentNullException(nameof(deleteDirectory));
    }

    /// <summary>Writes to the full canonical target or a filtered diagnostic child.</summary>
    /// <param name="model">The already evaluated report model.</param>
    /// <param name="canonicalDirectory">The full-run package directory.</param>
    /// <param name="diagnosticsDirectory">The parent for filtered packages.</param>
    /// <param name="diagnosticId">The safe filtered package id; null for a full run.</param>
    /// <returns>The written package paths.</returns>
    public ReportPackageResult Write(
        ReportModel model,
        string canonicalDirectory,
        string diagnosticsDirectory,
        string? diagnosticId)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(diagnosticsDirectory);

        var target = ResolveTarget(
            model.Scope.Filtered,
            canonicalDirectory,
            diagnosticsDirectory,
            diagnosticId);
        var json = JsonReportWriter.Render(model);
        var html = HtmlReportWriter.Render(model);
        var css = ReadCss();
        var artifacts = ValidateArtifacts(model.ArtifactSources);
        var targetParent = Path.GetDirectoryName(target)
            ?? throw new InvalidOperationException("Report package target has no parent directory.");
        var targetName = Path.GetFileName(target);
        var staging = Path.Combine(targetParent, $".{targetName}.{Guid.NewGuid():N}.tmp");
        var backup = Path.Combine(targetParent, $".{targetName}.{Guid.NewGuid():N}.bak");

        Directory.CreateDirectory(targetParent);
        try
        {
            Directory.CreateDirectory(staging);
            File.WriteAllBytes(Path.Combine(staging, JsonFileName), json);
            File.WriteAllText(Path.Combine(staging, HtmlFileName), html, new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(staging, CssFileName), css, new UTF8Encoding(false));

            foreach (var artifact in artifacts)
            {
                var destination = Path.Combine(
                    staging,
                    artifact.Artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(artifact.SourcePath, destination, overwrite: false);
            }

            Replace(staging, target, backup);
        }
        catch
        {
            if (Directory.Exists(staging))
            {
                Directory.Delete(staging, recursive: true);
            }

            throw;
        }

        return new ReportPackageResult(
            target,
            Path.Combine(target, JsonFileName),
            Path.Combine(target, HtmlFileName));
    }

    private static string ResolveTarget(
        bool filtered,
        string canonicalDirectory,
        string diagnosticsDirectory,
        string? diagnosticId)
    {
        if (!filtered)
        {
            if (diagnosticId is not null)
            {
                throw new InvalidOperationException(
                    "An unfiltered report cannot be written as a diagnostic package.");
            }

            return Path.GetFullPath(canonicalDirectory);
        }

        if (!IsSafeSegment(diagnosticId))
        {
            throw new InvalidOperationException(
                "A filtered report requires a safe lowercase diagnostic package id.");
        }

        var canonical = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(canonicalDirectory));
        var target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(
            Path.Combine(diagnosticsDirectory, diagnosticId!)));
        var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (string.Equals(canonical, target, comparison))
        {
            throw new InvalidOperationException(
                "A filtered report target cannot alias the canonical report package.");
        }

        return target;
    }

    private static IReadOnlyList<ReportArtifactSource> ValidateArtifacts(
        IReadOnlyList<ReportArtifactSource> sources)
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var source in sources)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(source.Artifact);
            ReportOutputSafety.ValidateRelativeArtifactPath(source.Artifact.RelativePath);
            if (!source.Artifact.RelativePath.StartsWith("assets/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Report artifacts must be stored below the local assets directory.");
            }

            if (!paths.Add(source.Artifact.RelativePath))
            {
                throw new InvalidOperationException(
                    $"Report artifact path '{source.Artifact.RelativePath}' is duplicated.");
            }

            if (!File.Exists(source.SourcePath))
            {
                throw new InvalidOperationException(
                    $"Report artifact '{source.Artifact.RelativePath}' is missing.");
            }

            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source.SourcePath)));
            if (!string.Equals(hash, source.Artifact.Sha256, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Report artifact '{source.Artifact.RelativePath}' failed integrity validation.");
            }

            if (!string.Equals(source.Artifact.MediaType, "image/png", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Report artifact '{source.Artifact.RelativePath}' has an unsupported media type.");
            }
        }

        return sources;
    }

    private static string ReadCss()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Report", CssFileName),
            Path.Combine(
                Infrastructure.ParityPaths.HarnessRoot,
                "Blazix.BaseUI.Parity.Tests",
                "Report",
                CssFileName)
        };
        var path = candidates.FirstOrDefault(File.Exists)
            ?? throw new InvalidOperationException("The local offline report stylesheet is missing.");
        var css = File.ReadAllText(path);
        if (css.Contains("@import", StringComparison.OrdinalIgnoreCase) ||
            css.Contains("url(", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The offline report stylesheet cannot import or reference external resources.");
        }

        return css;
    }

    private void Replace(string staging, string target, string backup)
    {
        var hadTarget = Directory.Exists(target);
        if (hadTarget)
        {
            Directory.Move(target, backup);
        }

        try
        {
            Directory.Move(staging, target);
            if (hadTarget)
            {
                deleteDirectory(backup, true);
            }
        }
        catch
        {
            if (hadTarget && Directory.Exists(backup))
            {
                if (Directory.Exists(target))
                {
                    Directory.Move(target, staging);
                }

                Directory.Move(backup, target);
                TryDeleteDirectory(staging);
            }
            else if (Directory.Exists(target))
            {
                TryDeleteDirectory(target);
            }

            throw;
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // The atomic target has already been restored. Cleanup is best-effort only.
        }
    }

    private static bool IsSafeSegment(string? value)
        => !string.IsNullOrEmpty(value) &&
           value[0] is >= 'a' and <= 'z' or >= '0' and <= '9' &&
           value[^1] is >= 'a' and <= 'z' or >= '0' and <= '9' &&
           !value.Contains("--", StringComparison.Ordinal) &&
           value.All(character =>
               character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');
}
