namespace Blazix.BaseUI.Tests.OtpField;

public class OtpFieldUtilitiesTests
{
    [Fact]
    public void StripWhitespace_RemovesAllWhitespace()
    {
        OtpFieldUtilities.StripWhitespace(" 12 3\t4\n5 ").ShouldBe("12345");
        OtpFieldUtilities.StripWhitespace(null).ShouldBe(string.Empty);
    }

    [Fact]
    public void Normalize_FiltersClampsAndReportsRejectedCharacters()
    {
        var result = OtpFieldUtilities.NormalizeWithDetails("1a 2b34c56", 4, OtpFieldValidationType.Numeric);

        result.Value.ShouldBe("1234");
        result.DidRejectCharacters.ShouldBeTrue();
    }

    [Fact]
    public void Normalize_FiltersUnicodeDecimalDigitsForBrowserPatternParity()
    {
        var result = OtpFieldUtilities.NormalizeWithDetails("\u06612", 6, OtpFieldValidationType.Numeric);

        result.Value.ShouldBe("2");
        result.DidRejectCharacters.ShouldBeTrue();
    }

    [Fact]
    public void Normalize_ThrowsForUnsupportedValidationType()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            OtpFieldUtilities.Normalize("1234", 6, (OtpFieldValidationType)999));
    }

    [Fact]
    public void Normalize_ComposesCustomNormalizationWithBuiltInValidation()
    {
        var result = OtpFieldUtilities.Normalize(
            "ab-12 cd!",
            6,
            OtpFieldValidationType.Alphanumeric,
            value => value.ToUpperInvariant());

        result.ShouldBe("AB12CD");
    }

    [Fact]
    public void Normalize_ReturnsEmptyForNegativeLength()
    {
        OtpFieldUtilities.Normalize("1234", -1, OtpFieldValidationType.None).ShouldBe(string.Empty);
    }

    [Fact]
    public void ReplaceValue_PreservesSuffixAfterNormalizedMiddleReplacement()
    {
        var result = OtpFieldUtilities.ReplaceValue(
            "1303",
            1,
            "29",
            4,
            OtpFieldValidationType.Numeric,
            value => value.Replace("9", string.Empty, StringComparison.Ordinal));

        result.ShouldBe("1203");
    }

    [Fact]
    public void RemoveCharacter_IgnoresOutOfBoundsIndex()
    {
        OtpFieldUtilities.RemoveCharacter("1234", 10).ShouldBe("1234");
        OtpFieldUtilities.RemoveCharacter("1234", 0).ShouldBe("234");
        OtpFieldUtilities.RemoveCharacter("1234", 3).ShouldBe("123");
    }
}
