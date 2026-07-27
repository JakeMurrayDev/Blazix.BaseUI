namespace Blazix.BaseUI.Parity.Tests.Client;

/// <summary>
/// Resolves a fixture id such as <c>select/grouped</c> to its fixture component type.
/// </summary>
public static class FixtureRegistry
{
    private static readonly Dictionary<string, Type> Fixtures = BuildIndex();

    /// <summary>
    /// Gets every registered fixture id.
    /// </summary>
    public static IReadOnlyCollection<string> Ids => Fixtures.Keys;

    /// <summary>
    /// Resolves the fixture component for the supplied id segments.
    /// </summary>
    /// <param name="component">The component segment, for example <c>select</c>.</param>
    /// <param name="demo">The demo segment, for example <c>grouped</c>.</param>
    /// <returns>The fixture component type, or <see langword="null"/> when unknown.</returns>
    public static Type? Resolve(string component, string demo)
        => Fixtures.GetValueOrDefault($"{component}/{demo}");

    private static Dictionary<string, Type> BuildIndex()
    {
        var index = new Dictionary<string, Type>(StringComparer.Ordinal);
        var prefix = $"{typeof(FixtureRegistry).Namespace}.Fixtures.";

        foreach (var type in typeof(FixtureRegistry).Assembly.GetTypes())
        {
            if (type.FullName is null || !type.FullName.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            // Fixtures.Select.Grouped -> select/grouped
            var segments = type.FullName[prefix.Length..].Split('.');
            if (segments.Length != 2)
            {
                continue;
            }

            index[$"{ToKebab(segments[0])}/{ToKebab(segments[1])}"] = type;
        }

        return index;
    }

    private static string ToKebab(string pascal)
    {
        var builder = new System.Text.StringBuilder(pascal.Length + 4);

        for (var i = 0; i < pascal.Length; i++)
        {
            if (i > 0 && char.IsUpper(pascal[i]))
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(pascal[i]));
        }

        return builder.ToString();
    }
}
