namespace Blazix.BaseUI.Accordion;

/// <summary>
/// Represents the state of the <see cref="AccordionHeader"/> component.
/// </summary>
/// <param name="Index">The index of the accordion item.</param>
/// <param name="Orientation">The visual orientation of the accordion.</param>
/// <param name="Disabled">Whether the item is disabled.</param>
/// <param name="Open">Whether the item is open.</param>
/// <param name="Hidden">Whether the accordion item's panel is hidden.</param>
public sealed record AccordionHeaderState(
    int Index,
    Orientation Orientation,
    bool Disabled,
    bool Open,
    bool Hidden);
