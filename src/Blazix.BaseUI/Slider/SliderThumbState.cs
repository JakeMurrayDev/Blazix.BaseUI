namespace Blazix.BaseUI.Slider;

/// <summary>
/// Represents the current state of a <see cref="SliderThumb"/> component.
/// </summary>
/// <param name="Index">Gets the index of this thumb in the slider's value array.</param>
/// <param name="Disabled">Gets whether the thumb is disabled.</param>
/// <param name="Dragging">Gets whether this thumb is currently being dragged.</param>
/// <param name="Orientation">Gets the orientation of the slider.</param>
/// <param name="ReadOnly">Gets whether the slider is read-only.</param>
/// <param name="Required">Gets whether the slider is required for form submission.</param>
/// <param name="Valid">Gets whether the slider is in a valid state, or <see langword="null"/> if validation is not applicable.</param>
/// <param name="Touched">Gets whether the slider has been interacted with.</param>
/// <param name="Dirty">Gets whether the slider value has changed from its initial value.</param>
/// <param name="Focused">Gets whether this thumb currently has focus.</param>
public sealed record SliderThumbState(
    int Index,
    bool Disabled,
    bool Dragging,
    Orientation Orientation,
    bool ReadOnly,
    bool Required,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Focused)
{
    internal static SliderThumbState Default { get; } = new(
        Index: 0,
        Disabled: false,
        Dragging: false,
        Orientation: Orientation.Horizontal,
        ReadOnly: false,
        Required: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Focused: false);

    internal static SliderThumbState FromRootState(SliderRootState rootState, int index, bool isActive) => new(
        Index: index,
        Disabled: rootState.Disabled,
        Dragging: rootState.Dragging && rootState.ActiveThumbIndex == index,
        Orientation: rootState.Orientation,
        ReadOnly: rootState.ReadOnly,
        Required: rootState.Required,
        Valid: rootState.Valid,
        Touched: rootState.Touched,
        Dirty: rootState.Dirty,
        Focused: isActive);
}
