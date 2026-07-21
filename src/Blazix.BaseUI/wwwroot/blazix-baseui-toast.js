import {
    activeElement,
    contains,
    getTarget,
    initializePositioner as initializeFloatingPositioner,
    updatePositioner,
    disposePositioner as disposeFloatingPositioner
} from './blazix-baseui-floating.min.js';

const STATE_KEY = Symbol.for('Blazix.BaseUI.Toast.State');

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
const SWIPE_IGNORE_SELECTOR = '[data-base-ui-swipe-ignore], [data-blazix-base-ui-swipe-ignore], [data-swipe-ignore], button, a, input, textarea, [role="button"]';

function invokeDotNet(dotNetRef, method, ...args) {
    return dotNetRef.invokeMethodAsync(method, ...args).catch((error) => {
        if (!String(error).includes('disposed') && !String(error).includes('disconnected')) {
            console.error(`Blazix.BaseUI Toast interop failed in ${method}.`, error);
        }
    });
}

export function initializeViewport(viewportId, viewport, dotNetRef) {
    disposeViewport(viewportId);

    const entry = {
        viewport,
        dotNetRef,
        isEmpty: true,
        prevFocusElement: null,
        focusGuardRelatedTarget: null,
        touchActive: false,
        markedReadyForMouseLeave: false,
        cleanups: [],
        globalCleanups: []
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
        invokeDotNet(dotNetRef, 'OnGlobalFocusHotkey');
    };

    const onWindowBlur = (event) => {
        if (getTarget(event) === win) {
            invokeDotNet(dotNetRef, 'OnWindowBlur');
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

        setTimeout(() => invokeDotNet(dotNetRef, 'OnWindowFocus', shouldResume), 0);
    };

    const onDocumentPointerDown = (event) => {
        if (event.pointerType !== 'touch') {
            return;
        }

        if (contains(viewport, getTarget(event))) {
            return;
        }

        invokeDotNet(dotNetRef, 'OnDocumentTouchPointerDownOutside');
    };

    const onFocus = () => {
        invokeDotNet(dotNetRef, 'OnViewportFocus', matchesFocusVisible(activeElement(doc)));
    };

    const onBlur = (event) => {
        invokeDotNet(dotNetRef, 'OnViewportBlur', contains(viewport, event.relatedTarget));
    };

    const onViewportKeyDown = (event) => {
        if (event.key !== 'Tab' || getTarget(event) !== viewport) {
            return;
        }

        event.preventDefault();
        if (event.shiftKey) {
            entry.prevFocusElement?.focus?.({ preventScroll: true });
            return;
        }

        const firstToast = viewport.querySelector('[data-blazix-base-ui-toast-root]:not([data-ending-style]):not([data-limited])');
        firstToast?.focus?.({ preventScroll: true });
        if (!firstToast) {
            entry.prevFocusElement?.focus?.({ preventScroll: true });
        }
    };

    const onDocumentFocusIn = (event) => {
        if (getTarget(event)?.hasAttribute?.('data-blazix-base-ui-focus-guard')) {
            entry.focusGuardRelatedTarget = event.relatedTarget;
        }
    };

    const bindGlobalListeners = () => {
        if (entry.globalCleanups.length > 0) {
            return;
        }

        win.addEventListener('keydown', onKeyDown);
        win.addEventListener('blur', onWindowBlur, true);
        win.addEventListener('focus', onWindowFocus, true);
        doc.addEventListener('pointerdown', onDocumentPointerDown, true);
        entry.globalCleanups.push(() => win.removeEventListener('keydown', onKeyDown));
        entry.globalCleanups.push(() => win.removeEventListener('blur', onWindowBlur, true));
        entry.globalCleanups.push(() => win.removeEventListener('focus', onWindowFocus, true));
        entry.globalCleanups.push(() => doc.removeEventListener('pointerdown', onDocumentPointerDown, true));
    };

    const unbindGlobalListeners = () => {
        for (const cleanup of entry.globalCleanups.splice(0)) {
            cleanup();
        }
    };

    entry.bindGlobalListeners = bindGlobalListeners;
    entry.unbindGlobalListeners = unbindGlobalListeners;
    viewport.addEventListener('focus', onFocus);
    viewport.addEventListener('blur', onBlur);
    viewport.addEventListener('keydown', onViewportKeyDown);
    doc.addEventListener('focusin', onDocumentFocusIn, true);

    entry.cleanups.push(() => viewport.removeEventListener('focus', onFocus));
    entry.cleanups.push(() => viewport.removeEventListener('blur', onBlur));
    entry.cleanups.push(() => viewport.removeEventListener('keydown', onViewportKeyDown));
    entry.cleanups.push(() => doc.removeEventListener('focusin', onDocumentFocusIn, true));
    entry.cleanups.push(unbindGlobalListeners);

    state.viewports.set(viewportId, entry);
}

export function updateViewport(viewportId, isEmpty) {
    const entry = state.viewports.get(viewportId);
    if (entry) {
        entry.isEmpty = isEmpty;
        if (isEmpty) {
            entry.unbindGlobalListeners();
        } else {
            entry.bindGlobalListeners();
        }
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

    const firstToast = entry.viewport.querySelector('[data-blazix-base-ui-toast-root]:not([data-ending-style]):not([data-limited])');
    const HTMLElementCtor = entry.viewport.ownerDocument.defaultView?.HTMLElement;
    if (entry.focusGuardRelatedTarget === entry.viewport && HTMLElementCtor && firstToast instanceof HTMLElementCtor) {
        entry.focusGuardRelatedTarget = null;
        firstToast.focus({ preventScroll: true });
        return;
    }

    entry.focusGuardRelatedTarget = null;
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

    const toasts = Array.from(viewport.querySelectorAll('[data-blazix-base-ui-toast-root]'));
    const currentIndex = toasts.findIndex((toast) => toast.getAttribute('data-toast-id') === toastId);
    const scan = (from, step) => {
        for (let index = from; index >= 0 && index < toasts.length; index += step) {
            if (!toasts[index].hasAttribute('data-ending-style')) {
                return toasts[index];
            }
        }
        return null;
    };
    const next = scan(currentIndex + 1, 1) || scan(currentIndex - 1, -1);
    const HTMLElementCtor = doc.defaultView?.HTMLElement;
    if (HTMLElementCtor && next instanceof HTMLElementCtor) {
        next.focus({ preventScroll: true });
    } else {
        entry.prevFocusElement?.focus?.({ preventScroll: true });
    }
}

export function initializeContent(contentId, element) {
    disposeContent(contentId);

    const root = element.closest('[data-blazix-base-ui-toast-root]');
    const rootEntry = root ? findRootEntry(root) : null;
    if (!rootEntry) {
        return;
    }

    const recalculate = () => measureRoot(rootEntry);
    recalculate();

    const canObserve = typeof ResizeObserver === 'function' && typeof MutationObserver === 'function';
    const resizeObserver = canObserve ? new ResizeObserver(recalculate) : null;
    const mutationObserver = canObserve ? new MutationObserver(recalculate) : null;

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
        completionController: null,
        lastMeasuredHeight: null,
        cleanups: [],
        elementCleanups: []
    };

    bindRootElement(entry, element);
    state.roots.set(rootId, entry);
    measureRoot(entry);
}

export function updateRoot(rootId, element, ending, swipeEnabled, swipeDirections) {
    const entry = state.roots.get(rootId);
    if (!entry) {
        return;
    }

    if (entry.element !== element) {
        bindRootElement(entry, element);
        entry.lastMeasuredHeight = null;
        measureRoot(entry);
    }
    entry.swipeEnabled = swipeEnabled;
    entry.swipeDirections = swipeDirections || [];

    if (ending) {
        waitForRootAnimations(entry);
    } else {
        entry.completionController?.abort();
        entry.completionController = null;
    }
}

export function disposeRoot(rootId) {
    const entry = state.roots.get(rootId);
    if (!entry) {
        return;
    }

    entry.abortController?.abort();
    entry.completionController?.abort();
    unbindRootElement(entry);
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
    entry.abortController?.abort();
    entry.abortController = new AbortController();
    const doc = entry.element.ownerDocument;
    doc.addEventListener('pointerup', (endEvent) => handleSwipeEnd(entry, endEvent), {
        signal: entry.abortController.signal
    });
    doc.addEventListener('pointercancel', (endEvent) => handleSwipeEnd(entry, endEvent), {
        signal: entry.abortController.signal
    });
    invokeDotNet(entry.dotNetRef, 'OnSwipeStateChanged', true, null);
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
            invokeDotNet(entry.dotNetRef, 'OnSwipeStateChanged', true, candidate);
        }
    } else {
        const currentDisplacement = getDisplacement(entry.intendedDirection, cancelDeltaX, cancelDeltaY);
        if (currentDisplacement > SWIPE_THRESHOLD) {
            entry.cancelledSwipe = false;
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
    entry.abortController?.abort();
    entry.abortController = null;
    entry.element.releasePointerCapture?.(event.pointerId);
    entry.element.style.transition = '';
    entry.element.style.transform = '';
    entry.element.removeAttribute('data-swiping');

    const deltaX = entry.dragOffset.x - entry.initialTransform.x;
    const deltaY = entry.dragOffset.y - entry.initialTransform.y;
    const dismissDirection = getDismissDirection(entry.swipeDirections, deltaX, deltaY);

    if (event.type !== 'pointercancel' && !entry.cancelledSwipe && dismissDirection) {
        entry.element.setAttribute('data-swipe-direction', dismissDirection);
        invokeDotNet(entry.dotNetRef, 'OnSwipeEnded', true, dismissDirection);
    } else {
        entry.element.removeAttribute('data-swipe-direction');
        entry.element.style.setProperty('--toast-swipe-movement-x', '0px');
        entry.element.style.setProperty('--toast-swipe-movement-y', '0px');
        invokeDotNet(entry.dotNetRef, 'OnSwipeEnded', false, null);
    }
}

function measureRoot(entry) {
    const element = entry.element;
    const previousHeight = element.style.height;
    element.style.height = 'auto';
    const height = element.offsetHeight;
    element.style.height = previousHeight;
    if (entry.lastMeasuredHeight !== height) {
        entry.lastMeasuredHeight = height;
        invokeDotNet(entry.dotNetRef, 'OnMeasuredHeight', height);
    }
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
        const HTMLElementCtor = element?.ownerDocument?.defaultView?.HTMLElement;
        return Boolean(HTMLElementCtor && element instanceof HTMLElementCtor && element.matches(':focus-visible'));
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

function bindRootElement(entry, element) {
    unbindRootElement(entry);
    entry.element = element;

    const onTouchMove = (event) => {
        if (entry.swipeEnabled && entry.activePointerId !== null && contains(entry.element, getTarget(event))) {
            event.preventDefault();
        }
    };
    const onPointerDown = (event) => handlePointerDown(entry, event);
    const onPointerMove = (event) => handlePointerMove(entry, event);
    const onPointerUp = (event) => handleSwipeEnd(entry, event);
    const onPointerCancel = (event) => handleSwipeEnd(entry, event);

    element.addEventListener('touchmove', onTouchMove, { passive: false });
    element.addEventListener('pointerdown', onPointerDown);
    element.addEventListener('pointermove', onPointerMove);
    element.addEventListener('pointerup', onPointerUp);
    element.addEventListener('pointercancel', onPointerCancel);
    entry.elementCleanups.push(() => element.removeEventListener('touchmove', onTouchMove));
    entry.elementCleanups.push(() => element.removeEventListener('pointerdown', onPointerDown));
    entry.elementCleanups.push(() => element.removeEventListener('pointermove', onPointerMove));
    entry.elementCleanups.push(() => element.removeEventListener('pointerup', onPointerUp));
    entry.elementCleanups.push(() => element.removeEventListener('pointercancel', onPointerCancel));
}

function unbindRootElement(entry) {
    for (const cleanup of entry.elementCleanups.splice(0)) {
        cleanup();
    }
}

function waitForRootAnimations(entry) {
    entry.completionController?.abort();
    const controller = new AbortController();
    entry.completionController = controller;
    const win = entry.element.ownerDocument.defaultView || window;

    win.requestAnimationFrame(async () => {
        if (controller.signal.aborted || !entry.element.hasAttribute('data-ending-style')) {
            return;
        }

        const animations = typeof entry.element.getAnimations === 'function'
            ? entry.element.getAnimations()
            : [];
        await Promise.allSettled(animations.map((animation) => animation.finished));

        if (!controller.signal.aborted && entry.element.hasAttribute('data-ending-style')) {
            invokeDotNet(entry.dotNetRef, 'OnTransitionComplete');
        }
    });
}
