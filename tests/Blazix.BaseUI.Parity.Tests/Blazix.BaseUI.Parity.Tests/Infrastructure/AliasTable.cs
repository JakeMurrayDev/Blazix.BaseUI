using System.Text.Json;
using System.Text.RegularExpressions;

namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>
/// Expands manifest selector aliases such as <c>@trigger</c> into CSS selectors.
/// </summary>
public sealed partial class AliasTable
{
    private readonly IReadOnlyDictionary<string, Dictionary<string, string>> table;

    private AliasTable(IReadOnlyDictionary<string, Dictionary<string, string>> table)
    {
        this.table = table;
    }

    /// <summary>
    /// Loads the alias table from <c>manifest/aliases.json</c>.
    /// </summary>
    /// <returns>The loaded table.</returns>
    public static AliasTable Load()
    {
        var path = Path.Combine(ParityPaths.Manifest, "aliases.json");
        var json = File.ReadAllText(path);
        var parsed = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json)!;
        return new AliasTable(parsed);
    }

    /// <summary>
    /// Expands a selector for the supplied component.
    /// </summary>
    /// <param name="component">The component segment of the fixture id.</param>
    /// <param name="selector">A raw CSS selector, or an alias such as <c>@item(2)</c>.</param>
    /// <returns>The expanded CSS selector.</returns>
    public string Expand(string component, string selector)
    {
        if (!selector.StartsWith('@'))
        {
            return selector;
        }

        var match = AliasPattern().Match(selector);
        if (!match.Success)
        {
            throw new FormatException($"Malformed selector alias: {selector}");
        }

        var name = match.Groups["name"].Value;
        var index = match.Groups["index"].Success ? int.Parse(match.Groups["index"].Value) : 0;

        var resolved = Lookup(component, name)
            ?? throw new KeyNotFoundException($"No alias '{name}' for component '{component}'.");

        // Manifest indices are zero-based; Playwright's :nth-match() is one-based.
        return resolved.Replace("{n}", (index + 1).ToString());
    }

    private string? Lookup(string component, string name)
    {
        if (table.TryGetValue(component, out var scoped) && scoped.TryGetValue(name, out var value))
        {
            return value;
        }

        return table.TryGetValue("*", out var fallback) && fallback.TryGetValue(name, out var shared)
            ? shared
            : null;
    }

    [GeneratedRegex(@"^@(?<name>[a-z-]+)(\((?<index>\d+)\))?$")]
    private static partial Regex AliasPattern();
}
