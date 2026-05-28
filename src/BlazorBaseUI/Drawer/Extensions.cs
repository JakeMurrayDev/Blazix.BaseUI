using BlazorBaseUI.Dialog;

namespace BlazorBaseUI.Drawer;

internal static class Extensions
{
    public static DialogModalMode ToDialogModalMode(this DrawerModalMode mode) => mode switch
    {
        DrawerModalMode.False => DialogModalMode.False,
        DrawerModalMode.TrapFocus => DialogModalMode.TrapFocus,
        _ => DialogModalMode.True
    };

    public static DrawerOpenChangeReason ToDrawerReason(this DialogOpenChangeReason reason) => reason switch
    {
        DialogOpenChangeReason.TriggerPress => DrawerOpenChangeReason.TriggerPress,
        DialogOpenChangeReason.OutsidePress => DrawerOpenChangeReason.OutsidePress,
        DialogOpenChangeReason.EscapeKey => DrawerOpenChangeReason.EscapeKey,
        DialogOpenChangeReason.ClosePress => DrawerOpenChangeReason.ClosePress,
        DialogOpenChangeReason.FocusOut => DrawerOpenChangeReason.FocusOut,
        DialogOpenChangeReason.ImperativeAction => DrawerOpenChangeReason.ImperativeAction,
        DialogOpenChangeReason.Swipe => DrawerOpenChangeReason.Swipe,
        DialogOpenChangeReason.CloseWatcher => DrawerOpenChangeReason.CloseWatcher,
        _ => DrawerOpenChangeReason.None
    };

    public static DialogOpenChangeReason ToDialogReason(this DrawerOpenChangeReason reason) => reason switch
    {
        DrawerOpenChangeReason.TriggerPress => DialogOpenChangeReason.TriggerPress,
        DrawerOpenChangeReason.OutsidePress => DialogOpenChangeReason.OutsidePress,
        DrawerOpenChangeReason.EscapeKey => DialogOpenChangeReason.EscapeKey,
        DrawerOpenChangeReason.ClosePress => DialogOpenChangeReason.ClosePress,
        DrawerOpenChangeReason.FocusOut => DialogOpenChangeReason.FocusOut,
        DrawerOpenChangeReason.ImperativeAction => DialogOpenChangeReason.ImperativeAction,
        DrawerOpenChangeReason.Swipe => DialogOpenChangeReason.Swipe,
        DrawerOpenChangeReason.CloseWatcher => DialogOpenChangeReason.CloseWatcher,
        _ => DialogOpenChangeReason.None
    };

    public static string? ToDataAttributeString(this DrawerSwipeDirection direction) => direction switch
    {
        DrawerSwipeDirection.Up => "up",
        DrawerSwipeDirection.Left => "left",
        DrawerSwipeDirection.Right => "right",
        DrawerSwipeDirection.Down => "down",
        _ => null
    };

    public static DrawerSwipeDirection Opposite(this DrawerSwipeDirection direction) => direction switch
    {
        DrawerSwipeDirection.Up => DrawerSwipeDirection.Down,
        DrawerSwipeDirection.Down => DrawerSwipeDirection.Up,
        DrawerSwipeDirection.Left => DrawerSwipeDirection.Right,
        DrawerSwipeDirection.Right => DrawerSwipeDirection.Left,
        _ => DrawerSwipeDirection.Up
    };
}
