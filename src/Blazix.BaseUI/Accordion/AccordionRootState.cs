namespace Blazix.BaseUI.Accordion;

/// <summary>
/// Represents the state of the <see cref="AccordionRoot{TValue}"/> component.
/// </summary>
/// <typeparam name="TValue">The type of the value used to identify accordion items.</typeparam>
/// <param name="Value">The current value of the expanded item(s).</param>
/// <param name="Disabled">Whether the accordion is disabled.</param>
/// <param name="Orientation">The visual orientation of the accordion.</param>
public sealed record AccordionRootState<TValue>(
    TValue[] Value,
    bool Disabled,
    Orientation Orientation);
