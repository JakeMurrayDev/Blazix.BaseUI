const STATE_KEY = Symbol.for('Blazix.BaseUI.Drawer.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        roots: new Map(),
        registeredCssVars: false
    };
}

const state = window[STATE_KEY];

const DATA_SWIPING = 'data-swiping';
const DATA_SWIPE_DISMISS = 'data-swipe-dismiss';
const CONTENT_SELECTOR = '[data-drawer-content]';
const SWIPE_IGNORE_SELECTOR = '[data-blazix-base-ui-swipe-ignore], [data-swipe-ignore]';
const MIN_SWIPE_THRESHOLD = 10;
const FAST_SWIPE_VELOCITY = 0.5;
const SNAP_VELOCITY_THRESHOLD = 0.5;
const SNAP_VELOCITY_MULTIPLIER = 300;
const MAX_SNAP_VELOCITY = 4;
const DEFAULT_SWIPE_OPEN_RATIO = 0.5;
const VELOCITY_THRESHOLD = 0.1;
const FALLBACK_SWIPE_OPEN_THRESHOLD = 40;

function getRoot(rootId) {
    let root = state.roots.get(rootId);
    if (!root) {
        root = {
            rootId,
            rootDotNetRef: null,
            popupDotNetRef: null,
            viewportDotNetRef: null,
            isOpen: false,
            mounted: false,
            popupElement: null,
            viewportElement: null,
            backdropElement: null,
            popupResizeObserver: null,
            viewportCleanup: null,
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
            suppressOutsidePressUntil: 0
        };
        state.roots.set(rootId, root);
    }
    return root;
}

function registerCssVars() {
    if (state.registeredCssVars) {
        return;
    }

    if (typeof CSS !== 'undefined' && 'registerProperty' in CSS) {
        [
            '--drawer-swipe-movement-x',
            '--drawer-swipe-movement-y',
            '--drawer-snap-point-offset'
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

export function initializeRootReporter(rootId, dotNetRef) {
    const root = getRoot(rootId);
    root.rootDotNetRef = dotNetRef;
}

export function disposeRootReporter(rootId) {
    const root = state.roots.get(rootId);
    if (!root) {
        return;
    }

    cleanupCloseWatcher(root);
    cleanupViewport(root);
    cleanupPopup(root);

    for (const [areaId] of root.swipeAreas) {
        disposeSwipeArea(rootId, areaId);
    }

    root.rootDotNetRef = null;
    state.roots.delete(rootId);
}

export function setRootOpen(rootId, open) {
    const root = getRoot(rootId);
    root.isOpen = !!open;

    if (root.isOpen) {
        setupCloseWatcher(root);
    } else {
        cleanupCloseWatcher(root);
        clearSwipeStyles(root);
    }
}

export function initializePopup(rootId, element, dotNetRef, swipeDirection) {
    const root = getRoot(rootId);
    registerCssVars();

    cleanupPopup(root);
    root.popupElement = element;
    root.popupDotNetRef = dotNetRef;
    root.swipeDirection = swipeDirection || root.swipeDirection || 'down';
    applyOutsidePressSuppression(root);

    if (element && typeof ResizeObserver === 'function') {
        root.popupResizeObserver = new ResizeObserver(() => measurePopup(root));
        root.popupResizeObserver.observe(element);
    }

    measurePopup(root);
    setupViewport(root);
    updateSnapPointOffset(root);

    if (root.isOpen) {
        setupCloseWatcher(root);
    }
}

export function updatePopup(
    rootId,
    open,
    mounted,
    swipeDirection,
    snapPoints,
    activeSnapPoint,
    frontmostHeight
) {
    const root = getRoot(rootId);
    root.isOpen = !!open;
    root.mounted = !!mounted;
    root.swipeDirection = swipeDirection || 'down';
    root.snapPoints = Array.isArray(snapPoints) && snapPoints.length > 0 ? snapPoints : null;
    root.activeSnapPoint = activeSnapPoint ?? null;
    root.frontmostHeight = Number.isFinite(frontmostHeight) ? frontmostHeight : 0;

    measureViewport(root);
    updateSnapPointOffset(root);
    setupViewport(root);

    if (root.isOpen) {
        setupCloseWatcher(root);
    } else {
        cleanupCloseWatcher(root);
    }
}

export function disposePopup(rootId) {
    const root = state.roots.get(rootId);
    if (!root) {
        return;
    }

    cleanupPopup(root);
}

export function setBackdropElement(rootId, element) {
    const root = getRoot(rootId);
    root.backdropElement = element || null;
    applyOutsidePressSuppression(root);
}

export function initializeViewport(rootId, element, dotNetRef, swipeDirection, snapToSequentialPoints) {
    const root = getRoot(rootId);
    root.viewportElement = element;
    root.viewportDotNetRef = dotNetRef;
    root.swipeDirection = swipeDirection || root.swipeDirection || 'down';
    root.snapToSequentialPoints = !!snapToSequentialPoints;
    measureViewport(root);
    setupViewport(root);
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
    measureViewport(root);
    updateSnapPointOffset(root);
    setupViewport(root);
}

export function disposeViewport(rootId) {
    const root = state.roots.get(rootId);
    if (!root) {
        return;
    }

    cleanupViewport(root);
    root.viewportElement = null;
    root.viewportDotNetRef = null;
}

export function initializeSwipeArea(rootId, areaId, element, dotNetRef, swipeDirection, disabled) {
    const root = getRoot(rootId);
    const existing = root.swipeAreas.get(areaId);
    if (existing?.cleanup) {
        existing.cleanup();
    }

    const area = {
        areaId,
        element,
        dotNetRef,
        swipeDirection: swipeDirection || opposite(root.swipeDirection),
        disabled: !!disabled,
        enabled: !disabled,
        cleanup: null
    };

    root.swipeAreas.set(areaId, area);
    area.cleanup = setupSwipeArea(root, area);
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
}

export function disposeSwipeArea(rootId, areaId) {
    const root = state.roots.get(rootId);
    const area = root?.swipeAreas.get(areaId);
    if (!area) {
        return;
    }

    area.cleanup?.();
    root.swipeAreas.delete(areaId);
}

export function initializeIndent(element) {
    if (!element) {
        return;
    }

    element.style.setProperty('--drawer-swipe-progress', element.style.getPropertyValue('--drawer-swipe-progress') || '0');
}

export function disposeIndent(element) {
    if (!element) {
        return;
    }

    element.style.setProperty('--drawer-swipe-progress', '0');
    element.style.removeProperty('--drawer-height');
}

function setupCloseWatcher(root) {
    if (root.closeWatcher?.popupElement === root.popupElement) {
        return;
    }

    cleanupCloseWatcher(root);

    if (!isAndroid()) {
        return;
    }

    const win = root.popupElement?.ownerDocument?.defaultView || window;
    const CloseWatcherCtor = win.CloseWatcher;
    if (typeof CloseWatcherCtor !== 'function') {
        return;
    }

    const watcher = new CloseWatcherCtor();
    const onClose = (event) => {
        if (!root.isOpen || !root.rootDotNetRef) {
            return;
        }
        event.preventDefault?.();
        root.rootDotNetRef.invokeMethodAsync('OnCloseWatcher').catch(() => { });
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

function cleanupPopup(root) {
    root.popupResizeObserver?.disconnect();
    root.popupResizeObserver = null;
    root.popupElement = null;
    root.popupDotNetRef = null;
}

function cleanupViewport(root) {
    root.viewportCleanup?.();
    root.viewportCleanup = null;
    root.viewportBoundPopupElement = null;
    root.viewportBoundDotNetRef = null;
}

function setupViewport(root) {
    if (!root.popupElement || !root.viewportDotNetRef) {
        return;
    }

    if (
        root.viewportCleanup &&
        root.viewportBoundPopupElement === root.popupElement &&
        root.viewportBoundDotNetRef === root.viewportDotNetRef
    ) {
        return;
    }

    cleanupViewport(root);
    root.viewportCleanup = setupDismissSwipe(root);
    root.viewportBoundPopupElement = root.popupElement;
    root.viewportBoundDotNetRef = root.viewportDotNetRef;
}

function measurePopup(root) {
    const element = root.popupElement;
    if (!element) {
        root.popupHeight = 0;
        return;
    }

    const height = element.offsetHeight || 0;
    if (height === root.popupHeight) {
        return;
    }

    root.popupHeight = height;
    root.popupDotNetRef?.invokeMethodAsync('OnPopupHeightChanged', Math.round(height)).catch(() => { });
    updateSnapPointOffset(root);
}

function measureViewport(root) {
    const doc = root.viewportElement?.ownerDocument || root.popupElement?.ownerDocument || document;
    const html = doc.documentElement;
    root.viewportHeight = root.viewportElement?.offsetHeight || html.clientHeight || window.innerHeight || 0;
    const fontSize = Number.parseFloat(getComputedStyle(html).fontSize);
    root.rootFontSize = Number.isFinite(fontSize) ? fontSize : 16;
}

function setupDismissSwipe(root) {
    const element = root.popupElement;
    let start = null;
    let last = null;
    let swiping = false;
    let activePointerId = null;
    let cleanupDocumentListeners = null;

    const onPointerDown = (event) => {
        if (!root.isOpen || !root.mounted || event.button !== 0) {
            return;
        }

        const target = event.target instanceof Element ? event.target : null;
        if (target?.closest(SWIPE_IGNORE_SELECTOR) || target?.closest(CONTENT_SELECTOR)) {
            return;
        }

        start = pointFromPointer(event);
        last = start;
        swiping = false;
        activePointerId = event.pointerId;
        trySetPointerCapture(element, event.pointerId);
        bindDocumentListeners(event);
    };

    const onPointerMove = (event) => {
        if (activePointerId !== null && event.pointerId !== activePointerId) {
            return;
        }

        if (!start) {
            return;
        }

        last = pointFromPointer(event);
        const delta = deltaFrom(start, last);
        const displacement = getDisplacement(root.swipeDirection, delta.x, delta.y);
        const hasSnapPoints = Array.isArray(root.snapPoints) && root.snapPoints.length > 0;
        const tracksSnap = hasSnapPoints && (root.swipeDirection === 'down' || root.swipeDirection === 'up');

        if (!swiping && Math.abs(displacement) >= MIN_SWIPE_THRESHOLD) {
            swiping = true;
            setSwiping(root, true);
        }

        if (!swiping) {
            return;
        }

        if (tracksSnap) {
            applySnapDrag(root, delta.y);
        } else {
            applyDismissDrag(root, Math.max(0, displacement));
        }

        event.preventDefault();
    };

    const onPointerUp = (event) => {
        if (activePointerId !== null && event.pointerId !== activePointerId) {
            return;
        }

        if (!start || !last) {
            reset();
            return;
        }

        last = pointFromPointer(event);
        const delta = deltaFrom(start, last);
        const elapsedMs = Math.max(1, performance.now() - start.time);
        const velocityX = delta.x / elapsedMs;
        const velocityY = delta.y / elapsedMs;
        const direction = dominantDirection(delta.x, delta.y);
        const hasSnapPoints = Array.isArray(root.snapPoints) && root.snapPoints.length > 0;

        if (hasSnapPoints && (root.swipeDirection === 'down' || root.swipeDirection === 'up')) {
            releaseSnapDrag(root, delta.y, velocityY);
        } else {
            releaseDismissDrag(root, direction, delta.x, delta.y, velocityX, velocityY);
        }

        reset();
    };

    const onPointerCancel = (event) => {
        if (activePointerId !== null && event.pointerId !== activePointerId) {
            return;
        }

        clearSwipeStyles(root);
        reset();
    };

    const bindDocumentListeners = (event) => {
        cleanupDocumentListeners?.();
        const doc = element.ownerDocument || document;
        const options = { capture: true };
        doc.addEventListener('pointermove', onPointerMove, options);
        doc.addEventListener('pointerup', onPointerUp, options);
        doc.addEventListener('pointercancel', onPointerCancel, options);
        cleanupDocumentListeners = () => {
            doc.removeEventListener('pointermove', onPointerMove, options);
            doc.removeEventListener('pointerup', onPointerUp, options);
            doc.removeEventListener('pointercancel', onPointerCancel, options);
        };
    };

    const reset = () => {
        cleanupDocumentListeners?.();
        cleanupDocumentListeners = null;
        activePointerId = null;
        start = null;
        last = null;
        if (swiping) {
            setSwiping(root, false);
        }
        swiping = false;
    };

    element.addEventListener('pointerdown', onPointerDown);

    return () => {
        cleanupDocumentListeners?.();
        element.removeEventListener('pointerdown', onPointerDown);
    };
}

function setupSwipeArea(root, area) {
    const element = area.element;
    let start = null;
    let last = null;
    let opened = false;
    let activePointerId = null;
    let cleanupDocumentListeners = null;

    const onPointerDown = (event) => {
        if (area.disabled || !area.enabled || event.button !== 0) {
            return;
        }

        start = pointFromPointer(event);
        last = start;
        opened = false;
        activePointerId = event.pointerId;
        suppressOutsidePress(root);
        area.dotNetRef?.invokeMethodAsync('OnSwipingChanged', true).catch(() => { });
        trySetPointerCapture(element, event.pointerId);
        bindDocumentListeners(event);
        if (event.cancelable) {
            event.preventDefault();
        }
    };

    const onPointerMove = (event) => {
        if (activePointerId !== null && event.pointerId !== activePointerId) {
            return;
        }

        if (!start) {
            return;
        }

        last = pointFromPointer(event);
        const delta = deltaFrom(start, last);
        const displacement = getDisplacement(area.swipeDirection, delta.x, delta.y);

        if (!opened && displacement > MIN_SWIPE_THRESHOLD) {
            opened = true;
            suppressOutsidePress(root);
            area.dotNetRef?.invokeMethodAsync('OnSwipeOpen').catch(() => { });
        }

        if (opened) {
            applySwipeAreaMovement(root, area.swipeDirection, delta.x, delta.y);
        }

        if (event.cancelable) {
            event.preventDefault();
        }
    };

    const onPointerUp = (event) => {
        if (activePointerId !== null && event.pointerId !== activePointerId) {
            return;
        }

        if (!start || !last) {
            finish();
            return;
        }

        last = pointFromPointer(event);
        const delta = deltaFrom(start, last);
        const elapsedMs = Math.max(1, performance.now() - start.time);
        const releaseVelocity = getDisplacement(area.swipeDirection, delta.x / elapsedMs, delta.y / elapsedMs);
        const displacement = getDisplacement(area.swipeDirection, delta.x, delta.y);
        const threshold = root.popupElement
            ? (isHorizontal(area.swipeDirection) ? root.popupElement.offsetWidth : root.popupElement.offsetHeight) * DEFAULT_SWIPE_OPEN_RATIO
            : FALLBACK_SWIPE_OPEN_THRESHOLD;

        if (!opened && (displacement >= threshold || releaseVelocity >= VELOCITY_THRESHOLD)) {
            suppressOutsidePress(root);
            area.dotNetRef?.invokeMethodAsync('OnSwipeOpen').catch(() => { });
        }

        finish();
    };

    const onPointerCancel = (event) => {
        if (activePointerId !== null && event.pointerId !== activePointerId) {
            return;
        }

        finish();
    };

    const bindDocumentListeners = (event) => {
        cleanupDocumentListeners?.();
        const doc = element.ownerDocument || document;
        const options = { capture: true };
        doc.addEventListener('pointermove', onPointerMove, options);
        doc.addEventListener('pointerup', onPointerUp, options);
        doc.addEventListener('pointercancel', onPointerCancel, options);
        cleanupDocumentListeners = () => {
            doc.removeEventListener('pointermove', onPointerMove, options);
            doc.removeEventListener('pointerup', onPointerUp, options);
            doc.removeEventListener('pointercancel', onPointerCancel, options);
        };
    };

    const finish = () => {
        cleanupDocumentListeners?.();
        cleanupDocumentListeners = null;
        activePointerId = null;
        start = null;
        last = null;
        opened = false;
        area.dotNetRef?.invokeMethodAsync('OnSwipingChanged', false).catch(() => { });
        clearSwipeStyles(root);
    };

    element.addEventListener('pointerdown', onPointerDown);

    return () => {
        cleanupDocumentListeners?.();
        element.removeEventListener('pointerdown', onPointerDown);
    };
}

function applyDismissDrag(root, displacement) {
    const popup = root.popupElement;
    if (!popup) {
        return;
    }

    const size = isHorizontal(root.swipeDirection) ? popup.offsetWidth : popup.offsetHeight;
    if (!Number.isFinite(size) || size <= 0) {
        return;
    }

    const damped = displacement > size ? size + Math.sqrt(displacement - size) : displacement;
    const directionSign = root.swipeDirection === 'left' || root.swipeDirection === 'up' ? -1 : 1;
    const movement = damped * directionSign;
    popup.style.setProperty('--drawer-swipe-movement-x', isHorizontal(root.swipeDirection) ? `${movement}px` : '0px');
    popup.style.setProperty('--drawer-swipe-movement-y', isHorizontal(root.swipeDirection) ? '0px' : `${movement}px`);
    popup.setAttribute(DATA_SWIPING, '');
    const progress = Math.max(0, Math.min(1, displacement / size));
    applyBackdropProgress(root, progress);
    root.viewportDotNetRef?.invokeMethodAsync('OnSwipeProgress', progress).catch(() => { });
}

function releaseDismissDrag(root, direction, deltaX, deltaY, velocityX, velocityY) {
    const popup = root.popupElement;
    if (!popup || !direction) {
        clearSwipeStyles(root);
        return;
    }

    const baseThreshold = Math.max((isHorizontal(direction) ? popup.offsetWidth : popup.offsetHeight) * 0.5, MIN_SWIPE_THRESHOLD);
    const axisDelta = isHorizontal(direction) ? deltaX : deltaY;
    const directionalDelta = direction === 'left' || direction === 'up' ? -axisDelta : axisDelta;
    const axisVelocity = isHorizontal(direction) ? velocityX : velocityY;
    const directionalVelocity = direction === 'left' || direction === 'up' ? -axisVelocity : axisVelocity;

    if (direction === root.swipeDirection && (directionalDelta > baseThreshold || directionalVelocity >= FAST_SWIPE_VELOCITY)) {
        setSwipeDismissed(root, true);
        root.viewportDotNetRef?.invokeMethodAsync('OnSwipeDismiss').catch(() => { });
        return;
    }

    clearSwipeStyles(root);
}

function applySnapDrag(root, deltaY) {
    const popup = root.popupElement;
    if (!popup) {
        return;
    }

    const points = resolveSnapPoints(root);
    if (!points.length) {
        return;
    }

    const currentOffset = root.activeSnapPointOffset ?? points[0].offset;
    const dragDelta = root.swipeDirection === 'down' ? deltaY : -deltaY;
    const nextOffset = clamp(currentOffset + dragDelta, 0, root.popupHeight);
    const movement = root.swipeDirection === 'down'
        ? nextOffset - currentOffset
        : -(nextOffset - currentOffset);
    popup.style.setProperty('--drawer-swipe-movement-y', `${movement}px`);
    popup.style.setProperty('--drawer-swipe-movement-x', '0px');
    popup.setAttribute(DATA_SWIPING, '');

    const minOffset = Math.min(...points.map((point) => point.offset));
    const maxOffset = Math.max(...points.map((point) => point.offset), root.popupHeight);
    const progress = maxOffset > minOffset ? Math.max(0, Math.min(1, (nextOffset - minOffset) / (maxOffset - minOffset))) : 0;
    applyBackdropProgress(root, progress);
    root.viewportDotNetRef?.invokeMethodAsync('OnSwipeProgress', progress).catch(() => { });
}

function releaseSnapDrag(root, deltaY, velocityY) {
    const points = resolveSnapPoints(root);
    if (!points.length) {
        clearSwipeStyles(root);
        return;
    }

    const currentOffset = root.activeSnapPointOffset ?? points[0].offset;
    const dragDelta = root.swipeDirection === 'down' ? deltaY : -deltaY;
    const resolvedVelocity = root.swipeDirection === 'down' ? velocityY : -velocityY;
    const dragTargetOffset = clamp(currentOffset + dragDelta, 0, root.popupHeight);
    const velocityOffset =
        Number.isFinite(resolvedVelocity) && Math.abs(resolvedVelocity) >= SNAP_VELOCITY_THRESHOLD
            ? clamp(resolvedVelocity, -MAX_SNAP_VELOCITY, MAX_SNAP_VELOCITY) * SNAP_VELOCITY_MULTIPLIER
            : 0;
    const targetOffset = root.snapToSequentialPoints
        ? dragTargetOffset
        : clamp(dragTargetOffset + velocityOffset, 0, root.popupHeight);

    const closeFromSnapPoints = () => {
        root.viewportDotNetRef?.invokeMethodAsync('OnSnapPointChange', null).catch(() => { });
        setSwipeDismissed(root, true);
        root.viewportDotNetRef?.invokeMethodAsync('OnSwipeDismiss').catch(() => { });
    };

    if (root.snapToSequentialPoints) {
        const orderedPoints = [...points].sort((first, second) => first.offset - second.offset);
        let currentIndex = 0;
        let currentDistance = Math.abs(currentOffset - orderedPoints[0].offset);
        for (let index = 1; index < orderedPoints.length; index += 1) {
            const nextDistance = Math.abs(currentOffset - orderedPoints[index].offset);
            if (nextDistance < currentDistance) {
                currentDistance = nextDistance;
                currentIndex = index;
            }
        }

        let targetPoint = orderedPoints[0];
        let closestDistance = Math.abs(targetOffset - targetPoint.offset);
        for (const point of orderedPoints) {
            const nextDistance = Math.abs(targetOffset - point.offset);
            if (nextDistance < closestDistance) {
                closestDistance = nextDistance;
                targetPoint = point;
            }
        }

        const dragDirection = Math.sign(dragDelta);
        const velocityDirection = Math.sign(resolvedVelocity);
        const shouldAdvance =
            dragDirection !== 0 &&
            velocityDirection !== 0 &&
            velocityDirection === dragDirection &&
            Math.abs(resolvedVelocity) >= SNAP_VELOCITY_THRESHOLD;
        let effectiveTargetOffset = targetOffset;

        if (shouldAdvance) {
            const adjacentIndex = clamp(currentIndex + dragDirection, 0, orderedPoints.length - 1);
            if (adjacentIndex !== currentIndex) {
                const adjacentPoint = orderedPoints[adjacentIndex];
                const shouldForceAdjacent = dragDirection > 0
                    ? targetOffset < adjacentPoint.offset
                    : targetOffset > adjacentPoint.offset;
                if (shouldForceAdjacent) {
                    targetPoint = adjacentPoint;
                    effectiveTargetOffset = adjacentPoint.offset;
                }
            } else if (dragDirection > 0) {
                closeFromSnapPoints();
                return;
            }
        }

        const closeDistance = Math.abs(effectiveTargetOffset - root.popupHeight);
        const snapDistance = Math.abs(effectiveTargetOffset - targetPoint.offset);
        if (closeDistance < snapDistance) {
            closeFromSnapPoints();
            return;
        }

        root.viewportDotNetRef?.invokeMethodAsync('OnSnapPointChange', targetPoint.value).catch(() => { });
        clearSwipeStyles(root);
        return;
    }

    if (resolvedVelocity >= FAST_SWIPE_VELOCITY && dragDelta > 0) {
        closeFromSnapPoints();
        return;
    }

    let target = points[0];
    let distance = Math.abs(targetOffset - target.offset);
    for (const point of points) {
        const nextDistance = Math.abs(targetOffset - point.offset);
        if (nextDistance < distance) {
            distance = nextDistance;
            target = point;
        }
    }

    const closeDistance = Math.abs(targetOffset - root.popupHeight);
    if (closeDistance < distance) {
        closeFromSnapPoints();
        return;
    }

    root.viewportDotNetRef?.invokeMethodAsync('OnSnapPointChange', target.value).catch(() => { });
    clearSwipeStyles(root);
}

function applySwipeAreaMovement(root, openDirection, deltaX, deltaY) {
    const popup = root.popupElement;
    if (!popup) {
        return;
    }

    const dismissDirection = opposite(openDirection);
    const size = isHorizontal(dismissDirection) ? popup.offsetWidth : popup.offsetHeight;
    if (!Number.isFinite(size) || size <= 0) {
        return;
    }

    const displacement = Math.max(0, getDisplacement(openDirection, deltaX, deltaY));
    const remaining = Math.max(0, size - displacement);
    const directionSign = dismissDirection === 'left' || dismissDirection === 'up' ? -1 : 1;
    const movement = remaining * directionSign;
    popup.style.setProperty('--drawer-swipe-movement-x', isHorizontal(dismissDirection) ? `${movement}px` : '0px');
    popup.style.setProperty('--drawer-swipe-movement-y', isHorizontal(dismissDirection) ? '0px' : `${movement}px`);
    popup.setAttribute(DATA_SWIPING, '');
    const progress = Math.max(0, Math.min(1, displacement / size));
    applyBackdropProgress(root, 1 - progress);
}

function updateSnapPointOffset(root) {
    const popup = root.popupElement;
    if (!popup) {
        return;
    }

    const points = resolveSnapPoints(root);
    let offset = 0;
    if (points.length && root.activeSnapPoint !== null) {
        const exact = points.find((point) => point.value === root.activeSnapPoint);
        offset = exact?.offset ?? points[0].offset;
    }

    const signedOffset = root.swipeDirection === 'up' ? -offset : offset;
    root.activeSnapPointOffset = offset;
    popup.style.setProperty('--drawer-snap-point-offset', `${signedOffset}px`);
    popup.style.setProperty('--drawer-swipe-strength', '1');

    if (root.frontmostHeight > 0) {
        popup.style.setProperty('--drawer-frontmost-height', `${root.frontmostHeight}px`);
    }
}

function resolveSnapPoints(root) {
    if (!Array.isArray(root.snapPoints) || !root.snapPoints.length || root.viewportHeight <= 0 || root.popupHeight <= 0) {
        return [];
    }

    const maxHeight = Math.min(root.popupHeight, root.viewportHeight);
    const resolved = [];
    for (const value of root.snapPoints) {
        const height = resolveSnapPointValue(value, root.viewportHeight, root.rootFontSize);
        if (height === null || !Number.isFinite(height)) {
            continue;
        }

        const clampedHeight = Math.max(0, Math.min(maxHeight, height));
        resolved.push({
            value: String(value),
            height: clampedHeight,
            offset: Math.max(0, root.popupHeight - clampedHeight)
        });
    }

    const deduped = [];
    const seenHeights = [];
    for (let index = resolved.length - 1; index >= 0; index -= 1) {
        const point = resolved[index];
        if (seenHeights.some((height) => Math.abs(height - point.height) <= 1)) {
            continue;
        }
        seenHeights.push(point.height);
        deduped.push(point);
    }
    deduped.reverse();
    return deduped;
}

function resolveSnapPointValue(value, viewportHeight, rootFontSize) {
    if (!Number.isFinite(viewportHeight) || viewportHeight <= 0) {
        return null;
    }

    const numeric = Number.parseFloat(value);
    const asString = String(value).trim();

    if (/^-?\d+(\.\d+)?$/.test(asString)) {
        if (!Number.isFinite(numeric)) {
            return null;
        }

        return numeric <= 1 ? Math.max(0, Math.min(1, numeric)) * viewportHeight : numeric;
    }

    if (asString.endsWith('px')) {
        return Number.isFinite(numeric) ? numeric : null;
    }

    if (asString.endsWith('rem')) {
        return Number.isFinite(numeric) ? numeric * rootFontSize : null;
    }

    return null;
}

function setSwiping(root, swiping) {
    root.popupElement?.toggleAttribute(DATA_SWIPING, swiping);
    root.backdropElement?.toggleAttribute(DATA_SWIPING, swiping);
    root.viewportDotNetRef?.invokeMethodAsync('OnSwipingChanged', swiping).catch(() => { });
}

function clearSwipeStyles(root) {
    const popup = root.popupElement;
    if (popup) {
        popup.style.removeProperty('--drawer-swipe-movement-x');
        popup.style.removeProperty('--drawer-swipe-movement-y');
        popup.style.setProperty('--drawer-swipe-strength', '1');
        popup.removeAttribute(DATA_SWIPING);
        popup.removeAttribute(DATA_SWIPE_DISMISS);
    }

    applyBackdropProgress(root, 0);
    root.backdropElement?.removeAttribute(DATA_SWIPING);
    root.backdropElement?.removeAttribute(DATA_SWIPE_DISMISS);
    root.viewportDotNetRef?.invokeMethodAsync('OnSwipeProgress', 0).catch(() => { });
}

function setSwipeDismissed(root, dismissed) {
    root.popupElement?.toggleAttribute(DATA_SWIPE_DISMISS, dismissed);
    root.backdropElement?.toggleAttribute(DATA_SWIPE_DISMISS, dismissed);
}

function applyBackdropProgress(root, progress) {
    if (!root.backdropElement) {
        return;
    }

    root.backdropElement.style.setProperty('--drawer-swipe-progress', `${Math.max(0, Math.min(1, progress))}`);
    if (progress > 0 && root.frontmostHeight > 0) {
        root.backdropElement.style.setProperty('--drawer-height', `${root.frontmostHeight}px`);
    } else {
        root.backdropElement.style.removeProperty('--drawer-height');
    }
}

function pointFromPointer(event) {
    return {
        x: event.clientX,
        y: event.clientY,
        time: performance.now()
    };
}

function deltaFrom(start, end) {
    return {
        x: end.x - start.x,
        y: end.y - start.y
    };
}

function getDisplacement(direction, deltaX, deltaY) {
    switch (direction) {
        case 'up':
            return -deltaY;
        case 'down':
            return deltaY;
        case 'left':
            return -deltaX;
        case 'right':
            return deltaX;
        default:
            return deltaY;
    }
}

function dominantDirection(deltaX, deltaY) {
    if (Math.abs(deltaX) > Math.abs(deltaY)) {
        return deltaX < 0 ? 'left' : 'right';
    }

    if (Math.abs(deltaY) > 0) {
        return deltaY < 0 ? 'up' : 'down';
    }

    return undefined;
}

function isHorizontal(direction) {
    return direction === 'left' || direction === 'right';
}

function opposite(direction) {
    switch (direction) {
        case 'up':
            return 'down';
        case 'down':
            return 'up';
        case 'left':
            return 'right';
        case 'right':
            return 'left';
        default:
            return 'up';
    }
}

function trySetPointerCapture(element, pointerId) {
    try {
        element?.setPointerCapture?.(pointerId);
    } catch {
        // Pointer capture can fail if the pointer is already released.
    }
}

function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
}

function suppressOutsidePress(root) {
    const now = performance.now();
    root.suppressOutsidePressUntil = Math.max(root.suppressOutsidePressUntil || 0, now + 750);
    applyOutsidePressSuppression(root);
}

function applyOutsidePressSuppression(root) {
    const until = root.suppressOutsidePressUntil || 0;
    if (until <= performance.now()) {
        return;
    }

    if (root.popupElement) {
        root.popupElement.__blazixBaseUIDrawerSuppressOutsidePressUntil = until;
    }
    if (root.backdropElement) {
        root.backdropElement.__blazixBaseUIDrawerSuppressOutsidePressUntil = until;
    }
}

function isAndroid() {
    return /Android/i.test(navigator.userAgent);
}
