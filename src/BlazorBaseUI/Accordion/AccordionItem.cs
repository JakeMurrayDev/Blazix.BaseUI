namespace BlazorBaseUI.Accordion;

/// <summary>
/// Groups an accordion header with the corresponding panel.
/// Renders a <c>&lt;div&gt;</c> element.
/// </summary>
/// <typeparam name="TValue">The type of the value used to identify accordion items.</typeparam>
// lint-ignore:RULE-11 Public type intentionally left unsealed; consumers may subclass for customization.
public partial class AccordionItem<TValue> where TValue : notnull;
