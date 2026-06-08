namespace Blazix.BaseUI.Accordion;

/// <summary>
/// Represents the state of the <see cref="AccordionTrigger"/> component.
/// </summary>
/// <param name="Open">Whether the associated panel is open.</param>
/// <param name="Orientation">The visual orientation of the accordion.</param>
/// <param name="Index">The index of the accordion item.</param>
/// <param name="Value">The string value of the accordion item.</param>
/// <param name="Disabled">Whether the trigger is disabled.</param>
/// <param name="Hidden">Whether the accordion item's panel is hidden.</param>
public sealed record AccordionTriggerState(
    bool Open,
    Orientation Orientation,
    int Index,
    string Value,
    bool Disabled,
    bool Hidden);
