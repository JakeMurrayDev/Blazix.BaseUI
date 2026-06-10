namespace Blazix.BaseUI.NumberField;

/// <summary>
/// A custom element to display instead of the native cursor while using the scrub area.
/// Renders a <c>&lt;span&gt;</c> element with <c>role="presentation"</c>.
/// This component uses the Pointer Lock API, which may prompt the browser to display
/// a related notification. It is disabled in Safari to avoid a layout shift.
/// </summary>
public sealed partial class NumberFieldScrubAreaCursor;
