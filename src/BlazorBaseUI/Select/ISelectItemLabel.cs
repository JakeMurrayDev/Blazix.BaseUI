namespace BlazorBaseUI.Select;

/// <summary>
/// Optional contract for value types that carry their own display label.
/// When a selected value implements this interface and returns a non-null
/// <see cref="Label"/>, <see cref="SelectRootContext{TValue}.GetLabel"/> uses
/// it as the display label before falling back to the <c>Items</c> scan.
/// Mirrors the React Base UI duck-typed <c>value.label</c> precedence in
/// <c>resolveSelectedLabel</c>.
/// </summary>
public interface ISelectItemLabel
{
    /// <summary>
    /// Gets the display label for this value, or <see langword="null"/>
    /// to defer to the Items scan / <c>ToString</c> fallback.
    /// </summary>
    string? Label { get; }
}
