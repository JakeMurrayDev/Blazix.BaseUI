using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BlazorBaseUI.Slider;

namespace BlazorBaseUI.NumberField;

internal static partial class NumberFieldUtilities
{
    public const double DefaultStep = 1;
    public const double SmallStep = 0.1;
    public const double LargeStep = 10;

    private const double StepEpsilonFactor = 1e-10;

    private static readonly ConcurrentDictionary<string, string> CurrencySymbolCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly char[] PercentSymbols = ['%', '\u066A', '\uFF05', '\uFE6A'];
    private static readonly char[] PermilleSymbols = ['\u2030', '\u0609'];
    private static readonly char[] UnicodeMinusSigns = ['\u2212', '\uFF0D', '\u2012', '\u2013', '\u2014', '\uFE63'];
    private static readonly char[] UnicodePlusSigns = ['\uFF0B', '\uFE62'];
    private static readonly char[] BaseNonNumericSymbols = ['.', ',', '\uFF0E', '\uFF0C', '\u066B', '\u066C'];

    public static string FormatNumber(
        double? value,
        string? locale,
        NumberFormatOptions? options,
        bool maximumPrecision = false)
    {
        if (!value.HasValue)
            return string.Empty;

        var culture = CreateCulture(locale);
        var numberFormat = (NumberFormatInfo)culture.NumberFormat.Clone();
        culture.NumberFormat = numberFormat;

        if (!string.IsNullOrEmpty(options?.Currency))
        {
            numberFormat.CurrencySymbol = ResolveCurrencySymbol(options.Currency, culture);
        }

        if (options?.UseGrouping == false)
        {
            numberFormat.NumberGroupSizes = [0];
            numberFormat.CurrencyGroupSizes = [0];
            numberFormat.PercentGroupSizes = [0];
        }

        var style = options?.Style?.ToLowerInvariant();
        if (style == "currency")
        {
            var digits = ResolveFractionDigits(options, numberFormat.CurrencyDecimalDigits, maximumPrecision);
            return value.Value.ToString($"C{digits}", culture);
        }

        if (style == "percent")
        {
            var digits = ResolveFractionDigits(options, 0, maximumPrecision);
            return value.Value.ToString($"P{digits}", culture);
        }

        if (style == "unit" && !string.IsNullOrWhiteSpace(options?.Unit))
        {
            return $"{FormatDecimal(value.Value, culture, options, maximumPrecision)} {options.Unit}";
        }

        return FormatDecimal(value.Value, culture, options ?? new NumberFormatOptions(), maximumPrecision);
    }

    public static string ToHiddenInputValue(double? value)
    {
        if (!value.HasValue)
            return string.Empty;

        return value.Value.ToString("G15", CultureInfo.InvariantCulture);
    }

    public static double? ParseNumber(string? formattedNumber, string? locale, NumberFormatOptions? options)
    {
        if (formattedNumber is null)
            return null;

        var original = formattedNumber;
        var input = FormatControlRegex().Replace(formattedNumber, string.Empty).Trim();
        if (input.Length == 0)
            return null;

        input = NormalizeSigns(input);

        if (InfinityRegex().IsMatch(input) || input.Contains('\u221E'))
            return null;

        var isNegative = false;
        var trailing = TrailingSignRegex().Match(input);
        if (trailing.Success)
        {
            isNegative = trailing.Groups[1].Value == "-";
            input = TrailingSignRegex().Replace(input, string.Empty);
        }

        var leading = LeadingSignRegex().Match(input);
        if (leading.Success)
        {
            if (leading.Groups[1].Value == "-")
                isNegative = true;

            input = LeadingSignRegex().Replace(input, string.Empty);
        }

        var computedLocale = locale;
        if (computedLocale is null)
        {
            if (ArabicDetectRegex().IsMatch(input) || PersianDetectRegex().IsMatch(input))
                computedLocale = "ar";
            else if (HanDetectRegex().IsMatch(input))
                computedLocale = "zh";
        }

        var culture = CreateCulture(computedLocale);
        var numberFormat = culture.NumberFormat;
        var group = numberFormat.NumberGroupSeparator;
        var decimalSeparator = numberFormat.NumberDecimalSeparator;
        var currency = !string.IsNullOrEmpty(options?.Currency)
            ? ResolveCurrencySymbol(options.Currency, culture)
            : numberFormat.CurrencySymbol;

        input = ReplaceGrouping(input, group);
        input = ReplaceDecimal(input, decimalSeparator);
        input = input
            .Replace('\uFF0E', '.')
            .Replace("\uFF0C", string.Empty)
            .Replace('\u066B', '.')
            .Replace("\u066C", string.Empty);

        input = StripToken(input, currency);
        if (!string.IsNullOrEmpty(options?.Currency))
            input = StripToken(input, options.Currency);
        if (!string.IsNullOrEmpty(options?.Unit))
            input = StripToken(input, options.Unit);

        input = NormalizeNumerals(input);
        input = RemoveSymbols(input, PercentSymbols);
        input = RemoveSymbols(input, PermilleSymbols);
        input = SpaceSeparatorRegex().Replace(input, string.Empty);

        var lastDot = input.LastIndexOf('.');
        if (lastDot >= 0)
        {
            var before = input[..lastDot].Replace(".", string.Empty, StringComparison.Ordinal);
            var after = input[(lastDot + 1)..].Replace(".", string.Empty, StringComparison.Ordinal);
            input = $"{before}.{after}";
        }

        if (input.Length == 0)
            return null;

        var parseTarget = (isNegative ? "-" : string.Empty) + input;
        if (!double.TryParse(parseTarget, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ||
            double.IsNaN(number) ||
            double.IsInfinity(number))
        {
            return null;
        }

        var style = options?.Style?.ToLowerInvariant();
        var isUnitPercent = style == "unit" && string.Equals(options?.Unit, "percent", StringComparison.OrdinalIgnoreCase);
        var hasPercentSymbol = original.IndexOfAny(PercentSymbols) >= 0 || style == "percent";
        var hasPermilleSymbol = original.IndexOfAny(PermilleSymbols) >= 0;

        if (hasPermilleSymbol)
            number /= 1000;
        else if (!isUnitPercent && hasPercentSymbol)
            number /= 100;

        return number;
    }

    public static bool IsValidCharacterString(
        string input,
        string? locale,
        NumberFormatOptions? options,
        double minWithDefault)
    {
        var allowed = GetAllowedNonNumericCharacters(locale, options, minWithDefault);

        foreach (var ch in input)
        {
            if (IsSupportedDigit(ch) || allowed.Contains(ch))
                continue;

            return false;
        }

        return true;
    }

    public static double? ToValidatedNumber(
        double? value,
        double? step,
        double minWithDefault,
        double maxWithDefault,
        double minWithZeroDefault,
        NumberFormatOptions? format,
        bool snapOnStep,
        bool small,
        bool clamp)
    {
        if (!value.HasValue)
            return null;

        var clampedValue = clamp
            ? Math.Max(minWithDefault, Math.Min(maxWithDefault, value.Value))
            : value.Value;

        if (step.HasValue && snapOnStep)
        {
            if (step.Value == 0)
                return RemoveFloatingPointErrors(clampedValue, format);

            var baseValue = minWithZeroDefault;
            if (!small && minWithDefault != double.MinValue)
                baseValue = minWithDefault;

            var snappedValue = SnapToStep(clampedValue, baseValue, step.Value, small);
            return RemoveFloatingPointErrors(snappedValue, format);
        }

        return RemoveFloatingPointErrors(clampedValue, format);
    }

    public static double RemoveFloatingPointErrors(double value, NumberFormatOptions? format = null)
    {
        if (!double.IsFinite(value))
            return value;

        var maximumFractionDigits = GetMaximumFractionDigits(format);
        var digits = Math.Clamp(maximumFractionDigits, 0, 20);
        return Math.Round(value, digits, MidpointRounding.AwayFromZero);
    }

    public static double GetStepAmount(bool altKey, bool shiftKey, double smallStep, double largeStep, double step)
    {
        if (altKey)
            return smallStep;

        if (shiftKey)
            return largeStep;

        return step;
    }

    private static HashSet<char> GetAllowedNonNumericCharacters(
        string? locale,
        NumberFormatOptions? options,
        double minWithDefault)
    {
        var culture = CreateCulture(locale);
        var numberFormat = culture.NumberFormat;
        var keys = new HashSet<char>(BaseNonNumericSymbols);

        AddCharacters(keys, numberFormat.NumberDecimalSeparator);
        AddCharacters(keys, numberFormat.NumberGroupSeparator);
        if (SpaceSeparatorRegex().IsMatch(numberFormat.NumberGroupSeparator))
            keys.Add(' ');

        var style = options?.Style?.ToLowerInvariant();
        var allowPercentSymbols =
            style == "percent" ||
            (style == "unit" && string.Equals(options?.Unit, "percent", StringComparison.OrdinalIgnoreCase));
        var allowPermilleSymbols =
            style == "percent" ||
            (style == "unit" && string.Equals(options?.Unit, "permille", StringComparison.OrdinalIgnoreCase));

        if (allowPercentSymbols)
            foreach (var symbol in PercentSymbols)
                keys.Add(symbol);

        if (allowPermilleSymbols)
            foreach (var symbol in PermilleSymbols)
                keys.Add(symbol);

        if (style == "currency")
        {
            AddCharacters(keys, !string.IsNullOrEmpty(options?.Currency)
                ? ResolveCurrencySymbol(options.Currency, culture)
                : numberFormat.CurrencySymbol);
            AddCharacters(keys, options?.Currency);
        }

        AddCharacters(keys, options?.Unit);
        keys.Add('+');
        foreach (var plus in UnicodePlusSigns)
            keys.Add(plus);

        if (minWithDefault < 0)
        {
            keys.Add('-');
            foreach (var minus in UnicodeMinusSigns)
                keys.Add(minus);
        }

        return keys;
    }

    private static string FormatDecimal(
        double value,
        CultureInfo culture,
        NumberFormatOptions options,
        bool maximumPrecision)
    {
        var minimumFractionDigits = Math.Max(0, options.MinimumFractionDigits ?? 0);
        var maximumFractionDigits = maximumPrecision
            ? 20
            : Math.Max(minimumFractionDigits, options.MaximumFractionDigits ?? Math.Max(3, minimumFractionDigits));
        var minimumIntegerDigits = Math.Max(1, options.MinimumIntegerDigits ?? 1);
        var integerPattern = new string('0', minimumIntegerDigits);

        if (options.UseGrouping != false)
            integerPattern = "#," + integerPattern;

        var fractionPattern = maximumFractionDigits == 0
            ? string.Empty
            : "." + new string('0', minimumFractionDigits) + new string('#', maximumFractionDigits - minimumFractionDigits);

        return value.ToString(integerPattern + fractionPattern, culture);
    }

    private static int ResolveFractionDigits(NumberFormatOptions? options, int defaultDigits, bool maximumPrecision)
    {
        if (maximumPrecision && options?.MaximumFractionDigits is null)
            return 20;

        if (options?.MaximumFractionDigits.HasValue == true)
            return options.MaximumFractionDigits.Value;

        if (options?.MinimumFractionDigits.HasValue == true)
            return options.MinimumFractionDigits.Value;

        return defaultDigits;
    }

    private static int GetMaximumFractionDigits(NumberFormatOptions? format)
    {
        var minimumFractionDigits = format?.MinimumFractionDigits ?? 0;
        var maximumFractionDigits = Math.Max(format?.MaximumFractionDigits ?? 3, minimumFractionDigits);
        return maximumFractionDigits;
    }

    private static double SnapToStep(double clampedValue, double baseValue, double step, bool nearest)
    {
        if (step == 0)
            return clampedValue;

        var stepSize = Math.Abs(step);
        var direction = Math.Sign(step);
        var tolerance = stepSize * StepEpsilonFactor * direction;
        var divisor = nearest ? step : stepSize;
        var rawSteps = (clampedValue - baseValue + tolerance) / divisor;

        double snappedSteps;
        if (nearest)
            snappedSteps = Math.Round(rawSteps, MidpointRounding.AwayFromZero);
        else if (direction > 0)
            snappedSteps = Math.Floor(rawSteps);
        else
            snappedSteps = Math.Ceiling(rawSteps);

        var stepForResult = nearest ? step : stepSize;
        return baseValue + snappedSteps * stepForResult;
    }

    private static bool IsSupportedDigit(char ch) =>
        ch is >= '0' and <= '9' ||
        ch is >= '\u0660' and <= '\u0669' ||
        ch is >= '\u06F0' and <= '\u06F9' ||
        ch is >= '\uFF10' and <= '\uFF19' ||
        ch is '\u96F6' or '\u3007' or '\u4E00' or '\u4E8C' or '\u4E09' or '\u56DB' or '\u4E94' or '\u516D' or '\u4E03' or '\u516B' or '\u4E5D';

    private static string NormalizeNumerals(string input)
    {
        var builder = new StringBuilder(input.Length);

        foreach (var ch in input)
        {
            builder.Append(ch switch
            {
                >= '\u0660' and <= '\u0669' => (char)('0' + ch - '\u0660'),
                >= '\u06F0' and <= '\u06F9' => (char)('0' + ch - '\u06F0'),
                >= '\uFF10' and <= '\uFF19' => (char)('0' + ch - '\uFF10'),
                '\u96F6' => '0',
                '\u3007' => '0',
                '\u4E00' => '1',
                '\u4E8C' => '2',
                '\u4E09' => '3',
                '\u56DB' => '4',
                '\u4E94' => '5',
                '\u516D' => '6',
                '\u4E03' => '7',
                '\u516B' => '8',
                '\u4E5D' => '9',
                _ => ch
            });
        }

        return builder.ToString();
    }

    private static string NormalizeSigns(string input)
    {
        foreach (var minus in UnicodeMinusSigns)
            input = input.Replace(minus, '-');

        foreach (var plus in UnicodePlusSigns)
            input = input.Replace(plus, '+');

        return input;
    }

    private static string ReplaceGrouping(string input, string? group)
    {
        if (string.IsNullOrEmpty(group))
            return input;

        if (SpaceSeparatorRegex().IsMatch(group))
            return SpaceSeparatorRegex().Replace(input, string.Empty);

        if (group is "'" or "\u2019")
            return input.Replace("'", string.Empty, StringComparison.Ordinal).Replace("\u2019", string.Empty, StringComparison.Ordinal);

        return input.Replace(group, string.Empty, StringComparison.Ordinal);
    }

    private static string ReplaceDecimal(string input, string? decimalSeparator)
    {
        if (string.IsNullOrEmpty(decimalSeparator))
            return input;

        return input.Replace(decimalSeparator, ".", StringComparison.Ordinal);
    }

    private static string StripToken(string input, string? token)
    {
        if (string.IsNullOrEmpty(token))
            return input;

        return input.Replace(token, string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static string RemoveSymbols(string input, char[] symbols)
    {
        foreach (var symbol in symbols)
            input = input.Replace(symbol.ToString(), string.Empty, StringComparison.Ordinal);

        return input;
    }

    private static void AddCharacters(HashSet<char> keys, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        foreach (var ch in value)
            keys.Add(ch);
    }

    private static CultureInfo CreateCulture(string? locale)
    {
        if (!string.IsNullOrWhiteSpace(locale))
        {
            try
            {
                return (CultureInfo)CultureInfo.GetCultureInfo(locale).Clone();
            }
            catch (CultureNotFoundException)
            {
            }
        }

        return (CultureInfo)CultureInfo.CurrentCulture.Clone();
    }

    private static string ResolveCurrencySymbol(string currency, CultureInfo culture)
    {
        var cacheKey = $"{culture.Name}|{currency}";
        return CurrencySymbolCache.GetOrAdd(cacheKey, _ => ResolveCurrencySymbolUncached(currency, culture));
    }

    private static string ResolveCurrencySymbolUncached(string currency, CultureInfo culture)
    {
        try
        {
            var region = new RegionInfo(culture.Name);
            if (string.Equals(region.ISOCurrencySymbol, currency, StringComparison.OrdinalIgnoreCase))
                return region.CurrencySymbol;
        }
        catch (ArgumentException)
        {
        }

        foreach (var specificCulture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            try
            {
                var region = new RegionInfo(specificCulture.Name);
                if (string.Equals(region.ISOCurrencySymbol, currency, StringComparison.OrdinalIgnoreCase))
                    return region.CurrencySymbol;
            }
            catch (ArgumentException)
            {
            }
        }

        return currency;
    }

    [GeneratedRegex(@"\p{Cf}", RegexOptions.CultureInvariant)]
    private static partial Regex FormatControlRegex();

    [GeneratedRegex(@"\p{Zs}", RegexOptions.CultureInvariant)]
    private static partial Regex SpaceSeparatorRegex();

    [GeneratedRegex(@"([+-])\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex TrailingSignRegex();

    [GeneratedRegex(@"^\s*([+-])", RegexOptions.CultureInvariant)]
    private static partial Regex LeadingSignRegex();

    [GeneratedRegex(@"^[-+]?Infinity$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InfinityRegex();

    [GeneratedRegex("[\u0660-\u0669]", RegexOptions.CultureInvariant)]
    private static partial Regex ArabicDetectRegex();

    [GeneratedRegex("[\u06F0-\u06F9]", RegexOptions.CultureInvariant)]
    private static partial Regex PersianDetectRegex();

    [GeneratedRegex("[\u96F6\u3007\u4E00\u4E8C\u4E09\u56DB\u4E94\u516D\u4E03\u516B\u4E5D]", RegexOptions.CultureInvariant)]
    private static partial Regex HanDetectRegex();
}
