using System.ComponentModel;

namespace BlazorBaseUI.Slider;

/// <summary>
/// Extension methods for converting slider enumerations to data attribute strings.
/// </summary>
internal static class Extensions
{
    extension(ThumbCollisionBehavior behavior)
    {
        public string ToDataAttributeString() =>
            behavior switch
            {
                ThumbCollisionBehavior.Push => "push",
                ThumbCollisionBehavior.Swap => "swap",
                ThumbCollisionBehavior.None => "none",
                _ => throw new InvalidEnumArgumentException(nameof(behavior), (int)behavior, typeof(ThumbCollisionBehavior))
            };
    }

    extension(ThumbAlignment alignment)
    {
        public string ToDataAttributeString() =>
            alignment switch
            {
                ThumbAlignment.Center => "center",
                ThumbAlignment.Edge => "edge",
                ThumbAlignment.EdgeClientOnly => "edge-client-only",
                _ => throw new InvalidEnumArgumentException(nameof(alignment), (int)alignment, typeof(ThumbAlignment))
            };
    }
}
