using System.Globalization;

namespace Blazix.BaseUI.Parity.Tests.Diff;

/// <summary>
/// Compares two CSS values, allowing the numbers inside them to differ slightly.
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
/// with one rule and no per-property table. Because the non-numeric runs must still be
/// ordinally equal, a keyword difference is never absorbed; and because a colour channel
/// or a step count cannot differ by less than one, neither is weakened by a sub-pixel
/// epsilon.
/// </para>
/// </remarks>
public static class ValueTolerance
{
    /// <summary>
    /// Reports whether two values agree, treating numbers no further apart than
    /// <paramref name="epsilon"/> as equal.
    /// </summary>
    /// <param name="reference">The React value.</param>
    /// <param name="candidate">The Blazor value.</param>
    /// <param name="epsilon">The largest difference two numbers may have and still agree.</param>
    /// <returns>
    /// <see langword="true"/> when both values hold the same sequence of runs, every
    /// non-numeric run is ordinally equal, and every numeric run differs by at most
    /// <paramref name="epsilon"/>.
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
                ? Math.Abs(referenceNumber - candidateNumber) <= epsilon
                : string.Equals(referenceToken.Text, candidateToken.Text, StringComparison.Ordinal);

            if (!equal)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Splits a value into its alternating numeric and non-numeric runs.
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
                tokens.Add(
                    double.TryParse(
                        text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                        ? new Token(text, number)
                        : new Token(text, null));

                continue;
            }

            while (index < value.Length && !StartsNumber(value, index))
            {
                index++;
            }

            tokens.Add(new Token(value[start..index], null));
        }

        return tokens;
    }

    /// <summary>Reports whether a number begins at <paramref name="index"/>.</summary>
    /// <remarks>
    /// A sign or a decimal point only opens a number when a digit follows it. Without that
    /// test <c>cubic-bezier</c> and <c>-apple-system</c> split around their hyphens into
    /// runs that no longer say what they meant, and the hyphen in a font stack would be read
    /// as the sign of whatever digit happened to follow it.
    /// </remarks>
    private static bool StartsNumber(string value, int index)
    {
        var character = value[index];

        return char.IsAsciiDigit(character)
            || (character is '-' or '.'
                && index + 1 < value.Length
                && char.IsAsciiDigit(value[index + 1]));
    }

    /// <summary>One run of a value.</summary>
    /// <param name="Text">The run as it was written.</param>
    /// <param name="Number">
    /// The run's value when it parsed as a number, or <see langword="null"/> when the run is
    /// to be compared as text.
    /// </param>
    private readonly record struct Token(string Text, double? Number);
}
