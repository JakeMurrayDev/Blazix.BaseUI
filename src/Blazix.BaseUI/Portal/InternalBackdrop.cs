namespace Blazix.BaseUI.Portal;

/// <summary>
/// A fixed-position backdrop element used internally by floating components for modal behavior.
/// Supports an optional cutout element whose bounding rectangle is excluded from the backdrop
/// via <c>clip-path: polygon()</c>, allowing that element to remain interactive.
/// Renders a <c>&lt;div&gt;</c> element with <c>role="presentation"</c>.
/// </summary>
public partial class InternalBackdrop;
