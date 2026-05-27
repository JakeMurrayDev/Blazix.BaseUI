namespace BlazorBaseUI.Drawer;

/// <summary>
/// Represents the state of the drawer root component.
/// </summary>
public readonly record struct DrawerRootState;

/// <summary>
/// Represents the state of the drawer provider component.
/// </summary>
public readonly record struct DrawerProviderState;

/// <summary>
/// Represents the state of the drawer trigger component.
/// </summary>
public readonly record struct DrawerTriggerState(bool Disabled, bool Open);

/// <summary>
/// Represents the state of the drawer close component.
/// </summary>
public readonly record struct DrawerCloseState(bool Disabled);

/// <summary>
/// Represents the state of the drawer title component.
/// </summary>
public readonly record struct DrawerTitleState;

/// <summary>
/// Represents the state of the drawer description component.
/// </summary>
public readonly record struct DrawerDescriptionState;

/// <summary>
/// Represents the state of the drawer content component.
/// </summary>
public readonly record struct DrawerContentState;

/// <summary>
/// Represents the state of the drawer backdrop component.
/// </summary>
public readonly record struct DrawerBackdropState(bool Open, TransitionStatus TransitionStatus);

/// <summary>
/// Represents the state of the drawer popup component.
/// </summary>
public readonly record struct DrawerPopupState(
    bool Open,
    TransitionStatus TransitionStatus,
    bool Expanded,
    bool Nested,
    bool NestedDrawerOpen,
    bool NestedDrawerSwiping,
    DrawerSwipeDirection SwipeDirection,
    bool Swiping);

/// <summary>
/// Represents the state of the drawer viewport component.
/// </summary>
public readonly record struct DrawerViewportState(
    bool Open,
    TransitionStatus TransitionStatus,
    bool Nested);

/// <summary>
/// Represents the state of the drawer swipe area component.
/// </summary>
public readonly record struct DrawerSwipeAreaState(
    bool Open,
    bool Swiping,
    DrawerSwipeDirection SwipeDirection,
    bool Disabled);

/// <summary>
/// Represents the state of the drawer indent component.
/// </summary>
public readonly record struct DrawerIndentState(bool Active);

/// <summary>
/// Represents the state of the drawer indent background component.
/// </summary>
public readonly record struct DrawerIndentBackgroundState(bool Active);
