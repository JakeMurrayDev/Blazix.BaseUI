namespace Blazix.BaseUI.NumberField;

/// <summary>
/// Provides extension methods for NumberField enumerations.
/// </summary>
internal static class Extensions
{
    extension(ScrubDirection direction)
    {
        /// <summary>
        /// Converts the <see cref="ScrubDirection"/> to its lowercase string representation
        /// used in data attributes and JavaScript interop.
        /// </summary>
        public string ToDataAttributeString() =>
            direction switch
            {
                ScrubDirection.Horizontal => "horizontal",
                ScrubDirection.Vertical => "vertical",
                _ => "horizontal"
            };
    }

    extension(NumberFieldChangeReason reason)
    {
        /// <summary>
        /// Converts the <see cref="NumberFieldChangeReason"/> to its kebab-case string representation
        /// matching the Base UI event detail format.
        /// </summary>
        public string ToReasonString() =>
            reason switch
            {
                NumberFieldChangeReason.None => "none",
                NumberFieldChangeReason.InputChange => "input-change",
                NumberFieldChangeReason.InputClear => "input-clear",
                NumberFieldChangeReason.InputBlur => "input-blur",
                NumberFieldChangeReason.InputPaste => "input-paste",
                NumberFieldChangeReason.Keyboard => "keyboard",
                NumberFieldChangeReason.IncrementPress => "increment-press",
                NumberFieldChangeReason.DecrementPress => "decrement-press",
                NumberFieldChangeReason.Wheel => "wheel",
                NumberFieldChangeReason.Scrub => "scrub",
                _ => "none"
            };
    }
}
