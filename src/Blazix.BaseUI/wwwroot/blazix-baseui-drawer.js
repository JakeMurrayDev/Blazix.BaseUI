import { setOutsidePressEnabled } from './blazix-baseui-dialog.min.js';

const STATE_KEY = Symbol.for('Blazix.BaseUI.Drawer.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        roots: new Map(),
        providers: new Map(),
        registeredCssVars: false
    };
}

const state = window[STATE_KEY];
state.providers ??= new Map();

const DATA_SWIPING = 'data-swiping';
const DATA_SWIPE_DISMISS = 'data-swipe-dismiss';
const DATA_ENDING_STYLE = 'data-ending-style';
const CONTENT_SELECTOR = '[data-drawer-content]';
const SWIPE_IGNORE_SELECTOR = '[data-base-ui-swipe-ignore], [data-blazix-base-ui-swipe-ignore], [data-swipe-ignore]';
const DEFAULT_IGNORE_SELECTOR = 'button,a,input,select,textarea,label,[role="button"]';
const COMPOSITE_KEYS = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End']);
const DEFAULT_SWIPE_THRESHOLD = 40;
const REVERSE_CANCEL_THRESHOLD = 10;
const MIN_DRAG_THRESHOLD = 1;
const MIN_VELOCITY_DURATION_MS = 50;
const MIN_RELEASE_VELOCITY_DURATION_MS = 16;
const MAX_RELEASE_VELOCITY_AGE_MS = 80;
const MIN_SWIPE_THRESHOLD = 10;
const FAST_SWIPE_VELOCITY = 0.5;
const SNAP_VELOCITY_THRESHOLD = 0.5;
const SNAP_VELOCITY_MULTIPLIER = 300;
const MAX_SNAP_VELOCITY = 4;
const MIN_SWIPE_RELEASE_VELOCITY = 0.2;
const MAX_SWIPE_RELEASE_VELOCITY = 4;
const MIN_SWIPE_RELEASE_DURATION_MS = 80;
const MAX_SWIPE_RELEASE_DURATION_MS = 360;
const MIN_SWIPE_RELEASE_SCALAR = 0.1;
const MAX_SWIPE_RELEASE_SCALAR = 1;
const DEFAULT_SWIPE_OPEN_RATIO = 0.5;
const VELOCITY_THRESHOLD = 0.1;
const FALLBACK_SWIPE_OPEN_THRESHOLD = 40;
const KEYBOARD_RESIZE_THRESHOLD = 60;
const KEYBOARD_VISIBILITY_MARGIN = 16;
const KEYBOARD_SCROLL_SLACK = 48;
const INPUT_TAP_MOVE_THRESHOLD = 10;
const INPUT_TAP_HIT_SLOP = 16;
const KEYBOARD_INPUT_TYPES = new Set(['email', 'number', 'password', 'search', 'tel', 'text', 'url']);
const KEYBOARD_TAP_BLOCKED = Symbol('KeyboardTapBlocked');

function createRoot(rootId) {
    return {
        rootId,
        parentRootId: null,
        providerId: null,
        rootDotNetRef: null,
        popupDotNetRef: null,
        viewportDotNetRef: null,
        isOpen: false,
        mounted: false,
        popupElement: null,
        viewportElement: null,
        backdropElement: null,
        popupResizeObserver: null,
        popupKeyCleanup: null,
        viewportCleanup: null,
        viewportBoundElement: null,
        viewportBoundPopupElement: null,
        viewportBoundDotNetRef: null,
        closeWatcher: null,
        swipeAreas: new Map(),
        swipeDirection: 'down',
        snapToSequentialPoints: false,
        snapPoints: null,
        activeSnapPoint: null,
        popupHeight: 0,
        viewportHeight: 0,
        rootFontSize: 16,
        activeSnapPointOffset: null,
        frontmostHeight: 0,
        hasNestedDrawer: false,
        nestedOpenDialogCount: 0,
        transitionEnding: false,
        swiping: false,
        nestedSwipeActive: false,
        swipeAreaActive: false,
        pendingSwipeCloseSnapPoint: undefined,
        dismissSwipe: null,
        virtualKeyboardEnabled: false,
        virtualKeyboardCleanup: null,
        virtualKeyboardState: null,
        reportedSnapPointOffset: undefined
    };
}

function getRoot(rootId) {
    let root = state.roots.get(rootId);
    if (!root) {
        root = createRoot(rootId);
        state.roots.set(rootId, root);
    }
    return root;
}

function getProvider(providerId) {
    if (!providerId) {
        return null;
    }

    let provider = state.providers.get(providerId);
    if (!provider) {
        provider = {
            providerId,
            indents: new Map(),
            swipeProgress: 0,
            frontmostHeight: 0
        };
        state.providers.set(providerId, provider);
    }
    return provider;
}

async function invoke(dotNetRef, method, ...args) {
    if (!dotNetRef) {
        return undefined;
    }

    try {
        return await dotNetRef.invokeMethodAsync(method, ...args);
    } catch {
        return undefined;
    }
}

function registerCssVars() {
    if (state.registeredCssVars) {
        return;
    }

    if (typeof CSS !== 'undefined' && 'registerProperty' in CSS) {
        [
            '--drawer-swipe-movement-x',
            '--drawer-swipe-movement-y',
            '--drawer-snap-point-offset',
            '--drawer-keyboard-inset'
        ].forEach((name) => {
            try {
                CSS.registerProperty({
                    name,
                    syntax: '<length>',
                    inherits: false,
                    initialValue: '0px'
                });
            } catch {
                // Already registered.
            }
        });

        [
            ['--drawer-swipe-progress', '0'],
            ['--drawer-swipe-strength', '1']
        ].forEach(([name, initialValue]) => {
            try {
                CSS.registerProperty({
                    name,
                    syntax: '<number>',
                    inherits: false,
                    initialValue
                });
            } catch {
                // Already registered.
            }
        });
    }

    state.registeredCssVars = true;
}

export function initializeRootReporter(rootId, dotNetRef, parentRootId, providerId) {
    const root = getRoot(rootId);
    root.rootDotNetRef = dotNetRef;
    root.parentRootId = parentRootId || null;
    root.providerId = providerId || null;
    refreshCloseWatchers();
}

export function disposeRootReporter(rootId) {
    const root = state.roots.get(rootId);
    if (!root) {
        return;
    }

    cleanupCloseWatcher(root);
    cleanupViewport(root);
    cleanupPopup(root);
    cleanupVirtualKeyboard(root);

    for (const [areaId] of root.swipeAreas) {
        disposeSwipeArea(rootId, areaId);
    }

    setProviderVisual(root, 0, 0);
    syncParentProgress(root, 0);
    root.rootDotNetRef = null;
    state.roots.delete(rootId);
    refreshCloseWatchers();
}

export function setRootOpen(rootId, open) {
    const root = getRoot(rootId);
    root.isOpen = !!open;
    syncSwipeAreaAvailability(root);

    if (!root.isOpen) {
        root.dismissSwipe?.reset();
        applySwipeProgress(root, 0, false);
    } else {
        if (!root.swipeAreaActive) {
            root.dismissSwipe?.reset();
        }
        clearSwipeRelease(root);
    }

    syncVirtualKeyboard(root);
    refreshCloseWatchers();
}

export function initializePopup(rootId, element, dotNetRef, swipeDirection, parentRootId) {
    const root = getRoot(rootId);
    registerCssVars();

    cleanupPopup(root);
    root.popupElement = element;
    root.popupDotNetRef = dotNetRef;
    root.swipeDirection = swipeDirection || root.swipeDirection || 'down';
    root.parentRootId = parentRootId || root.parentRootId || null;

    if (element) {
        const onKeyDown = (event) => {
            if (COMPOSITE_KEYS.has(event.key)) {
                event.stopPropagation();
            }
        };
        element.addEventListener('keydown', onKeyDown);
        root.popupKeyCleanup = () => element.removeEventListener('keydown', onKeyDown);
    }

    if (element && typeof ResizeObserver === 'function') {
        root.popupResizeObserver = new ResizeObserver(() => measurePopup(root));
        root.popupResizeObserver.observe(element);
    }

    measurePopup(root);
    setupViewport(root);
    updateSnapPointOffset(root);
    syncVirtualKeyboard(root);
    reapplyActiveSwipeArea(root);
    refreshCloseWatchers();
}

export function updatePopup(
    rootId,
    open,
    mounted,
    swipeDirection,
    snapPoints,
    activeSnapPoint,
    frontmostHeight,
    hasNestedDrawer,
    transitionEnding,
    nestedOpenDialogCount
) {
    const root = getRoot(rootId);
    root.isOpen = !!open;
    root.mounted = !!mounted;
    root.swipeDirection = swipeDirection || 'down';
    root.snapPoints = Array.isArray(snapPoints) && snapPoints.length > 0 ? snapPoints : null;
    root.activeSnapPoint = activeSnapPoint ?? null;
    root.frontmostHeight = Number.isFinite(frontmostHeight) ? frontmostHeight : 0;
    root.hasNestedDrawer = !!hasNestedDrawer;
    root.transitionEnding = !!transitionEnding;
    root.nestedOpenDialogCount = Number.isFinite(nestedOpenDialogCount) ? nestedOpenDialogCount : 0;
    syncSwipeAreaAvailability(root);

    if (!root.mounted) {
        setPopupHeight(root, 0);
    } else {
        measurePopup(root);
    }
    measureViewport(root);
    updateSnapPointOffset(root);
    applySettledSnapPointProgress(root);
    setupViewport(root);
    syncVirtualKeyboard(root);
    reapplyActiveSwipeArea(root);
    refreshCloseWatchers();
}

export function disposePopup(rootId) {
    const root = state.roots.get(rootId);
    if (root) {
        cleanupPopup(root);
    }
}

export function setBackdropElement(rootId, element) {
    const root = getRoot(rootId);
    root.backdropElement = element || null;
}

export function initializeViewport(
    rootId,
    element,
    dotNetRef,
    swipeDirection,
    snapToSequentialPoints,
    virtualKeyboardEnabled
) {
    const root = getRoot(rootId);
    root.viewportElement = element;
    root.viewportDotNetRef = dotNetRef;
    root.reportedSnapPointOffset = undefined;
    root.swipeDirection = swipeDirection || root.swipeDirection || 'down';
    root.snapToSequentialPoints = !!snapToSequentialPoints;
    root.virtualKeyboardEnabled = root.virtualKeyboardEnabled || !!virtualKeyboardEnabled;
    measureViewport(root);
    setupViewport(root);
    syncVirtualKeyboard(root);
}

export function updateViewport(
    rootId,
    open,
    mounted,
    swipeDirection,
    snapToSequentialPoints,
    snapPoints,
    activeSnapPoint
) {
    const root = getRoot(rootId);
    root.isOpen = !!open;
    root.mounted = !!mounted;
    root.swipeDirection = swipeDirection || 'down';
    root.snapToSequentialPoints = !!snapToSequentialPoints;
    root.snapPoints = Array.isArray(snapPoints) && snapPoints.length > 0 ? snapPoints : null;
    root.activeSnapPoint = activeSnapPoint ?? null;
    syncSwipeAreaAvailability(root);
    measureViewport(root);
    updateSnapPointOffset(root);
    applySettledSnapPointProgress(root);
    setupViewport(root);
    syncVirtualKeyboard(root);
}

export function disposeViewport(rootId) {
    const root = state.roots.get(rootId);
    if (!root) {
        return;
    }

    cleanupViewport(root);
    cleanupVirtualKeyboard(root);
    root.viewportElement = null;
    root.viewportDotNetRef = null;
}

export function initializeSwipeArea(rootId, areaId, element, dotNetRef, swipeDirection, disabled) {
    const root = getRoot(rootId);
    const existing = root.swipeAreas.get(areaId);
    existing?.cleanup?.();

    const area = {
        areaId,
        element,
        dotNetRef,
        swipeDirection: swipeDirection || opposite(root.swipeDirection),
        disabled: !!disabled,
        enabled: !disabled,
        cleanup: null,
        engine: null,
        swipeActive: false,
        openedBySwipe: false,
        openAccepted: false,
        startEvent: null,
        dragDelta: { x: 0, y: 0 },
        closedOffset: null,
        appliedStyles: false,
        popupTransition: null,
        releaseGuardCleanup: null
    };

    root.swipeAreas.set(areaId, area);
    area.cleanup = setupSwipeArea(root, area);
    syncSwipeAreaAvailability(root);
}

export function updateSwipeArea(rootId, areaId, swipeDirection, disabled, enabled) {
    const root = getRoot(rootId);
    const area = root.swipeAreas.get(areaId);
    if (!area) {
        return;
    }

    area.swipeDirection = swipeDirection || opposite(root.swipeDirection);
    area.disabled = !!disabled;
    area.enabled = !!enabled;
    syncSwipeAreaAvailability(root);

    if (!area.enabled) {
        if (area.swipeActive) {
            finishSwipeArea(root, area);
        }
        area.engine?.reset();
        clearSwipeAreaStyles(root, area);
    }
}

export function disposeSwipeArea(rootId, areaId) {
    const root = state.roots.get(rootId);
    const area = root?.swipeAreas.get(areaId);
    if (!area) {
        return;
    }

    area.cleanup?.();
    area.releaseGuardCleanup?.();
    setOutsidePressEnabled(rootId, true);
    clearSwipeAreaStyles(root, area);
    root.swipeAreas.delete(areaId);
}

export function registerIndent(providerId, indentId, element) {
    const provider = getProvider(providerId);
    if (!provider || !element) {
        return;
    }

    provider.indents.set(indentId, element);
    applyIndentVisual(element, provider.swipeProgress, provider.frontmostHeight);
}

export function unregisterIndent(providerId, indentId, element) {
    const provider = state.providers.get(providerId);
    if (!provider) {
        return;
    }

    const registered = provider.indents.get(indentId);
    if (!element || registered === element) {
        provider.indents.delete(indentId);
    }

    if (element) {
        applyIndentVisual(element, 0, 0);
    }

    if (provider.indents.size === 0) {
        state.providers.delete(providerId);
    }
}

export function initializeIndent(element) {
    if (element) {
        applyIndentVisual(element, 0, 0);
    }
}

export function disposeIndent(element) {
    if (element) {
        applyIndentVisual(element, 0, 0);
    }
}

export function initializeVirtualKeyboardProvider(rootId) {
    const root = getRoot(rootId);
    root.virtualKeyboardEnabled = true;
    syncVirtualKeyboard(root);
}

export function disposeVirtualKeyboardProvider(rootId) {
    const root = state.roots.get(rootId);
    if (!root) {
        return;
    }

    root.virtualKeyboardEnabled = false;
    cleanupVirtualKeyboard(root);
}

function cleanupPopup(root) {
    root.popupResizeObserver?.disconnect();
    root.popupResizeObserver = null;
    root.popupKeyCleanup?.();
    root.popupKeyCleanup = null;
    root.popupElement = null;
    root.popupDotNetRef = null;
}

function cleanupViewport(root) {
    root.viewportCleanup?.();
    root.viewportCleanup = null;
    root.viewportBoundElement = null;
    root.viewportBoundPopupElement = null;
    root.viewportBoundDotNetRef = null;
    root.dismissSwipe?.reset();
    root.dismissSwipe = null;
}

function setupViewport(root) {
    if (!root.popupElement || !root.viewportElement || !root.viewportDotNetRef) {
        return;
    }

    if (
        root.viewportCleanup &&
        root.viewportBoundElement === root.viewportElement &&
        root.viewportBoundPopupElement === root.popupElement &&
        root.viewportBoundDotNetRef === root.viewportDotNetRef
    ) {
        return;
    }

    cleanupViewport(root);

    const element = root.viewportElement;
    const doc = element.ownerDocument || document;
    const abortController = new AbortController();
    const signal = abortController.signal;
    let activePointerId = null;
    let pointerDocumentCleanup = null;

    root.dismissSwipe = createSwipeDismiss(createViewportSwipeOptions(root));

    const cleanupPointerDocument = () => {
        pointerDocumentCleanup?.();
        pointerDocumentCleanup = null;
        activePointerId = null;
    };

    const onPointerMove = (event) => {
        if (activePointerId !== null && event.pointerId !== activePointerId) {
            return;
        }
        root.dismissSwipe?.move(event, element);
    };

    const onPointerEnd = (event) => {
        if (activePointerId !== null && event.pointerId !== activePointerId) {
            return;
        }
        root.dismissSwipe?.end(event, element);
        cleanupPointerDocument();
    };

    const bindPointerDocument = (event) => {
        cleanupPointerDocument();
        activePointerId = event.pointerId;
        const options = { capture: true };
        doc.addEventListener('pointermove', onPointerMove, options);
        doc.addEventListener('pointerup', onPointerEnd, options);
        doc.addEventListener('pointercancel', onPointerEnd, options);
        pointerDocumentCleanup = () => {
            doc.removeEventListener('pointermove', onPointerMove, options);
            doc.removeEventListener('pointerup', onPointerEnd, options);
            doc.removeEventListener('pointercancel', onPointerEnd, options);
        };
    };

    const onPointerDown = (event) => {
        root.lastPointerType = event.pointerType;
        root.ignoreNextTouchStartFromPen = event.pointerType === 'pen';

        if (!root.isOpen || !root.mounted || hasOpenDescendant(root)) {
            return;
        }

        const target = getElementAtPoint(doc, event.clientX, event.clientY);
        if (isSwipeIgnoredTarget(target) || isDrawerContentTarget(target) || event.pointerType === 'touch') {
            return;
        }

        root.dismissSwipe?.start(event, element);
        bindPointerDocument(event);
    };

    const onTouchStart = (event) => {
        const startedFromPen = root.lastPointerType === 'pen' && root.ignoreNextTouchStartFromPen;
        if (startedFromPen) {
            root.ignoreNextTouchStartFromPen = false;
            resetTouchSwipeState(root, false);
            return;
        }

        if (!root.isOpen || !root.mounted || hasOpenDescendant(root)) {
            resetTouchSwipeState(root, false);
            return;
        }

        const touch = event.touches[0];
        if (!touch || isEventOnRangeInput(event, element.ownerDocument?.defaultView || window)) {
            resetTouchSwipeState(root, false);
            return;
        }

        const pointTarget = getElementAtPoint(doc, touch.clientX, touch.clientY);
        const eventTarget = getEventTarget(event);
        const target = eventTarget instanceof Element ? eventTarget : null;
        if (target && !contains(element, target)) {
            resetTouchSwipeState(root, true);
            return;
        }

        virtualKeyboardTouchStart(root, event);

        if (isSwipeIgnoredTarget(pointTarget)) {
            resetTouchSwipeState(root, true);
            return;
        }

        root.ignoreTouchSwipe = false;
        const scrollAxis = isHorizontal(root.swipeDirection) ? 'horizontal' : 'vertical';
        const crossAxis = scrollAxis === 'vertical' ? 'horizontal' : 'vertical';
        const scrollTarget = target ? findScrollableTouchTarget(target, element, scrollAxis) : null;
        const hasCrossAxisScrollableContent =
            target ? findScrollableTouchTarget(target, element, crossAxis) !== null : false;
        let allowSwipe = null;
        if (scrollTarget) {
            allowSwipe = isAtSwipeStartEdge(scrollTarget, scrollAxis, root.swipeDirection) ? null : false;
        }

        root.touchScrollState = {
            startX: touch.clientX,
            startY: touch.clientY,
            lastX: touch.clientX,
            lastY: touch.clientY,
            scrollTarget,
            hasCrossAxisScrollableContent,
            allowSwipe,
            preserveNativeCrossAxisScroll: false
        };
        root.dismissSwipe?.start(event, element);
    };

    const onTouchMove = (event) => {
        virtualKeyboardTouchMove(root, event);
        if (root.ignoreTouchSwipe) {
            return;
        }

        const touchState = root.touchScrollState;
        const touch = event.touches[0];
        if (!touch || !touchState) {
            return;
        }

        processViewportTouchMove(root, event, touchState, touch, element);
        touchState.lastX = touch.clientX;
        touchState.lastY = touch.clientY;
    };

    const onTouchEnd = (event) => {
        virtualKeyboardTouchEnd(root, event, element);
        root.dismissSwipe?.end(event, element);
        resetTouchTrackingState(root);
    };

    const onTouchCancel = (event) => {
        virtualKeyboardTouchCancel(root);
        root.dismissSwipe?.end(event, element);
        resetTouchTrackingState(root);
    };

    element.addEventListener('pointerdown', onPointerDown, { signal });
    element.addEventListener('touchstart', onTouchStart, { signal, passive: true });
    element.addEventListener('touchend', onTouchEnd, { signal, passive: false });
    element.addEventListener('touchcancel', onTouchCancel, { signal, passive: true });
    doc.addEventListener('touchmove', onTouchMove, { signal, passive: false, capture: true });

    root.viewportCleanup = () => {
        cleanupPointerDocument();
        abortController.abort();
        resetTouchTrackingState(root);
        setSwiping(root, false);
        applySwipeProgress(root, 0, false);
    };
    root.viewportBoundElement = element;
    root.viewportBoundPopupElement = root.popupElement;
    root.viewportBoundDotNetRef = root.viewportDotNetRef;
}

function createViewportSwipeOptions(root) {
    const hasVerticalSnapPoints = () =>
        Array.isArray(root.snapPoints) &&
        root.snapPoints.length > 0 &&
        (root.swipeDirection === 'down' || root.swipeDirection === 'up');

    return {
        enabled: () => root.mounted && !hasOpenDescendant(root),
        directions: () => hasVerticalSnapPoints()
            ? (root.swipeDirection === 'down' ? ['down', 'up'] : ['up', 'down'])
            : [root.swipeDirection],
        element: () => root.popupElement,
        movementCssVars: {
            x: '--drawer-swipe-movement-x',
            y: '--drawer-swipe-movement-y'
        },
        ignoreSelectorWhenTouch: false,
        ignoreScrollableAncestors: true,
        trackDrag: true,
        swipeThreshold(element, direction) {
            return getBaseSwipeThreshold(element, direction);
        },
        canStart(position, details) {
            const popup = root.popupElement;
            if (!popup) {
                return false;
            }

            const doc = popup.ownerDocument;
            const target = getElementAtPoint(doc, position.x, position.y);
            if (!target || !contains(popup, target)) {
                return false;
            }

            const touchLike = isTouchEvent(details.nativeEvent) ||
                details.nativeEvent.pointerType === 'touch';
            if (touchLike && shouldIgnoreSwipeForTextSelection(doc, popup)) {
                return false;
            }

            if (details.nativeEvent.type === 'touchstart' && isSwipeIgnoredTarget(target)) {
                return false;
            }

            return true;
        },
        onSwipeStart(event) {
            if (isTouchEvent(event) || event.pointerType === 'touch') {
                return;
            }

            const popup = root.popupElement;
            const selection = popup?.ownerDocument?.getSelection?.();
            if (!popup || !selection || selection.isCollapsed) {
                return;
            }

            const anchor = selection.anchorNode instanceof Element
                ? selection.anchorNode
                : selection.anchorNode?.parentElement;
            const focus = selection.focusNode instanceof Element
                ? selection.focusNode
                : selection.focusNode?.parentElement;
            if (contains(popup, anchor) || contains(popup, focus)) {
                selection.removeAllRanges();
            }
        },
        onSwipingChange(swiping) {
            setSwiping(root, swiping);
        },
        onProgress(progress, details) {
            updateViewportProgress(root, progress, details);
        },
        onRelease(details) {
            return releaseViewportSwipe(root, details);
        },
        onDismiss(event) {
            dismissViewportFromSwipe(root, event);
        },
        onCancel() {
            clearSwipeRelease(root);
            applySwipeProgress(root, 0, true);
            notifyFinalProgress(root, 0);
        }
    };
}

function createSwipeDismiss(options) {
    const stateRef = {
        currentDirection: undefined,
        isSwiping: false,
        dragDismissed: false,
        dragStart: { x: 0, y: 0 },
        dragOffset: { x: 0, y: 0 },
        lastMove: null,
        initialTransform: { x: 0, y: 0, scale: 1 },
        intendedDirection: undefined,
        maxDisplacement: 0,
        cancelled: false,
        cancelBaseline: { x: 0, y: 0 },
        lockedDirection: null,
        firstMove: false,
        pending: false,
        pendingStart: null,
        swipeFromScrollable: false,
        sawPrimaryButtons: false,
        elementSize: { width: 0, height: 0 },
        threshold: DEFAULT_SWIPE_THRESHOLD,
        startTime: null,
        lastDragSample: null,
        lastDragVelocity: { x: 0, y: 0 },
        progress: 0,
        lastDetails: null,
        styleSnapshot: null
    };

    const enabled = () => typeof options.enabled === 'function' ? options.enabled() : !!options.enabled;
    const directions = () => typeof options.directions === 'function' ? options.directions() : options.directions;
    const element = () => options.element();

    function setSwiping(next) {
        if (stateRef.isSwiping === next) {
            return;
        }
        stateRef.isSwiping = next;
        options.onSwipingChange?.(next);
    }

    function resolveThreshold(direction) {
        const target = element();
        const value = target && typeof options.swipeThreshold === 'function'
            ? options.swipeThreshold(target, direction)
            : options.swipeThreshold;
        stateRef.threshold = Math.max(0, Number.isFinite(value) ? value : DEFAULT_SWIPE_THRESHOLD);
    }

    function updateProgress(progress, details) {
        const next = Number.isFinite(progress) ? clamp(progress, 0, 1) : 0;
        const progressChanged = next !== stateRef.progress;
        const last = stateRef.lastDetails;
        const detailsChanged = details && (
            !last ||
            last.deltaX !== details.deltaX ||
            last.deltaY !== details.deltaY ||
            last.direction !== details.direction
        );
        if (!progressChanged && !detailsChanged) {
            return;
        }
        stateRef.progress = next;
        stateRef.lastDetails = details || (progressChanged ? null : stateRef.lastDetails);
        options.onProgress?.(next, details);
    }

    function syncDragStyles(swiping) {
        const target = element();
        if (!options.trackDrag || !target) {
            if (!swiping) {
                stateRef.styleSnapshot = null;
            }
            return;
        }

        const style = target.style;
        if (swiping) {
            stateRef.styleSnapshot ??= [style.transition, style.transform];
            style.transition = 'none';
            style.transform = getDragTransform(stateRef.dragOffset, stateRef.initialTransform.scale);
        } else if (stateRef.styleSnapshot) {
            [style.transition, style.transform] = stateRef.styleSnapshot;
            stateRef.styleSnapshot = null;
        }

        const deltaX = stateRef.dragOffset.x - stateRef.initialTransform.x;
        const deltaY = stateRef.dragOffset.y - stateRef.initialTransform.y;
        style.setProperty(options.movementCssVars.x, deltaX + 'px');
        style.setProperty(options.movementCssVars.y, deltaY + 'px');
    }

    function recordSample(offset, timeStamp) {
        const time = validTimeStamp(timeStamp);
        if (time === null) {
            return;
        }
        const last = stateRef.lastDragSample;
        if (last && time > last.time) {
            const duration = Math.max(time - last.time, MIN_RELEASE_VELOCITY_DURATION_MS);
            stateRef.lastDragVelocity = {
                x: (offset.x - last.x) / duration,
                y: (offset.y - last.y) / duration
            };
        }
        stateRef.lastDragSample = { x: offset.x, y: offset.y, time };
    }

    function reset() {
        setSwiping(false);
        updateProgress(0);
        stateRef.currentDirection = undefined;
        stateRef.dragDismissed = false;
        stateRef.dragStart = { x: 0, y: 0 };
        stateRef.dragOffset = { x: 0, y: 0 };
        stateRef.lastMove = null;
        stateRef.initialTransform = { x: 0, y: 0, scale: 1 };
        stateRef.intendedDirection = undefined;
        stateRef.maxDisplacement = 0;
        stateRef.cancelled = false;
        stateRef.cancelBaseline = { x: 0, y: 0 };
        stateRef.lockedDirection = null;
        stateRef.firstMove = false;
        stateRef.pending = false;
        stateRef.pendingStart = null;
        stateRef.swipeFromScrollable = false;
        stateRef.sawPrimaryButtons = false;
        stateRef.elementSize = { width: 0, height: 0 };
        stateRef.threshold = DEFAULT_SWIPE_THRESHOLD;
        stateRef.startTime = null;
        stateRef.lastDragSample = null;
        stateRef.lastDragVelocity = { x: 0, y: 0 };
        stateRef.lastDetails = null;
        syncDragStyles(false);
    }

    function clearPending() {
        stateRef.pending = false;
        stateRef.pendingStart = null;
    }

    function resetPending() {
        clearPending();
        stateRef.swipeFromScrollable = false;
        stateRef.lastMove = null;
    }

    function startAt(event, currentTarget, position, startOptions = {}) {
        stateRef.swipeFromScrollable = false;
        const touchLike = isTouchLikeEvent(event);
        const target = getTargetAtPoint(element(), position, event);
        const doc = element()?.ownerDocument || document;
        const dirs = directions();
        const hasHorizontal = dirs.some(isHorizontal);
        const hasVertical = dirs.some((direction) => !isHorizontal(direction));
        const scrollTarget = touchLike && doc.body
            ? findGestureScrollableTouchTarget(target, doc.body, hasHorizontal, hasVertical)
            : null;
        if (scrollTarget && !startOptions.ignoreScrollableTarget) {
            return false;
        }
        stateRef.swipeFromScrollable = !!(scrollTarget && startOptions.ignoreScrollableTarget);

        const interactive = target?.closest?.(DEFAULT_IGNORE_SELECTOR);
        if (interactive && (!touchLike || options.ignoreSelectorWhenTouch !== false)) {
            return false;
        }

        if (options.ignoreScrollableAncestors && target && element()) {
            const axes = [];
            if (hasVertical) axes.push('vertical');
            if (hasHorizontal) axes.push('horizontal');
            if (!startOptions.ignoreScrollableAncestors && hasScrollableAncestor(target, element(), axes)) {
                return false;
            }
        }

        stateRef.cancelled = false;
        stateRef.intendedDirection = undefined;
        stateRef.maxDisplacement = 0;
        stateRef.dragStart = position;
        stateRef.startTime = validTimeStamp(event.timeStamp);
        stateRef.cancelBaseline = position;
        stateRef.lastMove = position;

        const targetElement = element();
        if (targetElement) {
            stateRef.elementSize = {
                width: targetElement.offsetWidth,
                height: targetElement.offsetHeight
            };
            const primary = dirs.length === 1 ? dirs[0] : undefined;
            resolveThreshold(primary);
            stateRef.initialTransform = getElementTransform(targetElement);
            stateRef.dragOffset = {
                x: stateRef.initialTransform.x,
                y: stateRef.initialTransform.y
            };
            recordSample(stateRef.dragOffset, stateRef.startTime);
            if (!isTouchEvent(event)) {
                safelyChangePointerCapture(targetElement, event.pointerId, 'setPointerCapture');
            }
        }

        options.onSwipeStart?.(event);
        setSwiping(true);
        stateRef.lockedDirection = null;
        stateRef.firstMove = true;
        updateProgress(0);
        syncDragStyles(true);
        return true;
    }

    function start(event, currentTarget) {
        if (!enabled() || event.defaultPrevented || (!isTouchEvent(event) && event.button !== 0)) {
            return;
        }
        const position = getPrimaryPosition(event);
        if (!position) {
            return;
        }

        stateRef.pending = true;
        stateRef.pendingStart = position;
        stateRef.swipeFromScrollable = false;
        stateRef.sawPrimaryButtons = !isTouchEvent(event);
        const primary = directions().length === 1 ? directions()[0] : undefined;
        const allowed = options.canStart
            ? options.canStart(position, { nativeEvent: event, direction: primary })
            : true;
        if (allowed && startAt(event, currentTarget, position)) {
            clearPending();
        }
    }

    function cancel(event) {
        resetPending();
        if (!stateRef.isSwiping) {
            return;
        }

        setSwiping(false);
        stateRef.lockedDirection = null;
        stateRef.dragOffset = {
            x: stateRef.initialTransform.x,
            y: stateRef.initialTransform.y
        };
        stateRef.currentDirection = undefined;
        stateRef.sawPrimaryButtons = false;
        syncDragStyles(false);
        safelyChangePointerCapture(element(), event.pointerId, 'releasePointerCapture');
        updateProgress(0, { deltaX: 0, deltaY: 0, direction: undefined });
        options.onCancel?.(event);
    }

    function moveCore(event, currentTarget, position, movement) {
        if (!enabled() || !stateRef.isSwiping) {
            return;
        }

        const dirs = directions();
        const allowLeft = dirs.includes('left');
        const allowRight = dirs.includes('right');
        const allowUp = dirs.includes('up');
        const allowDown = dirs.includes('down');
        const hasHorizontal = allowLeft || allowRight;
        const hasVertical = allowUp || allowDown;
        const target = getEventTarget(event);
        if (isTouchLikeEvent(event) && !stateRef.swipeFromScrollable) {
            if (findGestureScrollableTouchTarget(target, currentTarget, hasHorizontal, hasVertical)) {
                return;
            }
        }

        if (!isTouchEvent(event) && event.cancelable) {
            event.preventDefault();
        }

        if (stateRef.firstMove) {
            stateRef.firstMove = false;
            if (options.trackDrag) {
                stateRef.dragStart = position;
                const moveTime = validTimeStamp(event.timeStamp);
                if (moveTime !== null) {
                    stateRef.startTime = moveTime;
                }
            }
        }

        if (
            (movement.y < 0 && position.y > stateRef.cancelBaseline.y) ||
            (movement.y > 0 && position.y < stateRef.cancelBaseline.y)
        ) {
            stateRef.cancelBaseline.y = position.y;
        }
        if (
            (movement.x < 0 && position.x > stateRef.cancelBaseline.x) ||
            (movement.x > 0 && position.x < stateRef.cancelBaseline.x)
        ) {
            stateRef.cancelBaseline.x = position.x;
        }

        const deltaX = position.x - stateRef.dragStart.x;
        const deltaY = position.y - stateRef.dragStart.y;
        const cancelDeltaX = position.x - stateRef.cancelBaseline.x;
        const cancelDeltaY = position.y - stateRef.cancelBaseline.y;

        if (stateRef.lockedDirection === null && hasHorizontal && hasVertical) {
            const distance = Math.sqrt(deltaX * deltaX + deltaY * deltaY);
            if (distance >= MIN_DRAG_THRESHOLD) {
                stateRef.lockedDirection = Math.abs(deltaX) > Math.abs(deltaY) ? 'horizontal' : 'vertical';
            }
        }

        if (!stateRef.intendedDirection) {
            let candidate;
            if (stateRef.lockedDirection === 'vertical') {
                candidate = deltaY > 0 ? 'down' : deltaY < 0 ? 'up' : undefined;
            } else if (stateRef.lockedDirection === 'horizontal') {
                candidate = deltaX > 0 ? 'right' : deltaX < 0 ? 'left' : undefined;
            } else if (Math.abs(deltaX) >= Math.abs(deltaY)) {
                candidate = deltaX > 0 ? 'right' : deltaX < 0 ? 'left' : undefined;
            } else {
                candidate = deltaY > 0 ? 'down' : deltaY < 0 ? 'up' : undefined;
            }

            if (candidate && dirs.includes(candidate)) {
                stateRef.intendedDirection = candidate;
                stateRef.maxDisplacement = getDisplacement(candidate, deltaX, deltaY);
                stateRef.currentDirection = candidate;
                resolveThreshold(candidate);
            }
        } else {
            const direction = stateRef.intendedDirection;
            const current = getDisplacement(direction, cancelDeltaX, cancelDeltaY);
            if (current > stateRef.threshold) {
                stateRef.cancelled = false;
                stateRef.currentDirection = direction;
            } else if (
                !(allowLeft && allowRight) &&
                !(allowUp && allowDown) &&
                stateRef.maxDisplacement - current >= REVERSE_CANCEL_THRESHOLD
            ) {
                stateRef.cancelled = true;
            }
        }

        const damped = applyDirectionalDamping(
            deltaX,
            deltaY,
            hasHorizontal,
            hasVertical,
            allowLeft,
            allowRight,
            allowUp,
            allowDown
        );
        let offsetX = stateRef.initialTransform.x;
        let offsetY = stateRef.initialTransform.y;
        if (stateRef.lockedDirection === 'horizontal') {
            if (hasHorizontal) offsetX += damped.x;
        } else if (stateRef.lockedDirection === 'vertical') {
            if (hasVertical) offsetY += damped.y;
        } else {
            if (hasHorizontal) offsetX += damped.x;
            if (hasVertical) offsetY += damped.y;
        }

        stateRef.dragOffset = { x: offsetX, y: offsetY };
        syncDragStyles(true);
        recordSample(stateRef.dragOffset, event.timeStamp);

        const dragDeltaX = offsetX - stateRef.initialTransform.x;
        const dragDeltaY = offsetY - stateRef.initialTransform.y;
        const details = {
            deltaX: dragDeltaX,
            deltaY: dragDeltaY,
            direction: stateRef.intendedDirection
        };
        const primary = dirs.length === 1 ? dirs[0] : stateRef.intendedDirection;
        let progress = 0;
        if (primary) {
            const size = isHorizontal(primary) ? stateRef.elementSize.width : stateRef.elementSize.height;
            const scale = stateRef.initialTransform.scale || 1;
            const displacement = getDisplacement(primary, dragDeltaX, dragDeltaY);
            if (size > 0 && scale > 0 && displacement > 0) {
                progress = displacement / (size * scale);
            }
        }
        updateProgress(progress, details);
    }

    function move(event, currentTarget) {
        const position = getPrimaryPosition(event);
        if (!position) {
            return;
        }

        let endAfterMove = false;
        if (!isTouchEvent(event)) {
            const primary = hasPrimaryMouseButton(event.buttons);
            if (primary) {
                stateRef.sawPrimaryButtons = true;
            }
            if (event.buttons !== 0 && !primary) {
                cancel(event);
                return;
            }
            if (event.buttons === 0 && stateRef.sawPrimaryButtons) {
                if (!stateRef.isSwiping) {
                    end(event, currentTarget);
                    return;
                }
                endAfterMove = true;
            }
        }

        if (!stateRef.isSwiping && stateRef.pending) {
            if (!isTouchLikeEvent(event) && event.defaultPrevented) {
                resetPending();
                return;
            }

            const primary = directions().length === 1 ? directions()[0] : undefined;
            const allowed = options.canStart
                ? options.canStart(position, { nativeEvent: event, direction: primary })
                : true;
            if (allowed) {
                const pendingStart = stateRef.pendingStart;
                let ignoreScrollable = false;
                if (isTouchLikeEvent(event) && pendingStart && element()) {
                    const target = getTargetAtPoint(element(), position, event);
                    const dirs = directions();
                    const hasHorizontal = dirs.some(isHorizontal);
                    const hasVertical = dirs.some((direction) => !isHorizontal(direction));
                    const scrollTarget = findGestureScrollableTouchTarget(
                        target,
                        element().ownerDocument.body,
                        hasHorizontal,
                        hasVertical
                    );
                    if (scrollTarget && (contains(element(), scrollTarget) || contains(scrollTarget, element()))) {
                        const canSwipe = canSwipeFromPendingScrollEdge(
                            scrollTarget,
                            position.x - pendingStart.x,
                            position.y - pendingStart.y,
                            dirs
                        );
                        if (canSwipe === false) {
                            return;
                        }
                        ignoreScrollable = canSwipe === true;
                    }
                }

                const started = startAt(event, currentTarget, position, {
                    ignoreScrollableTarget: ignoreScrollable,
                    ignoreScrollableAncestors: ignoreScrollable
                });
                if (started) {
                    clearPending();
                    if (pendingStart && ignoreScrollable) {
                        stateRef.dragStart = pendingStart;
                        stateRef.cancelBaseline = { ...pendingStart };
                        stateRef.lastMove = pendingStart;
                        stateRef.firstMove = false;
                    } else {
                        stateRef.swipeFromScrollable = false;
                    }
                }
            }
        }

        const previous = stateRef.lastMove;
        const movement = previous
            ? { x: position.x - previous.x, y: position.y - previous.y }
            : { x: 0, y: 0 };
        stateRef.lastMove = position;
        moveCore(event, currentTarget, position, movement);
        if (endAfterMove && !isTouchEvent(event)) {
            end(event, currentTarget);
        }
    }

    function end(event) {
        if (!enabled()) {
            return;
        }

        const initial = stateRef.initialTransform;
        const resolved = stateRef.dragOffset;
        const deltaX = resolved.x - initial.x;
        const deltaY = resolved.y - initial.y;
        const progressDetails = {
            deltaX,
            deltaY,
            direction: stateRef.intendedDirection
        };

        if (!stateRef.isSwiping) {
            resetPending();
            updateProgress(0, progressDetails);
            return;
        }

        setSwiping(false);
        stateRef.lockedDirection = null;
        resetPending();
        stateRef.sawPrimaryButtons = false;
        if (!isTouchEvent(event)) {
            safelyChangePointerCapture(element(), event.pointerId, 'releasePointerCapture');
        }

        const startTime = stateRef.startTime;
        const endTime = validTimeStamp(event.timeStamp);
        const duration = startTime !== null && endTime !== null && endTime > startTime
            ? endTime - startTime
            : 0;
        const velocityDuration = duration > 0 ? Math.max(duration, MIN_VELOCITY_DURATION_MS) : 0;
        const velocityX = velocityDuration > 0 ? deltaX / velocityDuration : 0;
        const velocityY = velocityDuration > 0 ? deltaY / velocityDuration : 0;
        let releaseVelocityX = stateRef.lastDragVelocity.x;
        let releaseVelocityY = stateRef.lastDragVelocity.y;
        const last = stateRef.lastDragSample;
        if (last && endTime !== null && endTime >= last.time) {
            const age = endTime - last.time;
            if (age <= MAX_RELEASE_VELOCITY_AGE_MS) {
                const sampleDuration = Math.max(age, MIN_RELEASE_VELOCITY_DURATION_MS);
                const sampleX = (resolved.x - last.x) / sampleDuration;
                const sampleY = (resolved.y - last.y) / sampleDuration;
                if (sampleX !== 0) releaseVelocityX = sampleX;
                if (sampleY !== 0) releaseVelocityY = sampleY;
            } else {
                releaseVelocityX = 0;
                releaseVelocityY = 0;
            }
        }

        const decision = options.onRelease?.({
            event,
            direction: stateRef.intendedDirection,
            deltaX,
            deltaY,
            velocityX,
            velocityY,
            releaseVelocityX,
            releaseVelocityY
        });
        const hasDecision = typeof decision === 'boolean';
        if (stateRef.cancelled && !hasDecision) {
            stateRef.dragOffset = { x: initial.x, y: initial.y };
            stateRef.currentDirection = undefined;
            syncDragStyles(false);
            updateProgress(0, progressDetails);
            return;
        }

        let shouldClose = false;
        let dismissDirection;
        if (hasDecision) {
            shouldClose = decision;
            dismissDirection = stateRef.intendedDirection ?? (directions().length === 1 ? directions()[0] : undefined);
        } else {
            for (const direction of directions()) {
                if (getDisplacement(direction, deltaX, deltaY) > stateRef.threshold) {
                    shouldClose = true;
                    dismissDirection = direction;
                    break;
                }
            }
        }

        if (shouldClose && dismissDirection) {
            stateRef.currentDirection = dismissDirection;
            stateRef.dragDismissed = true;
            syncDragStyles(false);
            options.onDismiss?.(event, { direction: dismissDirection });
        } else {
            stateRef.dragOffset = { x: initial.x, y: initial.y };
            stateRef.currentDirection = undefined;
            syncDragStyles(false);
            updateProgress(0, progressDetails);
        }
    }

    return {
        start,
        move,
        moveNative: move,
        end,
        reset,
        get swiping() {
            return stateRef.isSwiping;
        },
        get direction() {
            return stateRef.currentDirection;
        }
    };
}

function updateViewportProgress(root, progress, details) {
    updateNestedSwipeActive(root, details);

    const points = resolveSnapPoints(root);
    const hasSnapPoints = points.length > 0;
    if (
        root.swiping &&
        root.swipeDirection === 'down' &&
        hasSnapPoints &&
        details &&
        Number.isFinite(details.deltaY) &&
        root.popupElement
    ) {
        root.popupElement.style.removeProperty('transform');
        root.popupElement.style.setProperty(
            '--drawer-swipe-movement-y',
            getSnapPointSwipeMovement(root.activeSnapPointOffset ?? 0, details.deltaY) + 'px'
        );
    }

    const currentDirection = details?.direction ?? root.dismissSwipe?.direction;
    const isDismissSwipe = currentDirection === undefined || currentDirection === root.swipeDirection;
    const vertical = !isHorizontal(root.swipeDirection);
    const shouldTrack =
        (hasSnapPoints && vertical) ||
        !hasSnapPoints ||
        isHorizontal(root.swipeDirection) ||
        isDismissSwipe;
    let resolvedProgress = progress;
    const range = getSnapPointRange(points);
    if (range && root.popupHeight > 0) {
        const baseOffset = root.activeSnapPointOffset ?? range.minOffset;
        const offsetToProgress = (offset) => clamp((offset - range.minOffset) / range.range, 0, 1);
        if (details && Number.isFinite(details.deltaY)) {
            resolvedProgress = offsetToProgress(clamp(baseOffset + details.deltaY, 0, root.popupHeight));
        } else {
            resolvedProgress = offsetToProgress(baseOffset);
        }
    }

    applySwipeProgress(root, resolvedProgress, shouldTrack);
}

function releaseViewportSwipe(root, details) {
    const {
        event,
        deltaX,
        deltaY,
        direction,
        velocityX,
        velocityY,
        releaseVelocityX,
        releaseVelocityY
    } = details;
    const points = resolveSnapPoints(root);

    if (points.length === 0) {
        const popup = root.popupElement;
        if (!direction || !popup) {
            clearSwipeRelease(root);
            notifyFinalProgress(root, 0);
            return undefined;
        }
        const directionalDelta = getDisplacement(direction, deltaX, deltaY);
        if (!Number.isFinite(directionalDelta)) {
            clearSwipeRelease(root);
            notifyFinalProgress(root, 0);
            return undefined;
        }
        if (directionalDelta <= 0) {
            clearSwipeRelease(root);
            notifyFinalProgress(root, 0);
            return false;
        }
        if (getDisplacement(direction, velocityX, velocityY) >= FAST_SWIPE_VELOCITY) {
            startSwipeRelease(root, direction, details);
            return true;
        }
        const shouldClose = directionalDelta > getBaseSwipeThreshold(popup, direction);
        if (shouldClose) {
            startSwipeRelease(root, direction, details);
        } else {
            clearSwipeRelease(root);
            notifyFinalProgress(root, 0);
        }
        return shouldClose;
    }

    if (isHorizontal(root.swipeDirection) || !root.popupHeight) {
        clearSwipeRelease(root);
        notifyFinalProgress(root, 0);
        return undefined;
    }

    const dragDelta = root.swipeDirection === 'down' ? deltaY : -deltaY;
    if (!Number.isFinite(dragDelta)) {
        clearSwipeRelease(root);
        notifyFinalProgress(root, 0);
        return undefined;
    }

    const dragDirection = Math.sign(dragDelta);
    const releaseDirectionalVelocity =
        root.swipeDirection === 'down' ? releaseVelocityY : -releaseVelocityY;
    const fallbackVelocity = root.swipeDirection === 'down' ? velocityY : -velocityY;
    let resolvedVelocity = Number.isFinite(releaseDirectionalVelocity)
        ? releaseDirectionalVelocity
        : fallbackVelocity;
    if (
        dragDirection !== 0 &&
        Math.abs(dragDelta) >= MIN_SWIPE_THRESHOLD &&
        Number.isFinite(resolvedVelocity)
    ) {
        const velocityDirection = Math.sign(resolvedVelocity);
        if (velocityDirection !== 0 && velocityDirection !== dragDirection) {
            resolvedVelocity = fallbackVelocity;
        }
    }

    const currentOffset = root.activeSnapPointOffset ?? 0;
    const dragTargetOffset = clamp(currentOffset + dragDelta, 0, root.popupHeight);
    const velocityOffset =
        Number.isFinite(resolvedVelocity) && Math.abs(resolvedVelocity) >= SNAP_VELOCITY_THRESHOLD
            ? clamp(resolvedVelocity, -MAX_SNAP_VELOCITY, MAX_SNAP_VELOCITY) * SNAP_VELOCITY_MULTIPLIER
            : 0;
    const targetOffset = root.snapToSequentialPoints
        ? dragTargetOffset
        : clamp(dragTargetOffset + velocityOffset, 0, root.popupHeight);

    const closeFromSnapPoints = () => {
        root.pendingSwipeCloseSnapPoint = root.activeSnapPoint;
        invoke(root.viewportDotNetRef, 'OnSnapPointChange', null);
        startSwipeRelease(root, root.swipeDirection, details);
        return true;
    };

    if (root.snapToSequentialPoints) {
        const ordered = [...points].sort((first, second) => first.offset - second.offset);
        const offsets = ordered.map((point) => point.offset);
        const currentIndex = closestSnapPointIndex(offsets, currentOffset);
        let targetPoint = ordered[closestSnapPointIndex(offsets, targetOffset)];
        const velocityDirection = Math.sign(resolvedVelocity);
        const shouldAdvance =
            dragDirection !== 0 &&
            velocityDirection !== 0 &&
            velocityDirection === dragDirection &&
            Math.abs(resolvedVelocity) >= SNAP_VELOCITY_THRESHOLD;
        let effectiveTargetOffset = targetOffset;

        if (shouldAdvance) {
            const adjacentIndex = clamp(currentIndex + dragDirection, 0, ordered.length - 1);
            if (adjacentIndex !== currentIndex) {
                const adjacent = ordered[adjacentIndex];
                const forceAdjacent = dragDirection > 0
                    ? targetOffset < adjacent.offset
                    : targetOffset > adjacent.offset;
                if (forceAdjacent) {
                    targetPoint = adjacent;
                    effectiveTargetOffset = adjacent.offset;
                }
            } else if (dragDirection > 0) {
                return closeFromSnapPoints();
            }
        }

        const closeDistance = Math.abs(effectiveTargetOffset - root.popupHeight);
        const snapDistance = Math.abs(effectiveTargetOffset - targetPoint.offset);
        if (closeDistance < snapDistance) {
            return closeFromSnapPoints();
        }

        invoke(root.viewportDotNetRef, 'OnSnapPointChange', targetPoint.value);
        clearSwipeRelease(root);
        const settledProgress = progressForSnapPoint(root, targetPoint.offset);
        applySwipeProgress(root, settledProgress, true, false);
        notifyFinalProgress(root, settledProgress);
        return false;
    }

    if (resolvedVelocity >= FAST_SWIPE_VELOCITY && dragDelta > 0) {
        return closeFromSnapPoints();
    }

    const targetPoint = points[closestSnapPointIndex(points.map((point) => point.offset), targetOffset)];
    const closeDistance = Math.abs(targetOffset - root.popupHeight);
    if (closeDistance < Math.abs(targetOffset - targetPoint.offset)) {
        return closeFromSnapPoints();
    }

    invoke(root.viewportDotNetRef, 'OnSnapPointChange', targetPoint.value);
    clearSwipeRelease(root);
    const settledProgress = progressForSnapPoint(root, targetPoint.offset);
    applySwipeProgress(root, settledProgress, true, false);
    notifyFinalProgress(root, settledProgress);
    return false;
}

async function dismissViewportFromSwipe(root, event) {
    setProviderVisual(root, 0, 0);
    syncParentProgress(root, 0);
    applyBackdropProgress(root, 0);
    const accepted = await invoke(root.viewportDotNetRef, 'OnSwipeDismiss');
    if (accepted === false) {
        await restorePendingSnapPoint(root);
        clearSwipeRelease(root);
        root.dismissSwipe?.reset();
        notifyFinalProgress(root, 0);
        return;
    }

    if (accepted === true) {
        root.pendingSwipeCloseSnapPoint = undefined;
        setSwipeDismissed(root, true);
        notifyFinalProgress(root, 0);
        return;
    }

    if (accepted === undefined) {
        requestAnimationFrame(async () => {
            if (root.isOpen) {
                await restorePendingSnapPoint(root);
                clearSwipeRelease(root);
                root.dismissSwipe?.reset();
                notifyFinalProgress(root, 0);
            } else {
                root.pendingSwipeCloseSnapPoint = undefined;
                setSwipeDismissed(root, true);
                notifyFinalProgress(root, 0);
            }
        });
    }
}

async function restorePendingSnapPoint(root) {
    const pending = root.pendingSwipeCloseSnapPoint;
    root.pendingSwipeCloseSnapPoint = undefined;
    if (pending !== undefined) {
        await invoke(root.viewportDotNetRef, 'OnSnapPointChange', pending);
    }
}

function startSwipeRelease(root, direction, details) {
    const popup = root.popupElement;
    if (!popup) {
        return;
    }

    setSwipeDismissed(root, true);
    popup.style.removeProperty('transition');
    popup.setAttribute(DATA_ENDING_STYLE, '');
    const strength = resolveSwipeRelease(root, direction, details);
    popup.style.setProperty('--drawer-swipe-strength', strength === null ? '1' : String(strength));
    invoke(root.viewportDotNetRef, 'OnSwipeRelease', strength);
}

function clearSwipeRelease(root) {
    setSwipeDismissed(root, false);
    const popup = root.popupElement;
    popup?.removeAttribute(DATA_ENDING_STYLE);
    popup?.style.setProperty('--drawer-swipe-strength', '1');
    invoke(root.viewportDotNetRef, 'OnSwipeRelease', null);
}

function resolveSwipeRelease(root, direction, details) {
    const popup = root.popupElement;
    if (!popup) {
        return null;
    }
    const size = getBaseSwipeSize(popup, direction);
    if (!Number.isFinite(size) || size <= 0) {
        return null;
    }

    const snapOffset =
        !isHorizontal(direction) && Array.isArray(root.snapPoints) && root.snapPoints.length > 0
            ? (root.activeSnapPointOffset ?? 0)
            : 0;
    const translation = snapOffset + getDisplacement(direction, details.deltaX, details.deltaY);
    const remaining = Math.max(0, size - translation);
    if (!Number.isFinite(remaining) || remaining <= 0) {
        return null;
    }

    const releaseVelocity = getDisplacement(
        direction,
        details.releaseVelocityX,
        details.releaseVelocityY
    );
    const velocity = Math.abs(releaseVelocity) > 0 && Number.isFinite(releaseVelocity)
        ? releaseVelocity
        : getDisplacement(direction, details.velocityX, details.velocityY);
    if (!Number.isFinite(velocity) || velocity <= MIN_SWIPE_RELEASE_VELOCITY) {
        return null;
    }

    const clampedVelocity = clamp(
        velocity,
        MIN_SWIPE_RELEASE_VELOCITY,
        MAX_SWIPE_RELEASE_VELOCITY
    );
    const duration = clamp(
        remaining / clampedVelocity,
        MIN_SWIPE_RELEASE_DURATION_MS,
        MAX_SWIPE_RELEASE_DURATION_MS
    );
    const normalized =
        (duration - MIN_SWIPE_RELEASE_DURATION_MS) /
        (MAX_SWIPE_RELEASE_DURATION_MS - MIN_SWIPE_RELEASE_DURATION_MS);
    return MIN_SWIPE_RELEASE_SCALAR +
        normalized * (MAX_SWIPE_RELEASE_SCALAR - MIN_SWIPE_RELEASE_SCALAR);
}

function setSwiping(root, swiping) {
    if (root.swiping === swiping) {
        return;
    }
    root.swiping = swiping;
    root.popupElement?.toggleAttribute(DATA_SWIPING, swiping);
    root.backdropElement?.toggleAttribute(DATA_SWIPING, swiping);
    if (!swiping) {
        finishNestedSwipe(root);
    }
    invoke(root.viewportDotNetRef, 'OnSwipingChanged', swiping);
}

function updateNestedSwipeActive(root, details) {
    if (root.nestedSwipeActive || !root.parentRootId || !details) {
        return;
    }

    const direction = details.direction ?? root.swipeDirection;
    const displacement = getDisplacement(direction, details.deltaX, details.deltaY);
    if (!Number.isFinite(displacement) || Math.abs(displacement) < MIN_SWIPE_THRESHOLD) {
        return;
    }

    root.nestedSwipeActive = true;
    const parent = state.roots.get(root.parentRootId);
    parent?.popupElement?.setAttribute('data-nested-drawer-swiping', '');
    invoke(root.viewportDotNetRef, 'OnNestedSwipingChanged', true);
}

function finishNestedSwipe(root) {
    if (!root.nestedSwipeActive) {
        return;
    }

    root.nestedSwipeActive = false;
    const parent = state.roots.get(root.parentRootId);
    parent?.popupElement?.removeAttribute('data-nested-drawer-swiping');
    invoke(root.viewportDotNetRef, 'OnNestedSwipingChanged', false);
}

function applySwipeProgress(root, progress, shouldTrack, notifyParent = true) {
    const resolved = Number.isFinite(progress) ? clamp(progress, 0, 1) : 0;
    const activeProgress = root.isOpen && !root.parentRootId && shouldTrack ? resolved : 0;
    const nestedProgress = root.isOpen && shouldTrack ? resolved : 0;
    applyBackdropProgress(root, activeProgress);
    setProviderVisual(
        root,
        activeProgress,
        activeProgress > 0 ? root.frontmostHeight : 0
    );
    if (notifyParent) {
        syncParentProgress(root, nestedProgress);
    }
    if (nestedProgress <= 0) {
        finishNestedSwipe(root);
    }
}

function applySettledSnapPointProgress(root) {
    const range = getSnapPointRange(resolveSnapPoints(root));
    if (!range || root.swiping) {
        return;
    }

    const progress = root.activeSnapPointOffset === null
        ? 0
        : progressForSnapPoint(root, root.activeSnapPointOffset);
    applySwipeProgress(root, progress, true, false);
}

function notifyFinalProgress(root, progress) {
    const resolved = Number.isFinite(progress) ? clamp(progress, 0, 1) : 0;
    invoke(root.viewportDotNetRef, 'OnSwipeProgress', resolved);
}

function applyBackdropProgress(root, progress) {
    const backdrop = root.backdropElement;
    if (!backdrop) {
        return;
    }
    const resolved = clamp(progress, 0, 1);
    backdrop.style.setProperty('--drawer-swipe-progress', String(resolved));
    if (resolved > 0 && root.frontmostHeight > 0) {
        backdrop.style.setProperty('--drawer-height', root.frontmostHeight + 'px');
    } else {
        backdrop.style.removeProperty('--drawer-height');
    }
}

function syncParentProgress(root, progress) {
    const parent = state.roots.get(root.parentRootId);
    if (!parent?.popupElement) {
        return;
    }
    parent.popupElement.style.setProperty('--drawer-swipe-progress', String(clamp(progress, 0, 1)));
}

function setProviderVisual(root, progress, height) {
    const provider = state.providers.get(root.providerId);
    if (!provider) {
        return;
    }
    provider.swipeProgress = clamp(progress, 0, 1);
    provider.frontmostHeight = height > 0 ? height : 0;
    for (const element of provider.indents.values()) {
        applyIndentVisual(element, provider.swipeProgress, provider.frontmostHeight);
    }
}

function applyIndentVisual(element, progress, height) {
    element.style.setProperty('--drawer-swipe-progress', String(clamp(progress, 0, 1)));
    if (height > 0) {
        element.style.setProperty('--drawer-height', height + 'px');
    } else {
        element.style.removeProperty('--drawer-height');
    }
}

function setSwipeDismissed(root, dismissed) {
    root.popupElement?.toggleAttribute(DATA_SWIPE_DISMISS, dismissed);
    root.backdropElement?.toggleAttribute(DATA_SWIPE_DISMISS, dismissed);
}

function syncSwipeAreaAvailability(root) {
    for (const area of root.swipeAreas.values()) {
        const enabled = !area.disabled && (!root.isOpen || area.swipeActive);
        area.enabled = enabled;
        area.element.toggleAttribute('data-open', root.isOpen);
        area.element.toggleAttribute('data-closed', !root.isOpen);
        area.element.toggleAttribute('data-disabled', area.disabled);
        if (enabled) {
            area.element.style.removeProperty('pointer-events');
        } else {
            area.element.style.pointerEvents = 'none';
        }
    }
}

function setupSwipeArea(root, area) {
    const element = area.element;
    const doc = element.ownerDocument || document;
    const abortController = new AbortController();
    const signal = abortController.signal;
    let activePointerId = null;
    let pointerCleanup = null;

    const resolvedDirection = () => area.swipeDirection || opposite(root.swipeDirection);
    const engine = createSwipeDismiss({
        enabled: () => area.enabled && !area.disabled,
        directions: () => [resolvedDirection()],
        element: () => element,
        movementCssVars: {
            x: '--drawer-swipe-movement-x',
            y: '--drawer-swipe-movement-y'
        },
        trackDrag: false,
        onSwipeStart(event) {
            disableOutsidePressForSwipe(root, area);
            area.startEvent = event;
            area.openedBySwipe = false;
            area.openAccepted = false;
            area.swipeActive = true;
            area.dragDelta = { x: 0, y: 0 };
            root.swipeAreaActive = true;
            syncSwipeAreaAvailability(root);
            invoke(area.dotNetRef, 'OnSwipingChanged', true);
        },
        onProgress(_progress, details) {
            if (!details || !area.startEvent) {
                return;
            }
            area.dragDelta = { x: details.deltaX, y: details.deltaY };
            if (details.direction !== resolvedDirection()) {
                return;
            }
            const displacement = getDisplacement(
                resolvedDirection(),
                details.deltaX,
                details.deltaY
            );
            if (displacement < MIN_DRAG_THRESHOLD && !area.openedBySwipe) {
                return;
            }
            if (!area.openedBySwipe) {
                area.openedBySwipe = true;
                invoke(area.dotNetRef, 'OnSwipeOpen').then((accepted) => {
                    area.openAccepted = accepted !== false;
                    reapplyActiveSwipeArea(root);
                });
            }
            applySwipeAreaMovement(root, area);
        },
        onRelease(details) {
            const displacement = getDisplacement(
                resolvedDirection(),
                details.deltaX,
                details.deltaY
            );
            const releaseVelocity = getDisplacement(
                resolvedDirection(),
                details.releaseVelocityX,
                details.releaseVelocityY
            );
            const threshold = resolveSwipeAreaThreshold(root, area);
            const shouldOpen =
                details.direction === resolvedDirection() &&
                (displacement >= threshold || releaseVelocity >= VELOCITY_THRESHOLD) &&
                !area.disabled;
            if (shouldOpen) {
                if (!area.openedBySwipe) {
                    area.openedBySwipe = true;
                    invoke(area.dotNetRef, 'OnSwipeOpen');
                }
            } else if (area.openedBySwipe) {
                invoke(area.dotNetRef, 'OnSwipeClose');
            }
            finishSwipeArea(root, area);
            return false;
        },
        onCancel() {
            finishSwipeArea(root, area);
        }
    });
    area.engine = engine;

    const cleanupPointer = () => {
        pointerCleanup?.();
        pointerCleanup = null;
        activePointerId = null;
    };
    const onPointerMove = (event) => {
        if (activePointerId === null || event.pointerId === activePointerId) {
            engine.move(event, element);
        }
    };
    const onPointerEnd = (event) => {
        if (activePointerId === null || event.pointerId === activePointerId) {
            engine.end(event, element);
            cleanupPointer();
        }
    };
    const onPointerDown = (event) => {
        if (event.pointerType === 'touch' || area.disabled || !area.enabled) {
            return;
        }
        engine.start(event, element);
        activePointerId = event.pointerId;
        const options = { capture: true };
        doc.addEventListener('pointermove', onPointerMove, options);
        doc.addEventListener('pointerup', onPointerEnd, options);
        doc.addEventListener('pointercancel', onPointerEnd, options);
        pointerCleanup = () => {
            doc.removeEventListener('pointermove', onPointerMove, options);
            doc.removeEventListener('pointerup', onPointerEnd, options);
            doc.removeEventListener('pointercancel', onPointerEnd, options);
        };
        if (event.cancelable) {
            event.preventDefault();
        }
    };
    const onTouchStart = (event) => engine.start(event, element);
    const onTouchMove = (event) => engine.move(event, element);
    const onTouchEnd = (event) => engine.end(event, element);

    element.addEventListener('pointerdown', onPointerDown, { signal });
    element.addEventListener('touchstart', onTouchStart, { signal, passive: true });
    doc.addEventListener('touchmove', onTouchMove, { signal, passive: true, capture: true });
    doc.addEventListener('touchend', onTouchEnd, { signal, passive: true, capture: true });
    doc.addEventListener('touchcancel', onTouchEnd, { signal, passive: true, capture: true });

    return () => {
        cleanupPointer();
        abortController.abort();
        engine.reset();
        area.releaseGuardCleanup?.();
        setOutsidePressEnabled(root.rootId, true);
    };
}

function disableOutsidePressForSwipe(root, area) {
    area.releaseGuardCleanup?.();
    area.releaseGuardCleanup = null;
    setOutsidePressEnabled(root.rootId, false);
}

function enableOutsidePressAfterRelease(root, area) {
    area.releaseGuardCleanup?.();
    const doc = area.element.ownerDocument || document;
    const view = doc.defaultView || window;
    const restore = () => {
        view.removeEventListener('pointerdown', restore, true);
        area.releaseGuardCleanup = null;
        setOutsidePressEnabled(root.rootId, true);
    };
    area.releaseGuardCleanup = restore;
    view.addEventListener('pointerdown', restore, true);
}

function finishSwipeArea(root, area) {
    area.startEvent = null;
    area.openedBySwipe = false;
    area.openAccepted = false;
    area.closedOffset = null;
    area.swipeActive = false;
    root.swipeAreaActive = false;
    syncSwipeAreaAvailability(root);
    enableOutsidePressAfterRelease(root, area);
    area.dragDelta = { x: 0, y: 0 };
    clearSwipeAreaStyles(root, area);
    invoke(area.dotNetRef, 'OnSwipingChanged', false);
}

function resolveSwipeAreaThreshold(root, area) {
    const popup = root.popupElement;
    if (!popup) {
        return FALLBACK_SWIPE_OPEN_THRESHOLD;
    }
    const dismissDirection = opposite(area.swipeDirection);
    const size = isHorizontal(dismissDirection) ? popup.offsetWidth : popup.offsetHeight;
    return size > 0 ? size * DEFAULT_SWIPE_OPEN_RATIO : FALLBACK_SWIPE_OPEN_THRESHOLD;
}

function resolveSwipeAreaClosedOffset(root, area) {
    const popup = root.popupElement;
    if (!popup) {
        return null;
    }
    const dismissDirection = opposite(area.swipeDirection);
    const horizontal = isHorizontal(dismissDirection);
    const size = horizontal ? popup.offsetWidth : popup.offsetHeight;
    if (size <= 0) {
        return null;
    }
    const transform = getElementTransform(popup);
    const transformOffset = horizontal ? transform.x : transform.y;
    return Number.isFinite(transformOffset) && Math.abs(transformOffset) > 0.5
        ? Math.min(size, Math.abs(transformOffset))
        : size;
}

function applySwipeAreaMovement(root, area) {
    const popup = root.popupElement;
    if (!area.swipeActive || !popup || (!root.mounted && !area.openedBySwipe)) {
        return;
    }
    area.closedOffset ??= resolveSwipeAreaClosedOffset(root, area);
    const closedOffset = area.closedOffset;
    if (!closedOffset || closedOffset <= 0) {
        return;
    }

    const displacement = Math.max(
        0,
        getDisplacement(area.swipeDirection, area.dragDelta.x, area.dragDelta.y)
    );
    const damped = displacement > closedOffset
        ? closedOffset + Math.sqrt(displacement - closedOffset)
        : displacement;
    const remaining = closedOffset - damped;
    const dismissDirection = opposite(area.swipeDirection);
    const sign = dismissDirection === 'left' || dismissDirection === 'up' ? -1 : 1;
    const movement = remaining * sign;
    const horizontal = isHorizontal(dismissDirection);
    const openProgress = clamp(displacement / closedOffset, 0, 1);
    const backdropProgress = clamp(1 - openProgress, 0, 1);

    popup.style.setProperty('--drawer-swipe-movement-x', horizontal ? movement + 'px' : '0px');
    popup.style.setProperty('--drawer-swipe-movement-y', horizontal ? '0px' : movement + 'px');
    popup.setAttribute(DATA_SWIPING, '');
    if (area.popupTransition === null) {
        area.popupTransition = popup.style.transition;
    }
    popup.style.transition = 'none';
    area.appliedStyles = true;
    root.swipeAreaActive = true;

    root.backdropElement?.setAttribute(DATA_SWIPING, '');
    applyBackdropProgress(root, backdropProgress);
    setProviderVisual(root, openProgress, openProgress > 0 ? root.frontmostHeight : 0);
}

function clearSwipeAreaStyles(root, area) {
    const popup = root.popupElement;
    if (popup && area.appliedStyles) {
        popup.style.removeProperty('--drawer-swipe-movement-x');
        popup.style.removeProperty('--drawer-swipe-movement-y');
        popup.removeAttribute(DATA_SWIPING);
    }
    if (popup && area.popupTransition !== null) {
        popup.style.transition = area.popupTransition;
    }
    area.popupTransition = null;
    area.appliedStyles = false;
    root.backdropElement?.removeAttribute(DATA_SWIPING);
    applyBackdropProgress(root, 0);
    setProviderVisual(root, 0, 0);
}

function reapplyActiveSwipeArea(root) {
    for (const area of root.swipeAreas.values()) {
        if (area.swipeActive && area.appliedStyles) {
            applySwipeAreaMovement(root, area);
        }
    }
}

function processViewportTouchMove(root, event, touchState, touch, element) {
    const vertical = !isHorizontal(root.swipeDirection);
    const scrollAxis = vertical ? 'vertical' : 'horizontal';
    const drawerAxisDelta = vertical
        ? touch.clientY - touchState.lastY
        : touch.clientX - touchState.lastX;
    const win = element.ownerDocument?.defaultView || window;

    if (isEventOnRangeInput(event, win)) {
        touchState.allowSwipe = false;
        return;
    }
    if (event.touches.length === 2) {
        return;
    }
    if (
        shouldIgnoreSwipeForTextSelection(element.ownerDocument, element) ||
        !root.isOpen ||
        !root.mounted ||
        hasOpenDescendant(root)
    ) {
        return;
    }
    if (preserveNativeCrossAxisScrollOnMove(touchState, touch, vertical)) {
        return;
    }

    const scrollTarget = touchState.scrollTarget;
    const doc = element.ownerDocument;
    if (!scrollTarget || scrollTarget === doc.documentElement || scrollTarget === doc.body) {
        if (event.cancelable) event.preventDefault();
        event.stopPropagation();
        root.dismissSwipe?.moveNative(event, element);
        return;
    }
    if (!hasScrollableContentOnAxis(scrollTarget, scrollAxis)) {
        if (event.cancelable) event.preventDefault();
        event.stopPropagation();
        return;
    }
    if (drawerAxisDelta !== 0) {
        const canSwipe = canSwipeFromScrollEdgeOnMove(
            scrollTarget,
            scrollAxis,
            root.swipeDirection,
            drawerAxisDelta
        );
        if (!touchState.allowSwipe) {
            if (event.cancelable && canSwipe) {
                touchState.allowSwipe = true;
                event.preventDefault();
            } else {
                touchState.allowSwipe = false;
            }
        } else if (event.cancelable) {
            event.preventDefault();
        }
    }
    if (touchState.allowSwipe === true) {
        event.stopPropagation();
        root.dismissSwipe?.moveNative(event, element);
    }
}

function resetTouchSwipeState(root, ignoreSwipe) {
    root.ignoreTouchSwipe = ignoreSwipe;
    root.touchScrollState = null;
}

function resetTouchTrackingState(root) {
    resetTouchSwipeState(root, false);
    root.lastPointerType = '';
    root.ignoreNextTouchStartFromPen = false;
}

function measurePopup(root) {
    const element = root.popupElement;
    if (!element || !root.mounted) {
        setPopupHeight(root, 0);
        return;
    }
    const height = element.offsetHeight || 0;
    if (
        root.popupHeight > 0 &&
        root.frontmostHeight > root.popupHeight &&
        height > root.popupHeight
    ) {
        return;
    }
    if (root.popupHeight > 0 && root.hasNestedDrawer) {
        invoke(root.popupDotNetRef, 'OnPopupHeightChanged', Math.round(root.popupHeight));
        return;
    }
    setPopupHeight(root, height);
}

function setPopupHeight(root, height) {
    const resolved = Math.max(0, Number.isFinite(height) ? height : 0);
    if (resolved === root.popupHeight) {
        return;
    }
    root.popupHeight = resolved;
    invoke(root.popupDotNetRef, 'OnPopupHeightChanged', Math.round(resolved));
    updateSnapPointOffset(root);
}

function measureViewport(root) {
    const doc = root.viewportElement?.ownerDocument || root.popupElement?.ownerDocument || document;
    const html = doc.documentElement;
    root.viewportHeight = root.viewportElement?.offsetHeight || html.clientHeight || window.innerHeight || 0;
    const fontSize = Number.parseFloat(getComputedStyle(html).fontSize);
    root.rootFontSize = Number.isFinite(fontSize) ? fontSize : 16;
}

function updateSnapPointOffset(root) {
    const popup = root.popupElement;
    if (!popup) {
        return;
    }
    const points = resolveSnapPoints(root);
    let resolvedPoint;
    if (points.length && root.activeSnapPoint !== null) {
        resolvedPoint = points.find((point) => point.value === root.activeSnapPoint);
        if (!resolvedPoint) {
            const height = resolveSnapPointValue(
                root.activeSnapPoint,
                root.viewportHeight,
                root.rootFontSize
            );
            if (height !== null && Number.isFinite(height)) {
                const maxHeight = Math.min(root.popupHeight, root.viewportHeight);
                const targetHeight = clamp(height, 0, maxHeight);
                resolvedPoint = points[
                    closestSnapPointIndex(points.map((point) => point.height), targetHeight)
                ];
            }
        }
    }

    const offset = resolvedPoint?.offset ?? 0;
    root.activeSnapPointOffset = resolvedPoint ? offset : null;
    const signedOffset = root.swipeDirection === 'up' ? -offset : offset;
    popup.style.setProperty('--drawer-snap-point-offset', signedOffset + 'px');
    if (root.reportedSnapPointOffset !== signedOffset) {
        root.reportedSnapPointOffset = signedOffset;
        invoke(root.viewportDotNetRef, 'OnSnapPointOffsetChanged', signedOffset);
    }
    if (root.frontmostHeight > 0) {
        popup.style.setProperty('--drawer-frontmost-height', root.frontmostHeight + 'px');
    } else {
        popup.style.removeProperty('--drawer-frontmost-height');
    }
}

function resolveSnapPoints(root) {
    if (
        !Array.isArray(root.snapPoints) ||
        root.snapPoints.length === 0 ||
        root.viewportHeight <= 0 ||
        root.popupHeight <= 0
    ) {
        return [];
    }
    const maxHeight = Math.min(root.popupHeight, root.viewportHeight);
    const resolved = [];
    for (const value of root.snapPoints) {
        const height = resolveSnapPointValue(value, root.viewportHeight, root.rootFontSize);
        if (height === null || !Number.isFinite(height)) {
            continue;
        }
        const clampedHeight = clamp(height, 0, maxHeight);
        resolved.push({
            value: String(value),
            height: clampedHeight,
            offset: Math.max(0, root.popupHeight - clampedHeight)
        });
    }

    const deduped = [];
    const seen = [];
    for (let index = resolved.length - 1; index >= 0; index -= 1) {
        const point = resolved[index];
        if (seen.some((height) => Math.abs(height - point.height) <= 1)) {
            continue;
        }
        seen.push(point.height);
        deduped.push(point);
    }
    deduped.reverse();
    return deduped;
}

function resolveSnapPointValue(value, viewportHeight, rootFontSize) {
    if (!Number.isFinite(viewportHeight) || viewportHeight <= 0) {
        return null;
    }
    const text = String(value).trim();
    const numeric = Number.parseFloat(text);
    if (/^-?\d+(\.\d+)?$/.test(text)) {
        if (!Number.isFinite(numeric)) return null;
        return numeric <= 1 ? clamp(numeric, 0, 1) * viewportHeight : numeric;
    }
    if (text.endsWith('px')) {
        return Number.isFinite(numeric) ? numeric : null;
    }
    if (text.endsWith('rem')) {
        return Number.isFinite(numeric) ? numeric * rootFontSize : null;
    }
    return null;
}

function getSnapPointRange(points) {
    if (points.length < 2) {
        return null;
    }
    const offsets = points.map((point) => point.offset).filter(Number.isFinite).sort((a, b) => a - b);
    if (offsets.length < 2) {
        return null;
    }
    const minOffset = offsets[0];
    const maxOffset = offsets[offsets.length - 1];
    let range = offsets[1] - minOffset;
    if (!Number.isFinite(range) || range <= 0) {
        range = maxOffset - minOffset;
    }
    return Number.isFinite(range) && range > 0 ? { minOffset, range } : null;
}

function progressForSnapPoint(root, offset) {
    const range = getSnapPointRange(resolveSnapPoints(root));
    return range ? clamp((offset - range.minOffset) / range.range, 0, 1) : 0;
}

function getSnapPointSwipeMovement(baseOffset, movement) {
    const nextOffset = baseOffset + movement;
    return nextOffset >= 0 ? movement : -Math.sqrt(-nextOffset) - baseOffset;
}

function closestSnapPointIndex(values, target) {
    let index = -1;
    let distance = Infinity;
    for (let current = 0; current < values.length; current += 1) {
        const nextDistance = Math.abs(values[current] - target);
        if (nextDistance < distance) {
            distance = nextDistance;
            index = current;
        }
    }
    return index;
}

function setupCloseWatcher(root) {
    if (
        !root.isOpen ||
        !root.popupElement ||
        root.nestedOpenDialogCount > 0 ||
        hasOpenDescendant(root) ||
        !isAndroid()
    ) {
        cleanupCloseWatcher(root);
        return;
    }
    if (root.closeWatcher?.popupElement === root.popupElement) {
        return;
    }
    cleanupCloseWatcher(root);
    const win = root.popupElement.ownerDocument?.defaultView || window;
    if (typeof win.CloseWatcher !== 'function') {
        return;
    }
    const watcher = new win.CloseWatcher();
    const onClose = (event) => {
        if (!root.isOpen || hasOpenDescendant(root)) {
            return;
        }
        event.preventDefault?.();
        invoke(root.rootDotNetRef, 'OnCloseWatcher');
    };
    watcher.addEventListener('close', onClose);
    root.closeWatcher = { watcher, onClose, popupElement: root.popupElement };
}

function cleanupCloseWatcher(root) {
    if (!root.closeWatcher) {
        return;
    }
    root.closeWatcher.watcher.removeEventListener('close', root.closeWatcher.onClose);
    root.closeWatcher.watcher.destroy?.();
    root.closeWatcher = null;
}

function refreshCloseWatchers() {
    for (const root of state.roots.values()) {
        setupCloseWatcher(root);
    }
}

function hasOpenDescendant(root) {
    for (const candidate of state.roots.values()) {
        if (!candidate.isOpen || candidate.rootId === root.rootId) {
            continue;
        }
        let parentId = candidate.parentRootId;
        while (parentId) {
            if (parentId === root.rootId) {
                return true;
            }
            parentId = state.roots.get(parentId)?.parentRootId || null;
        }
    }
    return false;
}

function syncVirtualKeyboard(root) {
    cleanupVirtualKeyboard(root);
    if (!root.virtualKeyboardEnabled || !root.isOpen || !root.mounted || !root.viewportElement) {
        return;
    }

    const element = root.viewportElement;
    const doc = element.ownerDocument;
    const win = doc.defaultView || window;
    const visualViewport = win.visualViewport;
    const abortController = new AbortController();
    const signal = abortController.signal;
    const keyboardState = root.virtualKeyboardState ??= {
        touchMoved: false,
        touchStart: null,
        focusedTarget: null,
        adjustment: null,
        frame: 0
    };

    const resetInset = () => element.style.setProperty('--drawer-keyboard-inset', '0px');
    const restoreAdjustment = () => restoreKeyboardScrollAdjustment(keyboardState);
    const clearFocused = () => {
        keyboardState.focusedTarget = null;
        resetInset();
        restoreAdjustment();
        cancelAnimationFrame(keyboardState.frame);
        keyboardState.frame = 0;
    };
    const align = () => {
        const target = keyboardState.focusedTarget;
        if (hasOpenDescendant(root) || !target || !contains(element, target)) {
            resetInset();
            restoreAdjustment();
            return;
        }
        const viewport = getKeyboardVisualViewport(win);
        if (!viewport) {
            resetInset();
            restoreAdjustment();
            return;
        }
        element.style.setProperty(
            '--drawer-keyboard-inset',
            Math.max(0, Math.ceil(win.innerHeight - viewport.bottom)) + 'px'
        );
        const scrollTarget = findKeyboardScrollTarget(target, element);
        if (!scrollTarget || !scrollTarget.isConnected || !contains(element, scrollTarget)) {
            restoreAdjustment();
            return;
        }
        const rect = scrollTarget.getBoundingClientRect();
        const clippedBottom = Math.min(rect.bottom, viewport.bottom);
        const overlap = Math.max(0, rect.bottom - viewport.bottom);
        setKeyboardScrollSlack(
            keyboardState,
            scrollTarget,
            overlap > 0 ? overlap + KEYBOARD_SCROLL_SLACK : 0
        );
        const maxScrollTop = Math.max(0, scrollTarget.scrollHeight - scrollTarget.clientHeight);
        if (maxScrollTop <= 0) {
            return;
        }
        const visibleTop = Math.max(rect.top, viewport.top) + KEYBOARD_VISIBILITY_MARGIN;
        const visibleBottom = clippedBottom - KEYBOARD_VISIBILITY_MARGIN;
        if (visibleBottom <= visibleTop) {
            return;
        }
        const targetRect = target.getBoundingClientRect();
        const targetCenter = (targetRect.top + targetRect.bottom) / 2;
        const visibleCenter = (visibleTop + visibleBottom) / 2;
        const nextScrollTop = clamp(
            scrollTarget.scrollTop + targetCenter - visibleCenter,
            0,
            maxScrollTop
        );
        const behavior = win.matchMedia?.('(prefers-reduced-motion: reduce)')?.matches
            ? 'auto'
            : 'smooth';
        scrollTarget.scrollTo({ top: nextScrollTop, behavior });
    };
    const schedule = () => {
        cancelAnimationFrame(keyboardState.frame);
        keyboardState.frame = requestAnimationFrame(align);
    };
    const capture = (target) => {
        if (hasOpenDescendant(root)) {
            return false;
        }
        const resolved = resolveKeyboardInputTarget(target);
        if (!resolved || !contains(element, resolved)) {
            return false;
        }
        keyboardState.focusedTarget = resolved;
        return true;
    };
    const onFocusIn = (event) => {
        if (capture(getEventTarget(event))) {
            schedule();
        }
    };
    const onFocusOut = (event) => {
        if (capture(event.relatedTarget)) {
            schedule();
        } else {
            clearFocused();
        }
    };
    const onViewportUpdate = () => {
        if (keyboardState.focusedTarget || capture(doc.activeElement)) {
            schedule();
        }
    };

    doc.addEventListener('focusin', onFocusIn, { signal, capture: true });
    doc.addEventListener('focusout', onFocusOut, { signal, capture: true });
    visualViewport?.addEventListener('resize', onViewportUpdate, { signal });
    visualViewport?.addEventListener('scroll', onViewportUpdate, { signal });
    if (capture(doc.activeElement)) {
        schedule();
    }

    root.virtualKeyboardCleanup = () => {
        abortController.abort();
        clearFocused();
        element.style.removeProperty('--drawer-keyboard-inset');
    };
}

function cleanupVirtualKeyboard(root) {
    root.virtualKeyboardCleanup?.();
    root.virtualKeyboardCleanup = null;
    if (root.viewportElement) {
        root.viewportElement.style.removeProperty('--drawer-keyboard-inset');
    }
}

function virtualKeyboardTouchStart(root, event) {
    if (!root.virtualKeyboardEnabled || !root.isOpen || !root.mounted || hasOpenDescendant(root)) {
        virtualKeyboardTouchCancel(root);
        return;
    }
    const touch = event.touches[0];
    if (!touch) {
        return;
    }
    const keyboardState = root.virtualKeyboardState ??= {};
    keyboardState.touchMoved = false;
    keyboardState.touchStart = { x: touch.clientX, y: touch.clientY };
}

function virtualKeyboardTouchMove(root, event) {
    const keyboardState = root.virtualKeyboardState;
    const touch = event.touches[0];
    const start = keyboardState?.touchStart;
    if (!touch || !start || keyboardState.touchMoved) {
        return;
    }
    if (
        Math.abs(touch.clientX - start.x) > INPUT_TAP_MOVE_THRESHOLD ||
        Math.abs(touch.clientY - start.y) > INPUT_TAP_MOVE_THRESHOLD
    ) {
        keyboardState.touchMoved = true;
    }
}

function virtualKeyboardTouchEnd(root, event, currentTarget) {
    const keyboardState = root.virtualKeyboardState;
    const rootElement = root.viewportElement;
    if (
        !root.virtualKeyboardEnabled ||
        !root.isOpen ||
        !root.mounted ||
        hasOpenDescendant(root) ||
        !rootElement ||
        !keyboardState?.touchStart ||
        keyboardState.touchMoved
    ) {
        virtualKeyboardTouchCancel(root);
        return;
    }

    const touch = event.changedTouches[0] ?? event.touches[0];
    const doc = currentTarget.ownerDocument;
    const nativeTarget = getEventTarget(event);
    const pointTarget = touch
        ? resolveKeyboardTouchTargetFromPoint(doc, touch.clientX, touch.clientY)
        : null;
    if (pointTarget === KEYBOARD_TAP_BLOCKED) {
        virtualKeyboardTouchCancel(root);
        return;
    }
    const keyboardTarget = touch && (pointTarget ?? resolveKeyboardTouchTarget(nativeTarget));
    if (
        keyboardTarget &&
        (!contains(rootElement, keyboardTarget.focusTarget) ||
            !contains(rootElement, keyboardTarget.clickTarget))
    ) {
        virtualKeyboardTouchCancel(root);
        return;
    }
    if (keyboardTarget) {
        const focusTarget = keyboardTarget.focusTarget;
        const win = focusTarget.ownerDocument.defaultView || window;
        if (win.visualViewport && win.visualViewport.scale !== 1) {
            virtualKeyboardTouchCancel(root);
            return;
        }
        if (
            focusTarget.ownerDocument.activeElement === focusTarget &&
            isKeyboardVisualViewportOpen(win)
        ) {
            virtualKeyboardTouchCancel(root);
            return;
        }
        if (event.cancelable) {
            event.preventDefault();
        }
        focusKeyboardInputWithoutPageScroll(focusTarget);
        dispatchKeyboardClick(keyboardTarget.clickTarget, touch);
    }
    virtualKeyboardTouchCancel(root);
}

function virtualKeyboardTouchCancel(root) {
    if (root.virtualKeyboardState) {
        root.virtualKeyboardState.touchMoved = false;
        root.virtualKeyboardState.touchStart = null;
    }
}

function restoreKeyboardScrollAdjustment(keyboardState) {
    const adjustment = keyboardState.adjustment;
    if (!adjustment) {
        return;
    }
    adjustment.element.style.overflowAnchor = adjustment.overflowAnchor;
    adjustment.element.style.paddingBottom = adjustment.paddingBottom;
    adjustment.element.style.scrollPaddingBottom = adjustment.scrollPaddingBottom;
    keyboardState.adjustment = null;
}

function setKeyboardScrollSlack(keyboardState, element, slack) {
    const rounded = Math.max(0, Math.ceil(slack));
    let adjustment = keyboardState.adjustment;
    if (adjustment && !adjustment.element.isConnected) {
        restoreKeyboardScrollAdjustment(keyboardState);
        adjustment = null;
    }
    if (rounded === 0) {
        restoreKeyboardScrollAdjustment(keyboardState);
        return;
    }
    if (adjustment && adjustment.element !== element) {
        restoreKeyboardScrollAdjustment(keyboardState);
        adjustment = null;
    }
    if (!adjustment) {
        const styles = getComputedStyle(element);
        adjustment = {
            element,
            overflowAnchor: element.style.overflowAnchor,
            paddingBottom: element.style.paddingBottom,
            scrollPaddingBottom: element.style.scrollPaddingBottom,
            computedPaddingBottom: Number.parseFloat(styles.paddingBottom) || 0,
            computedScrollPaddingBottom: Number.parseFloat(styles.scrollPaddingBottom) || 0
        };
        keyboardState.adjustment = adjustment;
    }
    element.style.overflowAnchor = 'none';
    element.style.paddingBottom = adjustment.computedPaddingBottom + rounded + 'px';
    element.style.scrollPaddingBottom =
        adjustment.computedScrollPaddingBottom + KEYBOARD_VISIBILITY_MARGIN + 'px';
}

function resolveKeyboardInputTarget(target) {
    if (!(target instanceof HTMLElement)) {
        return null;
    }
    if (isKeyboardInputElement(target)) {
        return target.isContentEditable ? getContentEditableHost(target) : target;
    }
    const control = target.closest('label')?.control;
    return control instanceof HTMLElement && isKeyboardInputElement(control) ? control : null;
}

function isKeyboardInputElement(element) {
    if (element.isContentEditable) {
        return true;
    }
    const win = element.ownerDocument.defaultView || window;
    return (
        element instanceof win.HTMLTextAreaElement ||
        (element instanceof win.HTMLInputElement && KEYBOARD_INPUT_TYPES.has(element.type))
    ) && !element.matches(':disabled');
}

function resolveKeyboardTouchTarget(target) {
    const focusTarget = resolveKeyboardInputTarget(target);
    return focusTarget
        ? { focusTarget, clickTarget: target instanceof HTMLElement ? target : focusTarget }
        : null;
}

function getContentEditableHost(element) {
    let host = element;
    while (host.parentElement?.isContentEditable) {
        host = host.parentElement;
    }
    return host;
}

function resolveKeyboardTouchTargetFromPoint(doc, x, y) {
    const exact = getElementAtPoint(doc, x, y);
    const focusTarget = resolveKeyboardInputTarget(exact);
    if (focusTarget) {
        return {
            focusTarget,
            clickTarget: exact instanceof HTMLElement ? exact : focusTarget
        };
    }
    if (isInteractiveElement(exact) || exact?.closest('label')) {
        return KEYBOARD_TAP_BLOCKED;
    }
    for (const [offsetX, offsetY] of [
        [0, INPUT_TAP_HIT_SLOP],
        [0, -INPUT_TAP_HIT_SLOP],
        [INPUT_TAP_HIT_SLOP, 0],
        [-INPUT_TAP_HIT_SLOP, 0]
    ]) {
        const target = resolveKeyboardInputTarget(getElementAtPoint(doc, x + offsetX, y + offsetY));
        if (target) {
            return { focusTarget: target, clickTarget: target };
        }
    }
    return null;
}

function dispatchKeyboardClick(target, touch) {
    const win = target.ownerDocument.defaultView || window;
    const ClickEvent = win.PointerEvent || win.MouseEvent;
    target.dispatchEvent(new ClickEvent('click', {
        bubbles: true,
        cancelable: true,
        clientX: touch.clientX,
        clientY: touch.clientY,
        detail: 1,
        view: win
    }));
}

function focusKeyboardInputWithoutPageScroll(target) {
    const wasFocused = target.ownerDocument.activeElement === target;
    const previousOpacity = target.style.opacity;
    const previousTransform = target.style.transform;
    const previousTransition = target.style.transition;
    target.style.transition = 'none';
    target.style.opacity = '0';
    target.style.transform = 'translateY(-2000px)';
    try {
        if (wasFocused) target.blur();
        target.focus({ preventScroll: true });
    } finally {
        target.style.opacity = previousOpacity;
        target.style.transform = previousTransform;
        target.style.transition = previousTransition;
    }
}

function findKeyboardScrollTarget(target, root) {
    const start = getParentNode(target);
    return findScrollableTouchTarget(start, root, 'vertical') ??
        findScrollableTouchTarget(start, root, 'vertical', true);
}

function getKeyboardVisualViewport(win) {
    const viewport = win.visualViewport;
    if (!viewport || viewport.scale !== 1) {
        return null;
    }
    if (win.innerHeight - viewport.height <= KEYBOARD_RESIZE_THRESHOLD) {
        return null;
    }
    const top = Math.max(0, viewport.offsetTop);
    return {
        top,
        bottom: Math.min(win.innerHeight, top + viewport.height)
    };
}

function isKeyboardVisualViewportOpen(win) {
    return !win.visualViewport || getKeyboardVisualViewport(win) !== null;
}

function findScrollableTouchTarget(target, root, axis = 'vertical', allowOverflowIntent = false) {
    let node = target instanceof HTMLElement ? target : null;
    while (node instanceof HTMLElement && node !== root) {
        if (isScrollable(node, axis, allowOverflowIntent)) {
            return node;
        }
        node = getParentNode(node);
    }
    return root instanceof HTMLElement && isScrollable(root, axis, allowOverflowIntent) ? root : null;
}

function hasScrollableAncestor(target, root, axes) {
    let node = target;
    while (node instanceof HTMLElement && node !== root) {
        if (axes.some((axis) => isScrollable(node, axis))) {
            return true;
        }
        node = getParentNode(node);
    }
    return false;
}

function isScrollable(element, axis, allowOverflowIntent = false) {
    const styles = getComputedStyle(element);
    const overflow = axis === 'vertical' ? styles.overflowY : styles.overflowX;
    if (overflow !== 'auto' && overflow !== 'scroll') {
        return false;
    }
    if (allowOverflowIntent) {
        return axis === 'vertical' ? element.clientHeight > 0 : element.clientWidth > 0;
    }
    return axis === 'vertical'
        ? element.scrollHeight > element.clientHeight
        : element.scrollWidth > element.clientWidth;
}

function getParentNode(node) {
    return node?.assignedSlot || node?.parentNode || node?.host || null;
}

function findGestureScrollableTouchTarget(target, root, hasHorizontal, hasVertical) {
    if (hasHorizontal && !hasVertical) {
        return findScrollableTouchTarget(target, root, 'horizontal');
    }
    if (hasVertical && !hasHorizontal) {
        return findScrollableTouchTarget(target, root, 'vertical');
    }
    return findScrollableTouchTarget(target, root, 'vertical') ??
        findScrollableTouchTarget(target, root, 'horizontal');
}

function canSwipeFromPendingScrollEdge(scrollTarget, deltaX, deltaY, directions) {
    const allowLeft = directions.includes('left');
    const allowRight = directions.includes('right');
    const allowUp = directions.includes('up');
    const allowDown = directions.includes('down');
    const horizontal = allowLeft || allowRight;
    const vertical = allowUp || allowDown;
    if (vertical && deltaY !== 0 && (!horizontal || Math.abs(deltaY) >= Math.abs(deltaX))) {
        const max = scrollTarget.scrollHeight - scrollTarget.clientHeight;
        return (deltaY > 0 && scrollTarget.scrollTop <= 0 && allowDown) ||
            (deltaY < 0 && scrollTarget.scrollTop >= Math.max(0, max) && allowUp);
    }
    if (horizontal && deltaX !== 0 && (!vertical || Math.abs(deltaX) > Math.abs(deltaY))) {
        const max = scrollTarget.scrollWidth - scrollTarget.clientWidth;
        return (deltaX > 0 && scrollTarget.scrollLeft <= 0 && allowRight) ||
            (deltaX < 0 && scrollTarget.scrollLeft >= Math.max(0, max) && allowLeft);
    }
    return null;
}

function preserveNativeCrossAxisScrollOnMove(touchState, touch, vertical) {
    if (touchState.preserveNativeCrossAxisScroll) {
        return true;
    }
    if (touchState.allowSwipe === true || !touchState.hasCrossAxisScrollableContent) {
        return false;
    }
    const drawerDelta = vertical
        ? touch.clientY - touchState.startY
        : touch.clientX - touchState.startX;
    const crossDelta = vertical
        ? touch.clientX - touchState.startX
        : touch.clientY - touchState.startY;
    if (Math.abs(crossDelta) < 6 || Math.abs(crossDelta) <= Math.abs(drawerDelta) + 2) {
        return false;
    }
    touchState.preserveNativeCrossAxisScroll = true;
    return true;
}

function hasScrollableContentOnAxis(target, axis) {
    return getScrollMetrics(target, axis).max > 0;
}

function getScrollMetrics(target, axis) {
    if (axis === 'vertical') {
        return {
            offset: target.scrollTop,
            max: Math.max(0, target.scrollHeight - target.clientHeight)
        };
    }
    return {
        offset: target.scrollLeft,
        max: Math.max(0, target.scrollWidth - target.clientWidth)
    };
}

function isAtSwipeStartEdge(target, axis, direction) {
    const fromStart = shouldDismissFromStartEdge(direction, axis);
    if (fromStart === null) {
        return false;
    }
    const metrics = getScrollMetrics(target, axis);
    return fromStart ? metrics.offset <= 0 : metrics.offset >= metrics.max;
}

function canSwipeFromScrollEdgeOnMove(target, axis, direction, delta) {
    const fromStart = shouldDismissFromStartEdge(direction, axis);
    if (fromStart === null || !(fromStart ? delta > 0 : delta < 0)) {
        return false;
    }
    return isAtSwipeStartEdge(target, axis, direction);
}

function shouldDismissFromStartEdge(direction, axis) {
    const start = axis === 'vertical' ? 'down' : 'right';
    const end = axis === 'vertical' ? 'up' : 'left';
    return direction === start ? true : direction === end ? false : null;
}

function shouldIgnoreSwipeForTextSelection(doc, root) {
    const active = doc.activeElement;
    if (
        active &&
        contains(root, active) &&
        (active.tagName === 'INPUT' || active.tagName === 'TEXTAREA') &&
        active.selectionStart != null &&
        active.selectionEnd != null &&
        active.selectionStart < active.selectionEnd
    ) {
        return true;
    }
    const selection = doc.getSelection?.();
    if (!selection || selection.isCollapsed) {
        return false;
    }
    const anchor = selection.anchorNode instanceof Element
        ? selection.anchorNode
        : selection.anchorNode?.parentElement;
    const focus = selection.focusNode instanceof Element
        ? selection.focusNode
        : selection.focusNode?.parentElement;
    return contains(root, anchor) || contains(root, focus) || selection.containsNode(root, true);
}

function isEventOnRangeInput(event, win) {
    const path = event.composedPath?.() || [];
    return path.some((target) => target instanceof win.HTMLInputElement && target.type === 'range') ||
        (getEventTarget(event) instanceof win.HTMLInputElement && getEventTarget(event).type === 'range');
}

function isSwipeIgnoredTarget(target) {
    return !!target?.closest?.(SWIPE_IGNORE_SELECTOR);
}

function isDrawerContentTarget(target) {
    return !!target?.closest?.(CONTENT_SELECTOR);
}

function isInteractiveElement(target) {
    return !!target?.closest?.(
        'button,a[href],[role="button"],select,[tabindex]:not([tabindex="-1"]),' +
        'input:not([type="hidden"]):not([disabled]),[contenteditable]:not([contenteditable="false"]),' +
        'textarea:not([disabled])'
    );
}

function getElementAtPoint(doc, x, y) {
    return typeof doc?.elementFromPoint === 'function' ? doc.elementFromPoint(x, y) : null;
}

function getTargetAtPoint(element, position, event) {
    const doc = element?.ownerDocument || document;
    return getElementAtPoint(doc, position.x, position.y) ?? getEventTarget(event);
}

function getEventTarget(event) {
    return event.composedPath?.()[0] ?? event.target;
}

function getPrimaryPosition(event) {
    if (isTouchEvent(event)) {
        const touch = event.touches[0];
        return touch ? { x: touch.clientX, y: touch.clientY } : null;
    }
    return { x: event.clientX, y: event.clientY };
}

function isTouchEvent(event) {
    return 'touches' in event;
}

function isTouchLikeEvent(event) {
    return isTouchEvent(event) || event.pointerType === 'touch';
}

function getElementTransform(element) {
    const transform = (element.ownerDocument.defaultView || window).getComputedStyle(element).transform;
    let x = 0;
    let y = 0;
    let scale = 1;
    if (transform && transform !== 'none') {
        const matrix = transform.match(/matrix(?:3d)?\(([^)]+)\)/);
        if (matrix) {
            const values = matrix[1].split(/,\s*/).map(Number.parseFloat);
            if (values.length === 6) {
                x = values[4];
                y = values[5];
                scale = Math.sqrt(values[0] * values[0] + values[1] * values[1]);
            } else if (values.length === 16) {
                x = values[12];
                y = values[13];
                scale = values[0];
            }
        }
    }
    return { x, y, scale };
}

function getDragTransform(offset, scale) {
    return 'translate3d(' + offset.x + 'px,' + offset.y + 'px,0) scale(' + scale + ')';
}

function applyDirectionalDamping(
    deltaX,
    deltaY,
    hasHorizontal,
    hasVertical,
    allowLeft,
    allowRight,
    allowUp,
    allowDown
) {
    const exponent = (value) => Math.sign(value) * Math.sqrt(Math.abs(value));
    const damp = (value, allowNegative, allowPositive) =>
        ((!allowNegative && value < 0) || (!allowPositive && value > 0))
            ? exponent(value)
            : value;
    return {
        x: hasHorizontal ? damp(deltaX, allowLeft, allowRight) : exponent(deltaX),
        y: hasVertical ? damp(deltaY, allowUp, allowDown) : exponent(deltaY)
    };
}

function getDisplacement(direction, deltaX, deltaY) {
    switch (direction) {
        case 'up': return -deltaY;
        case 'down': return deltaY;
        case 'left': return -deltaX;
        case 'right': return deltaX;
        default: return 0;
    }
}

function getBaseSwipeSize(element, direction) {
    return isHorizontal(direction) ? element.offsetWidth : element.offsetHeight;
}

function getBaseSwipeThreshold(element, direction) {
    return Math.max(getBaseSwipeSize(element, direction) * 0.5, MIN_SWIPE_THRESHOLD);
}

function hasPrimaryMouseButton(buttons) {
    return buttons % 2 === 1;
}

function safelyChangePointerCapture(element, pointerId, method) {
    try {
        element?.[method]?.(pointerId);
    } catch (error) {
        if (error?.name !== 'NotFoundError') {
            throw error;
        }
    }
}

function validTimeStamp(value) {
    return Number.isFinite(value) && value > 0 ? value : null;
}

function isHorizontal(direction) {
    return direction === 'left' || direction === 'right';
}

function opposite(direction) {
    switch (direction) {
        case 'up': return 'down';
        case 'down': return 'up';
        case 'left': return 'right';
        case 'right': return 'left';
        default: return 'up';
    }
}

function contains(parent, child) {
    return !!parent && !!child && (parent === child || parent.contains(child));
}

function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
}

function isAndroid() {
    const userAgentData = navigator.userAgentData;
    if (userAgentData?.platform) {
        return /Android/i.test(userAgentData.platform);
    }
    return /Android/i.test(navigator.userAgent);
}
