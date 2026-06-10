using System.ComponentModel;

namespace Blazix.BaseUI.Toast;

internal static class Extensions
{
    extension(ToastPriority priority)
    {
        public string ToAttributeString() =>
            priority switch
            {
                ToastPriority.Low => "low",
                ToastPriority.High => "high",
                _ => throw new InvalidEnumArgumentException(nameof(priority), (int)priority, typeof(ToastPriority))
            };
    }

    extension(ToastSwipeDirection direction)
    {
        public string ToDataAttributeString() =>
            direction switch
            {
                ToastSwipeDirection.Up => "up",
                ToastSwipeDirection.Down => "down",
                ToastSwipeDirection.Left => "left",
                ToastSwipeDirection.Right => "right",
                _ => throw new InvalidEnumArgumentException(nameof(direction), (int)direction, typeof(ToastSwipeDirection))
            };
    }
}
