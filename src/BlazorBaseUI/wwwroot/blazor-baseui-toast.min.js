import {
    activeElement,
    contains,
    getTarget,
    initializePositioner as initializeFloatingPositioner,
    updatePositioner,
    disposePositioner as disposeFloatingPositioner
} from './blazor-baseui-floating.min.js';

const STATE_KEY = Symbol.for('BlazorBaseUI.Toast.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        viewports: new Map(),
        roots: new Map(),
        contents: new Map(),
        positioners: new Map()
    };
}

const state = window[STATE_KEY];
const SWIPE_THRESHOLD = 40;
const REVERSE_CANCEL_THRESHOLD = 10;
const OPPOSITE_DIRECTION_DAMPING_FACTOR = 0.5;
const MIN_DRAG_THRESHOLD = 1;
const SWIPE_IGNORE_SELECTOR = '[data-blazor-base-ui-swipe-ignore], [data-swipe-ignore], button, a, input, textarea, [role="button"]';

export function initializeViewport(viewportId, viewport, dotNetRef) {
    disposeViewport(viewportId);

    const entry = {
        viewport,
        dotNetRef,
        isEmpty: true,
        prevFocusElement: null,
        touchActive: false,
        markedReadyForMouseLeave: false,
        cleanups: []
    };

    const win = viewport.ownerDocument.defaultView || window;
    const doc = viewport.ownerDocument;

    const onKeyDown = (event) => {
        if (entry.isEmpty || event.key !== 'F6' || getTarget(event) === viewport) {
            return;
        }

        event.preventDefault();
        entry.prevFocusElement = activeElement(doc);
        viewport.focus({ preventScroll: true });
        dotNetRef.invokeMethodAsync('OnGlobalFocusHotkey').catch(() => {});
    };

    const onWindowBlur = (event) => {
        if (getTarget(event) === win) {
            dotNetRef.invokeMethodAsync('OnWindowBlur').catch(() => {});
        }
    };

    const onWindowFocus = (event) => {
        if (event.relatedTarget) {
            return;
        }

        const target = getTarget(event);
        const currentActive = activeElement(doc);
        const shouldResume = target === win ||
            !contains(viewport, target) ||
            !matchesFocusVisible(currentActive);

        setTimeout(() => dotNetRef.invokeMethodAsync('OnWindowFocus', shouldResume).catch(() => {}), 0);
    };

    const onDocumentPointerDown = (event) => {
        if (event.pointerType !== 'touch') {
            return;
        }

        if (contains(viewport, getTarget(event))) {
            return;
        }

        dotNetRef.invokeMethodAsync('OnDocumentTouchPointerDownOutside').catch(() => {});
    };

    const onFocus = () => {
        dotNetRef.invokeMethodAsync('OnViewportFocus', matchesFocusVisible(activeElement(doc))).catch(() => {});
    };

    const onBlur = (event) => {
        dotNetRef.invokeMethodAsync('OnViewportBlur', contains(viewport, event.relatedTarget)).catch(() => {});
    };

    win.addEventListener('keydown', onKeyDown);
    win.addEventListener('blur', onWindowBlur, true);
    win.addEventListener('focus', onWindowFocus, true);
    doc.addEventListener('pointerdown', onDocumentPointerDown, true);
    viewport.addEventListener('focus', onFocus);
    viewport.addEventListener('blur', onBlur);

    entry.cleanups.push(() => win.removeEventListener('keydown', onKeyDown));
    entry.cleanups.push(() => win.removeEventListener('blur', onWindowBlur, true));
    entry.cleanups.push(() => win.removeEventListener('focus', onWindowFocus, true));
    entry.cleanups.push(() => doc.removeEventListener('pointerdown', onDocumentPointerDown, true));
    entry.cleanups.push(() => viewport.removeEventListener('focus', onFocus));
    entry.cleanups.push(() => viewport.removeEventListener('blur', onBlur));

    state.viewports.set(viewportId, entry);
}

export function updateViewport(viewportId, isEmpty) {
    const entry = state.viewports.get(viewportId);
    if (entry) {
        entry.isEmpty = isEmpty;
    }
}

export function disposeViewport(viewportId) {
    const entry = state.viewports.get(viewportId);
    if (!entry) {
        return;
    }

    for (const cleanup of entry.cleanups) {
        cleanup();
    }

    state.viewports.delete(viewportId);
}

export function handleFocusGuard(viewportId) {
    const entry = state.viewports.get(viewportId);
    if (!entry) {
        return;
    }

    const firstToast = entry.viewport.querySelector('[data-blazor-base-ui-toast-root]:not([data-ending-style])');
    if (entry.viewport.ownerDocument.activeElement === entry.viewport && firstToast instanceof HTMLElement) {
        firstToast.focus({ preventScroll: true });
        return;
    }

    entry.prevFocusElement?.focus?.({ preventScroll: true });
}

export function handleFocusAfterClose(viewportId, toastId) {
    const entry = state.viewports.get(viewportId);
    if (!entry) {
        return;
    }

    const { viewport } = entry;
    const doc = viewport.ownerDocument;
    const currentActive = activeElement(doc);
    if (!contains(viewport, currentActive) || !matchesFocusVisible(currentActive)) {
        return;
    }

    if (toastId == null) {
        entry.prevFocusElement?.focus?.({ preventScroll: true });
        return;
    }

    const selector = `[data-blazor-base-ui-toast-root]:not([data-ending-style])`;
    const remaining = Array.from(viewport.querySelectorAll(selector));
    const next = remaining.find((toast) => toast.getAttribute('data-toast-id') !== toastId);
    if (next instanceof HTMLElement) {
        next.focus({ preventScroll: true });
    } else {
        entry.prevFocusElement?.focus?.({ preventScroll: true });
    }
}

export function initializeContent(contentId, element) {
    disposeContent(contentId);

    const root = element.closest('[data-blazor-base-ui-toast-root]');
    const rootEntry = root ? findRootEntry(root) : null;
    if (!rootEntry) {
        return;
    }

    const recalculate = () => measureRoot(rootEntry);
    recalculate();

    const resizeObserver = typeof ResizeObserver === 'function'
        ? new ResizeObserver(recalculate)
        : null;
    const mutationObserver = typeof MutationObserver === 'function'
        ? new MutationObserver(recalculate)
        : null;

    resizeObserver?.observe(element);
    mutationObserver?.observe(element, { childList: true, subtree: true, characterData: true });

    state.contents.set(contentId, {
        resizeObserver,
        mutationObserver
    });
}

export function disposeContent(contentId) {
    const entry = state.contents.get(contentId);
    if (!entry) {
        return;
    }

    entry.resizeObserver?.disconnect();
    entry.mutationObserver?.disconnect();
    state.contents.delete(contentId);
}

export function initializeRoot(rootId, element, dotNetRef, swipeEnabled, swipeDirections) {
    disposeRoot(rootId);

    const entry = {
        rootId,
        element,
        dotNetRef,
        swipeEnabled,
        swipeDirections: swipeDirections || [],
        activePointerId: null,
        dragStart: { x: 0, y: 0 },
        swipeCancelBaseline: { x: 0, y: 0 },
        initialTransform: { x: 0, y: 0, scale: 1 },
        dragOffset: { x: 0, y: 0 },
        isSwiping: false,
        isRealSwipe: false,
        intendedDirection: null,
        lockedDirection: null,
        cancelledSwipe: false,
        isFirstPointerMove: false,
        maxSwipeDisplacement: 0,
        abortController: null,
        cleanups: []
    };

    const onTransitionEnd = () => {
        if (element.hasAttribute('data-ending-style')) {
            dotNetRef.invokeMethodAsync('OnTransitionComplete').catch(() => {});
        }
    };

    const onTouchMove = (event) => {
        if (swipeEnabled && contains(element, getTarget(event))) {
            event.preventDefault();
        }
    };
    const onPointerDown = (event) => handlePointerDown(entry, event);
    const onPointerMove = (event) => handlePointerMove(entry, event);
    const onPointerUp = (event) => handleSwipeEnd(entry, event);
    const onPointerCancel = (event) => handleSwipeEnd(entry, event);

    element.addEventListener('transitionend', onTransitionEnd);
    element.addEventListener('animationend', onTransitionEnd);
    element.addEventListener('touchmove', onTouchMove, { passive: false });
    element.addEventListener('pointerdown', onPointerDown);
    element.addEventListener('pointermove', onPointerMove);
    element.addEventListener('pointerup', onPointerUp);
    element.addEventListener('pointercancel', onPointerCancel);

    entry.cleanups.push(() => element.removeEventListener('transitionend', onTransitionEnd));
    entry.cleanups.push(() => element.removeEventListener('animationend', onTransitionEnd));
    entry.cleanups.push(() => element.removeEventListener('touchmove', onTouchMove));
    entry.cleanups.push(() => element.removeEventListener('pointerdown', onPointerDown));
    entry.cleanups.push(() => element.removeEventListener('pointermove', onPointerMove));
    entry.cleanups.push(() => element.removeEventListener('pointerup', onPointerUp));
    entry.cleanups.push(() => element.removeEventListener('pointercancel', onPointerCancel));

    state.roots.set(rootId, entry);
    measureRoot(entry);
}

export function updateRoot(rootId, element, ending, swipeEnabled, swipeDirections) {
    const entry = state.roots.get(rootId);
    if (!entry) {
        return;
    }

    entry.element = element;
    entry.swipeEnabled = swipeEnabled;
    entry.swipeDirections = swipeDirections || [];

    if (ending && !hasCssTransition(element)) {
        setTimeout(() => entry.dotNetRef.invokeMethodAsync('OnTransitionComplete').catch(() => {}), 0);
    }
}

export function disposeRoot(rootId) {
    const entry = state.roots.get(rootId);
    if (!entry) {
        return;
    }

    entry.abortController?.abort();
    for (const cleanup of entry.cleanups) {
        cleanup();
    }

    state.roots.delete(rootId);
}

export async function initializePositioner(
    positionerElement,
    anchorElement,
    side,
    align,
    sideOffset,
    alignOffset,
    collisionPadding,
    collisionBoundary,
    arrowPadding,
    arrowElement,
    sticky,
    positionMethod,
    disableAnchorTracking,
    collisionAvoidanceSide,
    collisionAvoidanceAlign,
    collisionAvoidanceFallbackAxisSide,
    dotNetRef
) {
    const positionerId = await initializeFloatingPositioner({
        positionerElement,
        triggerElement: anchorElement,
        side,
        align,
        sideOffset,
        alignOffset,
        collisionPadding,
        collisionBoundary: collisionBoundary || 'clipping-ancestors',
        arrowPadding,
        arrowElement,
        sticky: sticky || false,
        positionMethod: positionMethod || 'absolute',
        disableAnchorTracking: disableAnchorTracking || false,
        collisionAvoidance: {
            side: collisionAvoidanceSide || 'flip',
            align: collisionAvoidanceAlign || 'flip',
            fallbackAxisSide: collisionAvoidanceFallbackAxisSide || 'end'
        },
        onPositionUpdated: (nextSide, nextAlign, anchorHidden, arrowUncentered) => {
            dotNetRef?.invokeMethodAsync('OnPositionUpdated', nextSide, nextAlign, anchorHidden, arrowUncentered).catch(() => {});
        },
        dotNetRef: dotNetRef || null
    });

    if (positionerId) {
        state.positioners.set(positionerId, {});
    }

    return positionerId;
}

export async function updatePosition(
    positionerId,
    anchorElement,
    side,
    align,
    sideOffset,
    alignOffset,
    collisionPadding,
    collisionBoundary,
    arrowPadding,
    arrowElement,
    sticky,
    positionMethod,
    collisionAvoidanceSide,
    collisionAvoidanceAlign,
    collisionAvoidanceFallbackAxisSide
) {
    await updatePositioner(positionerId, {
        triggerElement: anchorElement,
        side,
        align,
        sideOffset,
        alignOffset,
        collisionPadding,
        collisionBoundary: collisionBoundary || 'clipping-ancestors',
        arrowPadding,
        arrowElement,
        sticky: sticky || false,
        positionMethod: positionMethod || 'absolute',
        collisionAvoidance: {
            side: collisionAvoidanceSide || 'flip',
            align: collisionAvoidanceAlign || 'flip',
            fallbackAxisSide: collisionAvoidanceFallbackAxisSide || 'end'
        }
    });
}

export function disposePositioner(positionerId) {
    disposeFloatingPositioner(positionerId);
    state.positioners.delete(positionerId);
}

function handlePointerDown(entry, event) {
    if (!entry.swipeEnabled || event.button !== 0) {
        return;
    }

    const target = getTarget(event);
    if (target?.closest?.(SWIPE_IGNORE_SELECTOR)) {
        return;
    }

    entry.activePointerId = event.pointerId;
    entry.dragStart = { x: event.clientX, y: event.clientY };
    entry.swipeCancelBaseline = entry.dragStart;
    entry.initialTransform = getElementTransform(entry.element);
    entry.dragOffset = { x: entry.initialTransform.x, y: entry.initialTransform.y };
    entry.isSwiping = true;
    entry.isRealSwipe = false;
    entry.intendedDirection = null;
    entry.lockedDirection = null;
    entry.cancelledSwipe = false;
    entry.isFirstPointerMove = true;
    entry.maxSwipeDisplacement = 0;

    entry.element.setPointerCapture?.(event.pointerId);
    entry.dotNetRef.invokeMethodAsync('OnSwipeStateChanged', true, null).catch(() => {});
}

function handlePointerMove(entry, event) {
    if (event.pointerId !== entry.activePointerId) {
        return;
    }

    event.preventDefault();

    if (entry.isFirstPointerMove) {
        entry.dragStart = { x: event.clientX, y: event.clientY };
        entry.isFirstPointerMove = false;
    }

    const clientX = event.clientX;
    const clientY = event.clientY;
    const movementX = event.movementX || 0;
    const movementY = event.movementY || 0;

    if ((movementY < 0 && clientY > entry.swipeCancelBaseline.y) ||
        (movementY > 0 && clientY < entry.swipeCancelBaseline.y)) {
        entry.swipeCancelBaseline = {
            x: entry.swipeCancelBaseline.x,
            y: clientY
        };
    }

    if ((movementX < 0 && clientX > entry.swipeCancelBaseline.x) ||
        (movementX > 0 && clientX < entry.swipeCancelBaseline.x)) {
        entry.swipeCancelBaseline = {
            x: clientX,
            y: entry.swipeCancelBaseline.y
        };
    }

    const deltaX = clientX - entry.dragStart.x;
    const deltaY = clientY - entry.dragStart.y;
    const cancelDeltaX = clientX - entry.swipeCancelBaseline.x;
    const cancelDeltaY = clientY - entry.swipeCancelBaseline.y;
    const movementDistance = Math.sqrt(deltaX * deltaX + deltaY * deltaY);

    if (!entry.isRealSwipe && movementDistance >= MIN_DRAG_THRESHOLD) {
        entry.isRealSwipe = true;
        if (entry.lockedDirection === null) {
            const hasHorizontal = entry.swipeDirections.includes('left') || entry.swipeDirections.includes('right');
            const hasVertical = entry.swipeDirections.includes('up') || entry.swipeDirections.includes('down');
            if (hasHorizontal && hasVertical) {
                entry.lockedDirection = Math.abs(deltaX) > Math.abs(deltaY) ? 'horizontal' : 'vertical';
            }
        }
    }

    if (!entry.intendedDirection) {
        const candidate = getCandidateDirection(entry, deltaX, deltaY);
        if (candidate && entry.swipeDirections.includes(candidate)) {
            entry.intendedDirection = candidate;
            entry.maxSwipeDisplacement = getDisplacement(candidate, deltaX, deltaY);
            entry.dotNetRef.invokeMethodAsync('OnSwipeStateChanged', true, candidate).catch(() => {});
        }
    } else {
        const currentDisplacement = getDisplacement(entry.intendedDirection, cancelDeltaX, cancelDeltaY);
        if (currentDisplacement > SWIPE_THRESHOLD) {
            entry.cancelledSwipe = false;
            entry.dotNetRef.invokeMethodAsync('OnSwipeStateChanged', true, entry.intendedDirection).catch(() => {});
        } else if (
            !(entry.swipeDirections.includes('left') && entry.swipeDirections.includes('right')) &&
            !(entry.swipeDirections.includes('up') && entry.swipeDirections.includes('down')) &&
            entry.maxSwipeDisplacement - currentDisplacement >= REVERSE_CANCEL_THRESHOLD
        ) {
            entry.cancelledSwipe = true;
        }
    }

    const damped = applyDirectionalDamping(entry, deltaX, deltaY);
    let nextOffsetX = entry.initialTransform.x;
    let nextOffsetY = entry.initialTransform.y;

    if (entry.lockedDirection === 'horizontal') {
        if (entry.swipeDirections.includes('left') || entry.swipeDirections.includes('right')) {
            nextOffsetX += damped.x;
        }
    } else if (entry.lockedDirection === 'vertical') {
        if (entry.swipeDirections.includes('up') || entry.swipeDirections.includes('down')) {
            nextOffsetY += damped.y;
        }
    } else {
        if (entry.swipeDirections.includes('left') || entry.swipeDirections.includes('right')) {
            nextOffsetX += damped.x;
        }

        if (entry.swipeDirections.includes('up') || entry.swipeDirections.includes('down')) {
            nextOffsetY += damped.y;
        }
    }

    entry.dragOffset = {
        x: nextOffsetX,
        y: nextOffsetY
    };

    entry.element.style.transition = 'none';
    entry.element.style.transform = `translateX(${entry.dragOffset.x}px) translateY(${entry.dragOffset.y}px) scale(${entry.initialTransform.scale})`;
    entry.element.style.setProperty('--toast-swipe-movement-x', `${entry.dragOffset.x - entry.initialTransform.x}px`);
    entry.element.style.setProperty('--toast-swipe-movement-y', `${entry.dragOffset.y - entry.initialTransform.y}px`);

    entry.element.toggleAttribute('data-swipe-direction', Boolean(entry.intendedDirection));
    if (entry.intendedDirection) {
        entry.element.setAttribute('data-swipe-direction', entry.intendedDirection);
    }
    entry.element.setAttribute('data-swiping', '');
}

function handleSwipeEnd(entry, event) {
    if (event.pointerId !== entry.activePointerId) {
        return;
    }

    entry.activePointerId = null;
    entry.element.releasePointerCapture?.(event.pointerId);
    entry.element.style.transition = '';
    entry.element.style.transform = '';
    entry.element.removeAttribute('data-swiping');

    const deltaX = entry.dragOffset.x - entry.initialTransform.x;
    const deltaY = entry.dragOffset.y - entry.initialTransform.y;
    const dismissDirection = getDismissDirection(entry.swipeDirections, deltaX, deltaY);

    if (event.type !== 'pointercancel' && !entry.cancelledSwipe && dismissDirection) {
        entry.element.setAttribute('data-swipe-direction', dismissDirection);
        entry.dotNetRef.invokeMethodAsync('OnSwipeStateChanged', false, dismissDirection).catch(() => {});
        entry.dotNetRef.invokeMethodAsync('OnSwipeDismissed').catch(() => {});
    } else {
        entry.element.removeAttribute('data-swipe-direction');
        entry.element.style.setProperty('--toast-swipe-movement-x', '0px');
        entry.element.style.setProperty('--toast-swipe-movement-y', '0px');
        entry.dotNetRef.invokeMethodAsync('OnSwipeStateChanged', false, null).catch(() => {});
    }
}

function measureRoot(entry) {
    const element = entry.element;
    const previousHeight = element.style.height;
    element.style.height = 'auto';
    const height = element.offsetHeight;
    element.style.height = previousHeight;
    entry.dotNetRef.invokeMethodAsync('OnMeasuredHeight', height).catch(() => {});
}

function findRootEntry(element) {
    for (const entry of state.roots.values()) {
        if (entry.element === element) {
            return entry;
        }
    }
    return null;
}

function matchesFocusVisible(element) {
    try {
        return element instanceof HTMLElement && element.matches(':focus-visible');
    } catch {
        return true;
    }
}

function getCandidateDirection(entry, deltaX, deltaY) {
    if (entry.lockedDirection === 'vertical') {
        if (deltaY > 0) {
            return 'down';
        }
        if (deltaY < 0) {
            return 'up';
        }
        return null;
    }

    if (entry.lockedDirection === 'horizontal') {
        if (deltaX > 0) {
            return 'right';
        }
        if (deltaX < 0) {
            return 'left';
        }
        return null;
    }

    if (Math.abs(deltaX) >= Math.abs(deltaY)) {
        return deltaX > 0 ? 'right' : 'left';
    }

    return deltaY > 0 ? 'down' : 'up';
}

function getDisplacement(direction, deltaX, deltaY) {
    switch (direction) {
        case 'right':
            return deltaX;
        case 'left':
            return -deltaX;
        case 'down':
            return deltaY;
        case 'up':
            return -deltaY;
        default:
            return 0;
    }
}

function getDismissDirection(directions, deltaX, deltaY) {
    for (const direction of directions) {
        if (direction === 'right' && deltaX > SWIPE_THRESHOLD) {
            return 'right';
        }
        if (direction === 'left' && deltaX < -SWIPE_THRESHOLD) {
            return 'left';
        }
        if (direction === 'down' && deltaY > SWIPE_THRESHOLD) {
            return 'down';
        }
        if (direction === 'up' && deltaY < -SWIPE_THRESHOLD) {
            return 'up';
        }
    }

    return null;
}

function applyDirectionalDamping(entry, deltaX, deltaY) {
    let x = deltaX;
    let y = deltaY;

    if (!entry.swipeDirections.includes('left') && !entry.swipeDirections.includes('right')) {
        x = deltaX > 0
            ? deltaX ** OPPOSITE_DIRECTION_DAMPING_FACTOR
            : -(Math.abs(deltaX) ** OPPOSITE_DIRECTION_DAMPING_FACTOR);
    } else {
        if (!entry.swipeDirections.includes('right') && deltaX > 0) {
            x = deltaX ** OPPOSITE_DIRECTION_DAMPING_FACTOR;
        }
        if (!entry.swipeDirections.includes('left') && deltaX < 0) {
            x = -(Math.abs(deltaX) ** OPPOSITE_DIRECTION_DAMPING_FACTOR);
        }
    }

    if (!entry.swipeDirections.includes('up') && !entry.swipeDirections.includes('down')) {
        y = deltaY > 0
            ? deltaY ** OPPOSITE_DIRECTION_DAMPING_FACTOR
            : -(Math.abs(deltaY) ** OPPOSITE_DIRECTION_DAMPING_FACTOR);
    } else {
        if (!entry.swipeDirections.includes('down') && deltaY > 0) {
            y = deltaY ** OPPOSITE_DIRECTION_DAMPING_FACTOR;
        }
        if (!entry.swipeDirections.includes('up') && deltaY < 0) {
            y = -(Math.abs(deltaY) ** OPPOSITE_DIRECTION_DAMPING_FACTOR);
        }
    }

    return { x, y };
}

function getElementTransform(element) {
    const transform = getComputedStyle(element).transform;
    if (!transform || transform === 'none') {
        return { x: 0, y: 0, scale: 1 };
    }

    const matrix = new DOMMatrixReadOnly(transform);
    return {
        x: matrix.m41,
        y: matrix.m42,
        scale: matrix.a
    };
}

function hasCssTransition(element) {
    const style = getComputedStyle(element);
    const transitionDuration = parseDuration(style.transitionDuration);
    const animationDuration = parseDuration(style.animationDuration);
    return transitionDuration > 0 || animationDuration > 0;
}

function parseDuration(value) {
    return value
        .split(',')
        .map((part) => part.trim())
        .reduce((max, part) => {
            if (part.endsWith('ms')) {
                return Math.max(max, parseFloat(part));
            }
            if (part.endsWith('s')) {
                return Math.max(max, parseFloat(part) * 1000);
            }
            return max;
        }, 0);
}
