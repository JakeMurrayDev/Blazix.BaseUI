using System.Collections.Concurrent;

namespace Blazix.BaseUI.Docs.Client.Data;

public sealed record DemoFile(string Name, string Language, string ResourcePath)
{
    public string Code => DemoSources.GetCode(ResourcePath);
}

public sealed record DemoVariant(string Name, IReadOnlyList<DemoFile> Files);

public static class DemoSources
{
    private const string ResourcePrefix = "Blazix.BaseUI.Docs.Client.";

    private static readonly ConcurrentDictionary<string, string> Cache = new();

    public static string GetCode(string resourcePath) => Cache.GetOrAdd(resourcePath, static path =>
    {
        var assembly = typeof(DemoSources).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourcePrefix + path)
            ?? throw new InvalidOperationException($"Embedded demo source '{ResourcePrefix}{path}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Replace("\r\n", "\n").TrimEnd() + "\n";
    });
}
