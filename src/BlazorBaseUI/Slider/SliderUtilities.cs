using System.Globalization;

namespace BlazorBaseUI.Slider;

/// <summary>
/// Provides utility methods for slider value calculations, including clamping,
/// percentage conversion, step rounding, thumb collision resolution, and number formatting.
/// </summary>
internal static class SliderUtilities
{
    public static double Clamp(double value, double min, double max) =>
        Math.Max(min, Math.Min(max, value));

    public static double ValueToPercent(double value, double min, double max) =>
        ((value - min) / (max - min)) * 100;

    public static double RoundValueToStep(double value, double step, double min)
    {
        if (step == 0)
            return value;

        var nearest = Math.Round((value - min) / step) * step + min;
        return Math.Round(nearest * 1e12) / 1e12;
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

    public static bool ValidateMinimumDistance(double[] values, double step, int minStepsBetweenValues)
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
        int minStepsBetweenValues)
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

    private static ResolveThumbCollisionResult ResolveSwapCollision(
        double[] activeValues,
        double[] baselineValues,
        int pressedIndex,
        double nextValue,
        double min,
        double max,
        double step,
        int minStepsBetweenValues,
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

    public static double[] GetPushedThumbValues(
        double[] values,
        int index,
        double nextValue,
        double min,
        double max,
        double step,
        int minStepsBetweenValues,
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
        if (options is null)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        var culture = string.IsNullOrEmpty(locale)
            ? CultureInfo.CurrentCulture
            : CultureInfo.GetCultureInfo(locale);

        var format = options.Style switch
        {
            "percent" => "P",
            "currency" => "C",
            _ => "N"
        };

        if (options.MaximumFractionDigits.HasValue)
        {
            format += options.MaximumFractionDigits.Value;
        }

        return value.ToString(format, culture);
    }
}

/// <summary>
/// Contains the result of resolving a thumb collision during slider interaction.
/// </summary>
/// <param name="Values">The adjusted thumb values after collision resolution.</param>
/// <param name="ThumbIndex">The index of the active thumb.</param>
/// <param name="DidSwap">Whether the active thumb swapped position with a neighbor.</param>
internal sealed record ResolveThumbCollisionResult(double[] Values, int ThumbIndex, bool DidSwap);
