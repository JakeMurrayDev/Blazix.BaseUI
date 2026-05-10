namespace BlazorBaseUI.Slider;

/// <summary>
/// Represents the bounding rectangle of a slider thumb element.
/// </summary>
internal sealed record ThumbRect(double Left, double Right, double Top, double Bottom, double Width, double Height, double MidX, double MidY);

/// <summary>
/// Contains the result of initiating a drag operation on a slider thumb.
/// </summary>
internal sealed record StartDragResult(int ThumbIndex, double[] Values, bool PressedOnFocusedThumb, bool WasTrackPress);

/// <summary>
/// Configuration passed to JavaScript for managing slider drag interactions.
/// </summary>
internal sealed record SliderDragConfig(double Min, double Max, double Step, int MinStepsBetweenValues, string Orientation, string Direction, string CollisionBehavior, string ThumbAlignment, double[] Values, bool Disabled, bool ReadOnly, double InsetOffset, bool NotifyOnMove);
