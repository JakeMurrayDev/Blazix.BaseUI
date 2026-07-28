using System.Globalization;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Compares two CSS values, allowing the lengths inside them to differ slightly.
/// </summary>
/// <remarks>
/// <para>
/// The two legs lay out with the same engine but not from the same box tree, so the last
/// decimal of a length routinely differs by a fraction of a pixel. Comparing the strings
/// ordinally reports that as a parity break on nearly every element; rounding the whole
/// value instead cannot be done without knowing which property it belongs to.
/// </para>
/// <para>
/// Splitting the value into numeric and non-numeric runs and comparing them pairwise
/// handles <c>10px</c>, <c>rgb(...)</c>, <c>matrix(...)</c>, and <c>cubic-bezier(...)</c>
/// with one rule and no per-property table. The non-numeric runs must be ordinally equal,
/// so a keyword difference is never absorbed.
/// </para>
/// <para>
/// The epsilon reaches only the numeric runs that carry a length, because only those carry
/// the noise it exists to absorb. Most of the allowlist in <c>shared/capture.js</c> is
/// numbers that are not lengths, and half a unit of one of those is a plain difference in
/// rendering: <c>opacity</c> <c>1</c> against <c>0.6</c>, <c>animation-duration</c>
/// <c>0s</c> against <c>0.5s</c>, a <c>line-height</c> ratio, an <c>rgba</c> alpha, a
/// <c>z-index</c>, a <c>flex-grow</c>, a flex fraction, and the scale and skew arguments of
/// a transform matrix. Those are all compared exactly.
/// </para>
/// <para>
/// A run carries a length when it is written with the <c>px</c> or <c>%</c> unit, or when
/// it is a translation argument of <c>matrix()</c> or <c>matrix3d()</c> — the one place a
/// computed value writes a pixel length with the unit left off. Both units are measured
/// rather than authored: a computed style resolves lengths to pixels, and the select popup
/// publishes <c>--transform-origin</c> as a percentage it derives from the measured offset
/// of the selected item.
/// </para>
/// <para>
/// <c>rem</c> and <c>em</c> are deliberately not lengths for this purpose, and neither is
/// any other unit. A computed style resolves font-relative lengths to pixels, so the only
/// ones a capture holds are custom properties a demo's own stylesheet declares —
/// <c>--bleed: 3rem</c>, say — which both legs read from the same file and which therefore
/// have no noise to absorb, while half a rem is eight pixels of real difference. An
/// unrecognised unit compares exactly, so a value this class does not understand is
/// reported rather than waved through.
/// </para>
/// </remarks>
public static class ValueTolerance
{
    /// <summary>
    /// The unit suffixes that mark a numeric run as a length the browser measured.
    /// </summary>
    private static readonly string[] LengthUnits = ["px", "%"];

    /// <summary>
    /// The zero-based arguments of <c>matrix(a, b, c, d, tx, ty)</c> that are translations.
    /// The other four are a scale and skew, which are ratios and not lengths.
    /// </summary>
    private static readonly int[] MatrixTranslations = [4, 5];

    /// <summary>
    /// The zero-based arguments of the column-major <c>matrix3d()</c> that are translations:
    /// <c>m41</c>, <c>m42</c>, and <c>m43</c>. base-ui writes these through
    /// <c>translate3d</c> for the scroll-area thumb and the toast stack.
    /// </summary>
    private static readonly int[] Matrix3dTranslations = [12, 13, 14];

    /// <summary>
    /// Reports whether two values agree, treating lengths no further apart than
    /// <paramref name="epsilon"/> as equal.
    /// </summary>
    /// <param name="reference">The React value.</param>
    /// <param name="candidate">The Blazor value.</param>
    /// <param name="epsilon">The largest difference two lengths may have and still agree.</param>
    /// <returns>
    /// <see langword="true"/> when both values hold the same sequence of runs, every
    /// non-numeric run is ordinally equal, every numeric run that carries a length differs
    /// by at most <paramref name="epsilon"/>, and every other numeric run is equal.
    /// </returns>
    public static bool Equivalent(string reference, string candidate, double epsilon)
    {
        var referenceTokens = Tokenize(reference);
        var candidateTokens = Tokenize(candidate);

        // A different number of runs is a different value however close the numbers in it
        // are. Usually the run-by-run comparison below would catch that anyway, because a
        // token boundary cannot move without some text run changing with it — but not when
        // one value is empty and the other is not, which is exactly what a property one leg
        // computes and the other leaves blank looks like.
        if (referenceTokens.Count != candidateTokens.Count)
        {
            return false;
        }

        for (var i = 0; i < referenceTokens.Count; i++)
        {
            var referenceToken = referenceTokens[i];
            var candidateToken = candidateTokens[i];

            // A number opposite a text run falls to the text comparison, which is correct
            // and needs no separate test for the kinds: tokenizing is a function of the
            // text, so two runs spelt the same are always the same kind, and two runs spelt
            // differently are unequal as text whatever kinds they are.
            var equal = referenceToken.Number is { } referenceNumber
                && candidateToken.Number is { } candidateNumber
                ? Math.Abs(referenceNumber - candidateNumber)
                    <= (referenceToken.IsLength && candidateToken.IsLength ? epsilon : 0)
                : string.Equals(referenceToken.Text, candidateToken.Text, StringComparison.Ordinal);

            if (!equal)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Splits a value into its numeric and non-numeric runs, marking the numeric ones that
    /// carry a length.
    /// </summary>
    /// <remarks>
    /// A run that looks numeric but will not parse — <c>1.2.3</c>, say — is kept as text and
    /// compared ordinally, so a value this tokenizer does not understand fails closed
    /// rather than being waved through by a number it guessed at.
    /// </remarks>
    private static List<Token> Tokenize(string value)
    {
        var tokens = new List<Token>();
        var index = 0;

        // The translation arguments of the transform matrix the walk is inside, and how many
        // numbers into it the walk has come. A text run is always read before the numbers
        // that follow it, so an opener is seen before its own arguments are.
        //
        // Nothing closes the state, because nothing needs to: a matrix's own arguments
        // consume every index the tolerance names, so a number written after one closes can
        // only be counted higher than any of them, and a second opener starts its own count.
        // Only a matrix with too few arguments could escape that, and neither a computed
        // value nor a published custom property holds one.
        int[]? translations = null;
        var argument = 0;

        while (index < value.Length)
        {
            var start = index;

            if (StartsNumber(value, index))
            {
                // The sign or first digit, then the rest of the number.
                index++;
                while (index < value.Length && (char.IsAsciiDigit(value[index]) || value[index] == '.'))
                {
                    index++;
                }

                var text = value[start..index];
                var isLength = StartsWithLengthUnit(value, index)
                    || (translations is not null && Array.IndexOf(translations, argument) >= 0);
                argument++;

                tokens.Add(
                    double.TryParse(
                        text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                        ? new Token(text, number, isLength)
                        : new Token(text, null, false));

                continue;
            }

            while (index < value.Length && !StartsNumber(value, index))
            {
                index++;
            }

            var run = value[start..index];

            if (run.EndsWith("matrix3d(", StringComparison.Ordinal))
            {
                translations = Matrix3dTranslations;
                argument = 0;
            }
            else if (run.EndsWith("matrix(", StringComparison.Ordinal))
            {
                translations = MatrixTranslations;
                argument = 0;
            }

            tokens.Add(new Token(run, null, false));
        }

        return tokens;
    }

    /// <summary>Reports whether a number begins at <paramref name="index"/>.</summary>
    /// <remarks>
    /// <para>
    /// A sign or a decimal point only opens a number when a digit follows it. Without that
    /// test <c>cubic-bezier</c> and <c>-apple-system</c> split around their hyphens into
    /// runs that no longer say what they meant, and the hyphen in a font stack would be read
    /// as the sign of whatever digit happened to follow it.
    /// </para>
    /// <para>
    /// Nothing opens a number directly after a letter, because a digit there belongs to the
    /// identifier it is spelt inside. A unit is written after its number and never before
    /// one, so no value loses a number to this; what it keeps whole is <c>matrix3d</c> and
    /// <c>translate3d</c>, whose <c>3</c> would otherwise be read as the function's first
    /// argument and shift every real argument one place along.
    /// </para>
    /// </remarks>
    private static bool StartsNumber(string value, int index)
    {
        if (index > 0 && char.IsAsciiLetter(value[index - 1]))
        {
            return false;
        }

        var character = value[index];

        return char.IsAsciiDigit(character)
            || (character is '-' or '.'
                && index + 1 < value.Length
                && char.IsAsciiDigit(value[index + 1]));
    }

    /// <summary>
    /// Reports whether a length unit begins at <paramref name="index"/>, which is where the
    /// number just read ended.
    /// </summary>
    private static bool StartsWithLengthUnit(string value, int index)
    {
        foreach (var unit in LengthUnits)
        {
            if (value.AsSpan(index).StartsWith(unit, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>One run of a value.</summary>
    /// <param name="Text">The run as it was written.</param>
    /// <param name="Number">
    /// The run's value when it parsed as a number, or <see langword="null"/> when the run is
    /// to be compared as text.
    /// </param>
    /// <param name="IsLength">
    /// Whether the run carries a length, and so is the tolerance's to absorb.
    /// </param>
    private readonly record struct Token(string Text, double? Number, bool IsLength);
}
