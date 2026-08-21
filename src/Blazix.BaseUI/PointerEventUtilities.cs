using Microsoft.AspNetCore.Components.Web;

namespace Blazix.BaseUI;

/// <summary>
/// Provides helper methods for interpreting pointer and mouse event arguments.
/// </summary>
internal static class PointerEventUtilities
{
    /// <summary>
    /// Determines whether the event is a zero-delta move reported by WebKit while the pointer is
    /// stationary.
    /// </summary>
    /// <param name="isWebKitEngine">Whether the browser runs the WebKit engine.</param>
    /// <param name="e">The move event arguments.</param>
    /// <remarks>
    /// WebKit fires <c>mousemove</c>/<c>pointermove</c> with no movement when a list scrolls
    /// beneath a stationary pointer, which moves the highlight onto whichever item slides under the
    /// cursor and fights keyboard navigation (base-ui <c>#5265</c>). The engine check keeps the
    /// guard from swallowing legitimate zero-delta moves that other engines and synthesized events
    /// report.
    /// </remarks>
    public static bool IsStationaryWebKitPointer(bool isWebKitEngine, MouseEventArgs e) =>
        isWebKitEngine && e.MovementX == 0 && e.MovementY == 0;
}
