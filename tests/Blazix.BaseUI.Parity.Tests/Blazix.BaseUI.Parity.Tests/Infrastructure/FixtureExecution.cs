namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>One manifest fixture executed under one declared color-scheme theme.</summary>
public sealed record FixtureExecution
{
    /// <summary>Gets the raw manifest fixture.</summary>
    public required FixtureEntry Fixture { get; init; }

    /// <summary>Gets the exact declared theme.</summary>
    public required string Theme { get; init; }

    /// <summary>Gets the six-field finding and waiver fixture identity.</summary>
    public string ExecutionId => $"{Fixture.Id}@{Theme}";

    /// <summary>Expands a manifest fixture in declared theme order.</summary>
    /// <param name="fixture">The fixture to expand.</param>
    /// <returns>One execution per declared theme.</returns>
    public static IReadOnlyList<FixtureExecution> Expand(FixtureEntry fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        return
        [
            .. fixture.Themes.Select(theme => new FixtureExecution
            {
                Fixture = fixture,
                Theme = theme
            })
        ];
    }

    /// <summary>Checks the exact schema-2 fixture-theme identity grammar.</summary>
    /// <param name="value">The candidate six-field fixture identity.</param>
    /// <returns>Whether it is one safe fixture id plus exactly one supported theme suffix.</returns>
    internal static bool IsExecutionId(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Count(character => character == '@') != 1)
        {
            return false;
        }

        var separator = value.IndexOf('@');
        return MilestoneFixtureCatalog.IsFixtureId(value[..separator]) &&
               value[(separator + 1)..] is "light" or "dark";
    }
}
