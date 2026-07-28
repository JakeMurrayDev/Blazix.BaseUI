using System.Globalization;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// The wording every comparator builds its finding messages from.
/// </summary>
/// <remarks>
/// Shared rather than repeated per comparator so that the sentences a reporter or a waiver
/// rule may later classify on exist as one literal. Four copies of the relaxed-pairing note
/// is four chances for one of them to drift and silently stop matching. It holds text and
/// nothing else — it knows no finding kind and reads no capture — so each comparator still
/// owns exactly one kind and can be reviewed and rejected on its own.
/// </remarks>
public static class FindingText
{
    /// <summary>
    /// Renders one side of a comparison, naming an absence rather than showing it as an
    /// empty value.
    /// </summary>
    /// <param name="present">Whether that leg captured the value at all.</param>
    /// <param name="value">The captured value, read only when <paramref name="present"/>.</param>
    /// <returns>The quoted value, or <c>absent</c>.</returns>
    public static string Describe(bool present, string? value)
        => present ? $"'{value}'" : "absent";

    /// <summary>
    /// Renders one side of a numeric comparison, invariantly, so a message written on one
    /// machine reads the same on another whose decimal separator differs.
    /// </summary>
    /// <param name="present">Whether that leg captured the value at all.</param>
    /// <param name="value">The captured value, read only when <paramref name="present"/>.</param>
    /// <returns>The quoted value, or <c>absent</c>.</returns>
    public static string Describe(bool present, double value)
        => present ? $"'{value.ToString(CultureInfo.InvariantCulture)}'" : "absent";

    /// <summary>
    /// Says so when the two nodes only matched on their tag.
    /// </summary>
    /// <remarks>
    /// Such a pair is still compared — suppressing it would lose a real difference beneath
    /// an identity difference the structural finding does not describe — but a reader has to
    /// be told that the two operands may not correspond.
    /// </remarks>
    /// <param name="pair">The matched pair the finding was read from.</param>
    /// <returns>The note, or an empty string when the pair matched on its full identity.</returns>
    public static string RelaxedNote(NodePair pair)
        => pair.Relaxed
            ? " The two nodes were paired on tag alone, so they may not be the same element."
            : string.Empty;
}
