using System.IO.Enumeration;

namespace Blazix.BaseUI.Parity.Tests.Infrastructure;

/// <summary>Where the React reference comes from for one parity invocation.</summary>
public enum ParityReferenceMode
{
    /// <summary>Read a committed platform-exact baseline.</summary>
    Baseline,

    /// <summary>Capture the locally built React fixture bundle without writing.</summary>
    Live,

    /// <summary>Capture live and atomically replace the platform baseline.</summary>
    WriteBaseline
}

/// <summary>The strictly parsed parity invocation environment.</summary>
public sealed record ParityOptions
{
    /// <summary>Gets the React reference mode.</summary>
    public required ParityReferenceMode Mode { get; init; }

    /// <summary>Gets the diagnostic fixture glob, when supplied.</summary>
    public string? FixtureFilter { get; init; }

    /// <summary>Reads the process environment.</summary>
    /// <returns>The validated options.</returns>
    public static ParityOptions FromEnvironment()
        => FromEnvironment(Environment.GetEnvironmentVariable);

    /// <summary>Reads a supplied environment source for deterministic tests.</summary>
    /// <param name="read">Returns a variable value by name.</param>
    /// <returns>The validated options.</returns>
    public static ParityOptions FromEnvironment(Func<string, string?> read)
    {
        ArgumentNullException.ThrowIfNull(read);

        var live = Flag(read, "PARITY_LIVE");
        var write = Flag(read, "PARITY_WRITE_BASELINES");
        var filter = read("PARITY_FIXTURES");

        if (filter is not null && string.IsNullOrWhiteSpace(filter))
        {
            throw new FormatException("PARITY_FIXTURES must be unset or non-whitespace.");
        }

        return new ParityOptions
        {
            Mode = write
                ? ParityReferenceMode.WriteBaseline
                : live
                    ? ParityReferenceMode.Live
                    : ParityReferenceMode.Baseline,
            FixtureFilter = filter
        };
    }

    /// <summary>Matches one fixture id against the case-sensitive simple glob contract.</summary>
    internal static bool MatchesFixture(string fixture, string? filter)
        => filter is null || FileSystemName.MatchesSimpleExpression(
            filter,
            fixture,
            ignoreCase: false);

    private static bool Flag(Func<string, string?> read, string name)
    {
        var value = read(name);
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (string.Equals(value, "1", StringComparison.Ordinal))
        {
            return true;
        }

        throw new FormatException($"{name} must be unset or exactly '1', not '{value}'.");
    }
}
