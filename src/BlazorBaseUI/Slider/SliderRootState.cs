using BlazorBaseUI.Field;

namespace BlazorBaseUI.Slider;

/// <summary>
/// Represents the current state of a <see cref="SliderRoot"/> component.
/// </summary>
/// <param name="ActiveThumbIndex">Gets the index of the currently active (focused or dragged) thumb, or <c>-1</c> if none.</param>
/// <param name="Disabled">Gets whether the slider is disabled.</param>
/// <param name="Dragging">Gets whether a thumb is currently being dragged.</param>
/// <param name="Max">Gets the maximum allowed value.</param>
/// <param name="Min">Gets the minimum allowed value.</param>
/// <param name="MinStepsBetweenValues">Gets the minimum number of steps between values in a range slider.</param>
/// <param name="Orientation">Gets the orientation of the slider.</param>
/// <param name="ReadOnly">Gets whether the slider is read-only.</param>
/// <param name="Required">Gets whether the slider is required for form submission.</param>
/// <param name="Step">Gets the step granularity.</param>
/// <param name="Values">Gets the current slider value(s).</param>
/// <param name="Valid">Gets whether the slider is in a valid state, or <see langword="null"/> if validation is not applicable.</param>
/// <param name="Touched">Gets whether the slider has been interacted with.</param>
/// <param name="Dirty">Gets whether the slider value has changed from its initial value.</param>
/// <param name="Filled">Gets whether the slider has a non-minimum value.</param>
/// <param name="Focused">Gets whether the slider currently has focus.</param>
public sealed record SliderRootState(
    int ActiveThumbIndex,
    bool Disabled,
    bool Dragging,
    double Max,
    double Min,
    double MinStepsBetweenValues,
    Orientation Orientation,
    bool ReadOnly,
    bool Required,
    double Step,
    double[] Values,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    internal static SliderRootState Default { get; } = new(
        ActiveThumbIndex: -1,
        Disabled: false,
        Dragging: false,
        Max: 100,
        Min: 0,
        MinStepsBetweenValues: 0,
        Orientation: Orientation.Horizontal,
        ReadOnly: false,
        Required: false,
        Step: 1,
        Values: [0],
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    internal static SliderRootState FromFieldState(
        FieldRootState fieldState,
        int activeThumbIndex,
        bool disabled,
        bool dragging,
        double max,
        double min,
        double minStepsBetweenValues,
        Orientation orientation,
        bool readOnly,
        bool required,
        double step,
        double[] values) => new(
            ActiveThumbIndex: activeThumbIndex,
            Disabled: disabled,
            Dragging: dragging,
            Max: max,
            Min: min,
            MinStepsBetweenValues: minStepsBetweenValues,
            Orientation: orientation,
            ReadOnly: readOnly,
            Required: required,
            Step: step,
            Values: values,
            Valid: fieldState.Valid,
            Touched: fieldState.Touched,
            Dirty: fieldState.Dirty,
            Filled: fieldState.Filled,
            Focused: fieldState.Focused);
}
