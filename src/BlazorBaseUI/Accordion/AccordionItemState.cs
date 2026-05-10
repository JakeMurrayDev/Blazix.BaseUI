namespace BlazorBaseUI.Accordion;

/// <summary>
/// Represents the state of the <see cref="AccordionItem{TValue}"/> component.
/// </summary>
/// <typeparam name="TValue">The type of the value used to identify accordion items.</typeparam>
/// <param name="Value">The current value of the expanded item(s).</param>
/// <param name="Disabled">Whether the item is disabled.</param>
/// <param name="Orientation">The visual orientation of the accordion.</param>
/// <param name="Index">The index of the accordion item.</param>
/// <param name="Open">Whether the item is open.</param>
public sealed record AccordionItemState<TValue>(
    TValue[] Value,
    bool Disabled,
    Orientation Orientation,
    int Index,
    bool Open);
