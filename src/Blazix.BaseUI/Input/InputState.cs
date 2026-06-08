using Blazix.BaseUI.Field;

namespace Blazix.BaseUI.Input;

/// <summary>
/// Represents the state of the <see cref="Input"/> component.
/// </summary>
/// <param name="Disabled">Whether the input is disabled.</param>
/// <param name="Touched">Whether the input has been touched (focused and blurred).</param>
/// <param name="Dirty">Whether the input value has been modified.</param>
/// <param name="Valid">Whether the input value is valid. <see langword="null"/> if not validated.</param>
/// <param name="Filled">Whether the input has a value.</param>
/// <param name="Focused">Whether the input is currently focused.</param>
public readonly record struct InputState(
    bool Disabled,
    bool Touched,
    bool Dirty,
    bool? Valid,
    bool Filled,
    bool Focused)
{
    internal static InputState Default => new(false, false, false, null, false, false);

    internal static InputState FromFieldRootState(FieldRootState state) =>
        new(state.Disabled, state.Touched, state.Dirty, state.Valid, state.Filled, state.Focused);

    public static implicit operator InputState(FieldRootState state) => FromFieldRootState(state);
}
