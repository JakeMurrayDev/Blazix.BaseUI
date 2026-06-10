using System.Collections.Concurrent;
using System.Globalization;

namespace Blazix.BaseUI.Slider;

/// <summary>
/// Provides utility methods for slider value calculations, including clamping,
/// percentage conversion, step rounding, thumb collision resolution, and number formatting.
/// </summary>
internal static class SliderUtilities
{
    private static readonly ConcurrentDictionary<string, string> CurrencySymbolCache = new(StringComparer.OrdinalIgnoreCase);

    public static double Clamp(double value, double min, double max) =>
        Math.Max(min, Math.Min(max, value));

    public static double ValueToPercent(double value, double min, double max) =>
        ((value - min) / (max - min)) * 100;

    public static double RoundValueToStep(double value, double step, double min)
    {
        if (step == 0)
            return value;

        var nearest = JsRound((value - min) / step) * step + min;
        var precision = Math.Max(GetDecimalPrecision(step), GetDecimalPrecision(min));
        return Math.Round(nearest, precision, MidpointRounding.AwayFromZero);
    }

    public static double GetNewValue(double thumbValue, double increment, int direction, double min, double max)
    {
        var value = direction == 1 ? thumbValue + increment : thumbValue - increment;
        var precision = Math.Max(
            GetDecimalPrecision(thumbValue),
            Math.Max(GetDecimalPrecision(increment), GetDecimalPrecision(min)));
        var roundedValue = Math.Round(value, precision, MidpointRounding.AwayFromZero);
        return Clamp(roundedValue, min, max);
    }

    public static double[] GetSliderValue(
        double valueInput,
        int index,
        double min,
        double max,
        bool range,
        double[] values)
    {
        var newValue = Clamp(valueInput, min, max);

        if (!range)
        {
            return [newValue];
        }

        if (index < 0 || index >= values.Length)
        {
            return [..values];
        }

        double[] result = [..values];

        var lowerBound = index > 0 ? values[index - 1] : double.NegativeInfinity;
        var upperBound = index < values.Length - 1 ? values[index + 1] : double.PositiveInfinity;

        result[index] = Clamp(newValue, lowerBound, upperBound);

        return result;
    }

    public static bool ValidateMinimumDistance(double[] values, double step, double minStepsBetweenValues)
    {
        if (values.Length < 2)
            return true;

        var minDistance = step * minStepsBetweenValues;

        for (var i = 0; i < values.Length - 1; i++)
        {
            var distance = Math.Abs(values[i + 1] - values[i]);
            if (distance < minDistance)
                return false;
        }

        return true;
    }

    public static ResolveThumbCollisionResult ResolveThumbCollision(
        ThumbCollisionBehavior behavior,
        double[] values,
        double[]? currentValues,
        double[]? initialValues,
        int pressedIndex,
        double nextValue,
        double min,
        double max,
        double step,
        double minStepsBetweenValues)
    {
        var activeValues = currentValues ?? values;
        var baselineValues = initialValues ?? values;
        var range = activeValues.Length > 1;

        if (pressedIndex < 0 || pressedIndex >= activeValues.Length)
        {
            var safeIndex = Math.Clamp(pressedIndex, 0, Math.Max(0, activeValues.Length - 1));
            return new ResolveThumbCollisionResult([..activeValues], safeIndex, false);
        }

        if (!range)
        {
            return new ResolveThumbCollisionResult([nextValue], 0, false);
        }

        var minValueDifference = step * minStepsBetweenValues;

        switch (behavior)
        {
            case ThumbCollisionBehavior.Swap:
                return ResolveSwapCollision(
                    activeValues, baselineValues, pressedIndex, nextValue,
                    min, max, step, minStepsBetweenValues, minValueDifference);

            case ThumbCollisionBehavior.Push:
                var pushedValues = GetPushedThumbValues(
                    activeValues, pressedIndex, nextValue, min, max, step, minStepsBetweenValues, null);
                return new ResolveThumbCollisionResult(pushedValues, pressedIndex, false);

            case ThumbCollisionBehavior.None:
            default:
                return ResolveNoneCollision(activeValues, pressedIndex, nextValue, min, max, minValueDifference);
        }
    }

    public static double[] GetPushedThumbValues(
        double[] values,
        int index,
        double nextValue,
        double min,
        double max,
        double step,
        double minStepsBetweenValues,
        double[]? initialValues)
    {
        if (values.Length == 0)
        {
            return [];
        }

        if (index < 0 || index >= values.Length)
        {
            return [..values];
        }

        double[] result = [..values];
        var minDistance = step * minStepsBetweenValues;
        var lastIndex = result.Length - 1;
        var baseInitialValues = initialValues ?? values;

        var indexMin = min + index * minDistance;
        var indexMax = max - (lastIndex - index) * minDistance;
        result[index] = Clamp(nextValue, indexMin, indexMax);

        for (var i = index + 1; i <= lastIndex; i++)
        {
            var minAllowed = result[i - 1] + minDistance;
            var maxAllowed = max - (lastIndex - i) * minDistance;
            var initialValue = i < baseInitialValues.Length ? baseInitialValues[i] : result[i];
            var candidate = Math.Max(result[i], minAllowed);

            if (initialValue < candidate)
            {
                candidate = Math.Max(initialValue, minAllowed);
            }

            result[i] = Clamp(candidate, minAllowed, maxAllowed);
        }

        for (var i = index - 1; i >= 0; i--)
        {
            var maxAllowed = result[i + 1] - minDistance;
            var minAllowed = min + i * minDistance;
            var initialValue = i < baseInitialValues.Length ? baseInitialValues[i] : result[i];
            var candidate = Math.Min(result[i], maxAllowed);

            if (initialValue > candidate)
            {
                candidate = Math.Min(initialValue, maxAllowed);
            }

            result[i] = Clamp(candidate, minAllowed, maxAllowed);
        }

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Math.Round(result[i] * 1e12) / 1e12;
        }

        return result;
    }

    public static string FormatNumber(double value, string? locale, NumberFormatOptions? options)
    {
        var culture = string.IsNullOrEmpty(locale)
            ? (CultureInfo)CultureInfo.CurrentCulture.Clone()
            : (CultureInfo)CultureInfo.GetCultureInfo(locale).Clone();
        var numberFormat = (NumberFormatInfo)culture.NumberFormat.Clone();
        culture.NumberFormat = numberFormat;

        if (options is null)
        {
            return FormatDecimal(value, culture, new NumberFormatOptions());
        }

        if (!string.IsNullOrEmpty(options.Currency))
        {
            numberFormat.CurrencySymbol = ResolveCurrencySymbol(options.Currency, culture);
        }

        if (options.UseGrouping == false)
        {
            numberFormat.NumberGroupSizes = [0];
            numberFormat.CurrencyGroupSizes = [0];
            numberFormat.PercentGroupSizes = [0];
        }

        return options.Style?.ToLowerInvariant() switch
        {
            "currency" => value.ToString($"C{ResolveFractionDigits(options, numberFormat.CurrencyDecimalDigits)}", culture),
            "percent" => value.ToString($"P{ResolveFractionDigits(options, numberFormat.PercentDecimalDigits)}", culture),
            _ => FormatDecimal(value, culture, options)
        };
    }

    private static ResolveThumbCollisionResult ResolveSwapCollision(
        double[] activeValues,
        double[] baselineValues,
        int pressedIndex,
        double nextValue,
        double min,
        double max,
        double step,
        double minStepsBetweenValues,
        double minValueDifference)
    {
        if (pressedIndex < 0 || pressedIndex >= activeValues.Length)
        {
            return new ResolveThumbCollisionResult([..activeValues], Math.Max(0, pressedIndex), false);
        }

        var pressedInitialValue = activeValues[pressedIndex];
        const double epsilon = 1e-7;
        double[] candidateValues = [..activeValues];
        var previousNeighbor = pressedIndex > 0 ? candidateValues[pressedIndex - 1] : (double?)null;
        var nextNeighbor = pressedIndex < candidateValues.Length - 1 ? candidateValues[pressedIndex + 1] : (double?)null;

        var lowerBound = previousNeighbor.HasValue ? previousNeighbor.Value + minValueDifference : min;
        var upperBound = nextNeighbor.HasValue ? nextNeighbor.Value - minValueDifference : max;

        var constrainedValue = Clamp(nextValue, lowerBound, upperBound);
        var pressedValueAfterClamp = Math.Round(constrainedValue * 1e12) / 1e12;
        candidateValues[pressedIndex] = pressedValueAfterClamp;

        var movingForward = nextValue > pressedInitialValue;
        var movingBackward = nextValue < pressedInitialValue;

        var shouldSwapForward = movingForward && nextNeighbor.HasValue && nextValue >= nextNeighbor.Value - epsilon;
        var shouldSwapBackward = movingBackward && previousNeighbor.HasValue && nextValue <= previousNeighbor.Value + epsilon;

        if (!shouldSwapForward && !shouldSwapBackward)
        {
            return new ResolveThumbCollisionResult(candidateValues, pressedIndex, false);
        }

        var targetIndex = shouldSwapForward ? pressedIndex + 1 : pressedIndex - 1;

        if (targetIndex < 0 || targetIndex >= candidateValues.Length)
        {
            return new ResolveThumbCollisionResult(candidateValues, pressedIndex, false);
        }

        var initialValuesForPush = new double[candidateValues.Length];
        for (var i = 0; i < candidateValues.Length; i++)
        {
            if (i == pressedIndex)
            {
                initialValuesForPush[i] = pressedValueAfterClamp;
            }
            else if (i < baselineValues.Length)
            {
                initialValuesForPush[i] = baselineValues[i];
            }
            else
            {
                initialValuesForPush[i] = activeValues[i];
            }
        }

        var nextValueForTarget = shouldSwapForward
            ? Math.Max(nextValue, candidateValues[targetIndex])
            : Math.Min(nextValue, candidateValues[targetIndex]);

        var adjustedValues = GetPushedThumbValues(
            candidateValues, targetIndex, nextValueForTarget, min, max, step, minStepsBetweenValues, initialValuesForPush);

        var neighborIndex = shouldSwapForward ? targetIndex - 1 : targetIndex + 1;

        if (neighborIndex >= 0 && neighborIndex < adjustedValues.Length)
        {
            var previousValue = neighborIndex > 0 ? adjustedValues[neighborIndex - 1] : (double?)null;
            var nextValueAfter = neighborIndex < adjustedValues.Length - 1 ? adjustedValues[neighborIndex + 1] : (double?)null;

            var neighborLowerBound = previousValue.HasValue ? previousValue.Value + minValueDifference : min;
            neighborLowerBound = Math.Max(neighborLowerBound, min + neighborIndex * minValueDifference);

            var neighborUpperBound = nextValueAfter.HasValue ? nextValueAfter.Value - minValueDifference : max;
            neighborUpperBound = Math.Min(neighborUpperBound, max - (adjustedValues.Length - 1 - neighborIndex) * minValueDifference);

            var restoredValue = Clamp(pressedValueAfterClamp, neighborLowerBound, neighborUpperBound);
            adjustedValues[neighborIndex] = Math.Round(restoredValue * 1e12) / 1e12;
        }

        return new ResolveThumbCollisionResult(adjustedValues, targetIndex, true);
    }

    private static ResolveThumbCollisionResult ResolveNoneCollision(
        double[] activeValues,
        int pressedIndex,
        double nextValue,
        double min,
        double max,
        double minValueDifference)
    {
        if (pressedIndex < 0 || pressedIndex >= activeValues.Length)
        {
            return new ResolveThumbCollisionResult([..activeValues], Math.Max(0, pressedIndex), false);
        }

        double[] candidateValues = [..activeValues];
        var previousNeighbor = pressedIndex > 0 ? candidateValues[pressedIndex - 1] : (double?)null;
        var nextNeighbor = pressedIndex < candidateValues.Length - 1 ? candidateValues[pressedIndex + 1] : (double?)null;

        var lowerBound = previousNeighbor.HasValue ? previousNeighbor.Value + minValueDifference : min;
        var upperBound = nextNeighbor.HasValue ? nextNeighbor.Value - minValueDifference : max;

        var constrainedValue = Clamp(nextValue, lowerBound, upperBound);
        candidateValues[pressedIndex] = Math.Round(constrainedValue * 1e12) / 1e12;

        return new ResolveThumbCollisionResult(candidateValues, pressedIndex, false);
    }

    private static int GetDecimalPrecision(double value)
    {
        if (value == 0)
            return 0;

        var text = Math.Abs(value).ToString("G", CultureInfo.InvariantCulture);
        var exponentIndex = text.IndexOf('E');
        if (exponentIndex < 0)
        {
            exponentIndex = text.IndexOf('e');
        }

        if (exponentIndex >= 0)
        {
            var mantissa = text[..exponentIndex];
            var exponent = int.Parse(text[(exponentIndex + 1)..], CultureInfo.InvariantCulture);
            var mantissaDecimalIndex = mantissa.IndexOf('.');
            var mantissaDecimalPlaces = mantissaDecimalIndex >= 0 ? mantissa.Length - mantissaDecimalIndex - 1 : 0;
            return exponent < 0 ? mantissaDecimalPlaces - exponent : Math.Max(0, mantissaDecimalPlaces - exponent);
        }

        var decimalIndex = text.IndexOf('.');
        return decimalIndex < 0 ? 0 : text.Length - decimalIndex - 1;
    }

    private static double JsRound(double value) => Math.Floor(value + 0.5);

    private static int ResolveFractionDigits(NumberFormatOptions options, int defaultDigits)
    {
        if (options.MaximumFractionDigits.HasValue)
        {
            return options.MaximumFractionDigits.Value;
        }

        if (options.MinimumFractionDigits.HasValue)
        {
            return options.MinimumFractionDigits.Value;
        }

        return defaultDigits;
    }

    private static string FormatDecimal(double value, CultureInfo culture, NumberFormatOptions options)
    {
        if (HasSignificantDigitOptions(options))
        {
            return FormatSignificantDecimal(value, culture, options);
        }

        var minimumFractionDigits = Math.Max(0, options.MinimumFractionDigits ?? 0);
        var maximumFractionDigits = Math.Max(minimumFractionDigits, options.MaximumFractionDigits ?? Math.Max(3, minimumFractionDigits));
        var minimumIntegerDigits = Math.Max(1, options.MinimumIntegerDigits ?? 1);
        var integerPattern = new string('0', minimumIntegerDigits);

        if (options.UseGrouping != false)
        {
            integerPattern = "#," + integerPattern;
        }

        var fractionPattern = maximumFractionDigits == 0
            ? string.Empty
            : "." + new string('0', minimumFractionDigits) + new string('#', maximumFractionDigits - minimumFractionDigits);

        return value.ToString(integerPattern + fractionPattern, culture);
    }

    private static bool HasSignificantDigitOptions(NumberFormatOptions options) =>
        options.MinimumSignificantDigits.HasValue || options.MaximumSignificantDigits.HasValue;

    private static string FormatSignificantDecimal(double value, CultureInfo culture, NumberFormatOptions options)
    {
        var minimumSignificantDigits = Math.Clamp(options.MinimumSignificantDigits ?? 1, 1, 21);
        var maximumSignificantDigits = Math.Clamp(options.MaximumSignificantDigits ?? 21, minimumSignificantDigits, 21);

        if (value == 0)
        {
            var zeroFractionDigits = options.MinimumSignificantDigits.HasValue
                ? minimumSignificantDigits - 1
                : 0;
            return 0m.ToString($"N{zeroFractionDigits}", culture);
        }

        var decimalValue = decimal.Parse(
            value.ToString("G", CultureInfo.InvariantCulture),
            NumberStyles.Float,
            CultureInfo.InvariantCulture);
        var roundedValue = RoundToSignificantDigits(decimalValue, maximumSignificantDigits);
        var invariantValue = ToInvariantSignificantString(
            roundedValue,
            minimumSignificantDigits,
            maximumSignificantDigits);
        var fractionDigits = CountFractionDigits(invariantValue);
        var parsedValue = decimal.Parse(invariantValue, NumberStyles.Number, CultureInfo.InvariantCulture);
        var minimumIntegerDigits = Math.Max(1, options.MinimumIntegerDigits ?? 1);
        var integerPattern = new string('0', minimumIntegerDigits);

        if (options.UseGrouping != false)
        {
            integerPattern = "#," + integerPattern;
        }

        var fractionPattern = fractionDigits == 0
            ? string.Empty
            : "." + new string('0', fractionDigits);

        return parsedValue.ToString(integerPattern + fractionPattern, culture);
    }

    private static decimal RoundToSignificantDigits(decimal value, int digits)
    {
        if (value == 0)
        {
            return 0;
        }

        var scale = (decimal)Math.Pow(
            10,
            Math.Floor(Math.Log10((double)Math.Abs(value))) - digits + 1);

        return Math.Round(value / scale, 0, MidpointRounding.AwayFromZero) * scale;
    }

    private static string ToInvariantSignificantString(decimal value, int minimumSignificantDigits, int maximumSignificantDigits)
    {
        var absoluteValue = Math.Abs(value);
        var exponent = absoluteValue == 0
            ? 0
            : (int)Math.Floor(Math.Log10((double)absoluteValue));
        var fractionDigits = exponent >= 0
            ? Math.Max(0, maximumSignificantDigits - exponent - 1)
            : -exponent - 1 + maximumSignificantDigits;
        var text = value.ToString($"F{fractionDigits}", CultureInfo.InvariantCulture);

        while (text.Contains('.') && text.EndsWith('0') && CountSignificantDigits(text[..^1]) >= minimumSignificantDigits)
        {
            text = text[..^1];
        }

        if (text.EndsWith('.'))
        {
            text = text[..^1];
        }

        return text;
    }

    private static int CountFractionDigits(string value)
    {
        var decimalIndex = value.IndexOf('.');
        return decimalIndex < 0 ? 0 : value.Length - decimalIndex - 1;
    }

    private static int CountSignificantDigits(string value)
    {
        var count = 0;
        var hasSeenNonZeroDigit = false;

        foreach (var character in value)
        {
            if (!char.IsDigit(character))
            {
                continue;
            }

            if (character == '0' && !hasSeenNonZeroDigit)
            {
                continue;
            }

            hasSeenNonZeroDigit = true;
            count++;
        }

        return count;
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
            {
                return region.CurrencySymbol;
            }
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
                {
                    return region.CurrencySymbol;
                }
            }
            catch (ArgumentException)
            {
            }
        }

        return currency;
    }
}

/// <summary>
/// Contains the result of resolving a thumb collision during slider interaction.
/// </summary>
/// <param name="Values">The adjusted thumb values after collision resolution.</param>
/// <param name="ThumbIndex">The index of the active thumb.</param>
/// <param name="DidSwap">Whether the active thumb swapped position with a neighbor.</param>
internal sealed record ResolveThumbCollisionResult(double[] Values, int ThumbIndex, bool DidSwap);
