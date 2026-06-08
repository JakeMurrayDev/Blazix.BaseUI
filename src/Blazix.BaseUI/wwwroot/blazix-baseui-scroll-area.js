const STATE_KEY = Symbol.for('Blazix.BaseUI.ScrollArea.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        roots: new Map(),
        pendingRoots: new Map(),
        overflowVarsRegistered: false,
        scrollbarStyleInjected: false
    };
}

const state = window[STATE_KEY];
state.pendingRoots ??= new Map();

const SCROLL_TIMEOUT = 500;
const MIN_THUMB_SIZE = 16;
const SCROLL_EDGE_TOLERANCE_PX = 1;
const DISABLE_SCROLLBAR_CLASS_NAME = 'base-ui-disable-scrollbar';
const OVERFLOW_CSS_VARS = [
    '--scroll-area-overflow-x-start',
    '--scroll-area-overflow-x-end',
    '--scroll-area-overflow-y-start',
    '--scroll-area-overflow-y-end'
];

export function initializeRoot(rootElement, rootId, dotNetRef, direction, overflowEdgeThreshold) {
    if (!rootElement || !rootId) {
        return;
    }

    disposeRoot(rootId);
    injectDisableScrollbarStyle(rootElement.ownerDocument);

    const root = {
        rootElement,
        rootId,
        dotNetRef,
        direction: normalizeDirection(direction),
        overflowEdgeThreshold: normalizeOverflowEdgeThreshold(overflowEdgeThreshold),
        viewportElement: null,
        contentElement: null,
        scrollbarYElement: null,
        scrollbarXElement: null,
        thumbYElement: null,
        thumbXElement: null,
        cornerElement: null,
        scrollbarYKeepMounted: false,
        scrollbarXKeepMounted: false,
        hiddenState: { x: true, y: true, corner: true },
        overflowEdges: { xStart: false, xEnd: false, yStart: false, yEnd: false },
        cornerSize: { width: 0, height: 0 },
        thumbSize: { width: 0, height: 0 },
        hasMeasuredScrollbar: false,
        hovering: false,
        scrollingX: false,
        scrollingY: false,
        touchModality: false,
        thumbDragging: false,
        startY: 0,
        startX: 0,
        startScrollTop: 0,
        startScrollLeft: 0,
        currentOrientation: 'vertical',
        scrollPosition: { x: 0, y: 0 },
        programmaticScroll: true,
        lastMeasuredViewportMetrics: [NaN, NaN, NaN, NaN],
        lastNotifiedKey: '',
        scrollXTimeout: 0,
        scrollYTimeout: 0,
        scrollEndTimeout: 0,
        waitForAnimationsTimeout: 0,
        rootCleanup: null,
        viewportCleanup: null,
        contentCleanup: null,
        scrollbarYCleanup: null,
        scrollbarXCleanup: null,
        thumbYCleanup: null,
        thumbXCleanup: null
    };

    state.roots.set(rootId, root);
    attachRootListeners(root);
    replayPendingRegistrations(root);
    syncDomState(root);
    notifyDotNet(root);
}

export function updateRoot(rootId, direction, overflowEdgeThreshold) {
    const root = state.roots.get(rootId);
    if (!root) {
        return;
    }

    root.direction = normalizeDirection(direction);
    root.overflowEdgeThreshold = normalizeOverflowEdgeThreshold(overflowEdgeThreshold);
    queueCompute(root);
}

export function disposeRoot(rootId) {
    const root = state.roots.get(rootId);
    if (!root) {
        return;
    }

    cleanupRoot(root);
    state.roots.delete(rootId);
}

export function registerViewport(rootId, viewportElement, direction) {
    const root = state.roots.get(rootId);
    if (!root || !viewportElement) {
        if (rootId && viewportElement) {
            getPendingRoot(rootId).viewport = { viewportElement, direction };
        }

        return;
    }

    root.direction = normalizeDirection(direction || root.direction);

    if (root.viewportElement === viewportElement && root.viewportCleanup) {
        queueCompute(root);
        return;
    }

    if (root.viewportCleanup) {
        root.viewportCleanup();
        root.viewportCleanup = null;
    }

    root.viewportElement = viewportElement;
    removeCSSVariableInheritance();

    const handleUserInteraction = () => {
        root.programmaticScroll = false;
    };

    const handleScroll = () => {
        computeThumbPosition(root);

        if (!root.programmaticScroll) {
            handleScrollPosition(root, {
                x: viewportElement.scrollLeft,
                y: viewportElement.scrollTop
            });
        }

        clearTimeout(root.scrollEndTimeout);
        root.scrollEndTimeout = setTimeout(() => {
            root.programmaticScroll = true;
        }, 100);
    };

    viewportElement.addEventListener('scroll', handleScroll, { passive: true });
    viewportElement.addEventListener('wheel', handleUserInteraction, { passive: true });
    viewportElement.addEventListener('touchmove', handleUserInteraction, { passive: true });
    viewportElement.addEventListener('pointermove', handleUserInteraction, { passive: true });
    viewportElement.addEventListener('pointerenter', handleUserInteraction, { passive: true });
    viewportElement.addEventListener('keydown', handleUserInteraction);

    let resizeObserver = null;
    if (typeof ResizeObserver !== 'undefined') {
        let hasInitialized = false;
        resizeObserver = new ResizeObserver(() => {
            if (!hasInitialized) {
                hasInitialized = true;
                const last = root.lastMeasuredViewportMetrics;
                if (
                    last[0] === viewportElement.clientHeight &&
                    last[1] === viewportElement.scrollHeight &&
                    last[2] === viewportElement.clientWidth &&
                    last[3] === viewportElement.scrollWidth
                ) {
                    return;
                }
            }

            computeThumbPosition(root);
        });
        resizeObserver.observe(viewportElement);
    }

    clearTimeout(root.waitForAnimationsTimeout);
    root.waitForAnimationsTimeout = setTimeout(() => {
        const animations = viewportElement.getAnimations
            ? viewportElement.getAnimations({ subtree: true })
            : [];

        if (animations.length === 0) {
            return;
        }

        Promise.allSettled(animations.map((animation) => animation.finished))
            .then(() => computeThumbPosition(root))
            .catch(() => {});
    }, 0);

    root.viewportCleanup = () => {
        viewportElement.removeEventListener('scroll', handleScroll);
        viewportElement.removeEventListener('wheel', handleUserInteraction);
        viewportElement.removeEventListener('touchmove', handleUserInteraction);
        viewportElement.removeEventListener('pointermove', handleUserInteraction);
        viewportElement.removeEventListener('pointerenter', handleUserInteraction);
        viewportElement.removeEventListener('keydown', handleUserInteraction);
        resizeObserver?.disconnect();
        clearTimeout(root.scrollEndTimeout);
        clearTimeout(root.waitForAnimationsTimeout);
    };

    if (viewportElement.matches?.(':hover')) {
        setHovering(root, true);
    }

    queueCompute(root);
}

export function unregisterViewport(rootId) {
    const root = state.roots.get(rootId);
    if (!root) {
        const pending = state.pendingRoots.get(rootId);
        if (pending) {
            delete pending.viewport;
        }

        return;
    }

    root.viewportCleanup?.();
    root.viewportCleanup = null;
    root.viewportElement = null;
}

export function registerContent(rootId, contentElement) {
    const root = state.roots.get(rootId);
    if (!root || !contentElement) {
        if (rootId && contentElement) {
            getPendingRoot(rootId).contentElement = contentElement;
        }

        return;
    }

    if (root.contentElement === contentElement && root.contentCleanup) {
        queueCompute(root);
        return;
    }

    root.contentCleanup?.();
    root.contentElement = contentElement;

    let resizeObserver = null;
    if (typeof ResizeObserver !== 'undefined') {
        let hasInitialized = false;
        resizeObserver = new ResizeObserver(() => {
            if (!hasInitialized) {
                hasInitialized = true;
                return;
            }

            computeThumbPosition(root);
        });
        resizeObserver.observe(contentElement);
    }

    root.contentCleanup = () => {
        resizeObserver?.disconnect();
    };

    queueCompute(root);
}

export function unregisterContent(rootId) {
    const root = state.roots.get(rootId);
    if (!root) {
        const pending = state.pendingRoots.get(rootId);
        if (pending) {
            delete pending.contentElement;
        }

        return;
    }

    root.contentCleanup?.();
    root.contentCleanup = null;
    root.contentElement = null;
}

export function registerScrollbar(rootId, scrollbarElement, orientation, keepMounted) {
    const root = state.roots.get(rootId);
    if (!root || !scrollbarElement) {
        if (rootId && scrollbarElement) {
            const pending = getPendingRoot(rootId);
            const normalizedOrientation = normalizeOrientation(orientation);
            if (normalizedOrientation === 'vertical') {
                pending.scrollbarY = { scrollbarElement, orientation: normalizedOrientation, keepMounted };
            } else {
                pending.scrollbarX = { scrollbarElement, orientation: normalizedOrientation, keepMounted };
            }
        }

        return;
    }

    const normalizedOrientation = normalizeOrientation(orientation);
    const cleanupKey = normalizedOrientation === 'vertical' ? 'scrollbarYCleanup' : 'scrollbarXCleanup';
    const elementKey = normalizedOrientation === 'vertical' ? 'scrollbarYElement' : 'scrollbarXElement';
    const keepMountedKey = normalizedOrientation === 'vertical' ? 'scrollbarYKeepMounted' : 'scrollbarXKeepMounted';

    if (root[elementKey] === scrollbarElement && root[cleanupKey]) {
        root[keepMountedKey] = !!keepMounted;
        queueCompute(root);
        return;
    }

    root[cleanupKey]?.();
    root[elementKey] = scrollbarElement;
    root[keepMountedKey] = !!keepMounted;

    const onWheel = (event) => {
        handleScrollbarWheel(root, normalizedOrientation, event);
    };

    const onPointerDown = (event) => {
        handleScrollbarPointerDown(root, normalizedOrientation, event);
    };

    const onPointerUp = (event) => {
        handlePointerUp(root, event);
    };

    scrollbarElement.addEventListener('wheel', onWheel, { passive: false });
    scrollbarElement.addEventListener('pointerdown', onPointerDown);
    scrollbarElement.addEventListener('pointerup', onPointerUp);
    scrollbarElement.addEventListener('pointercancel', onPointerUp);

    root[cleanupKey] = () => {
        scrollbarElement.removeEventListener('wheel', onWheel);
        scrollbarElement.removeEventListener('pointerdown', onPointerDown);
        scrollbarElement.removeEventListener('pointerup', onPointerUp);
        scrollbarElement.removeEventListener('pointercancel', onPointerUp);
    };

    syncDomState(root);
    queueCompute(root);
}

export function unregisterScrollbar(rootId, orientation) {
    const root = state.roots.get(rootId);
    const normalizedOrientation = normalizeOrientation(orientation);
    if (!root) {
        const pending = state.pendingRoots.get(rootId);
        if (pending) {
            if (normalizedOrientation === 'vertical') {
                delete pending.scrollbarY;
            } else {
                delete pending.scrollbarX;
            }
        }

        return;
    }

    if (normalizedOrientation === 'vertical') {
        root.scrollbarYCleanup?.();
        root.scrollbarYCleanup = null;
        root.scrollbarYElement = null;
        root.scrollbarYKeepMounted = false;
    } else {
        root.scrollbarXCleanup?.();
        root.scrollbarXCleanup = null;
        root.scrollbarXElement = null;
        root.scrollbarXKeepMounted = false;
    }
}

export function registerThumb(rootId, thumbElement, orientation) {
    const root = state.roots.get(rootId);
    if (!root || !thumbElement) {
        if (rootId && thumbElement) {
            const pending = getPendingRoot(rootId);
            const normalizedOrientation = normalizeOrientation(orientation);
            if (normalizedOrientation === 'vertical') {
                pending.thumbY = { thumbElement, orientation: normalizedOrientation };
            } else {
                pending.thumbX = { thumbElement, orientation: normalizedOrientation };
            }
        }

        return;
    }

    const normalizedOrientation = normalizeOrientation(orientation);
    const cleanupKey = normalizedOrientation === 'vertical' ? 'thumbYCleanup' : 'thumbXCleanup';
    const elementKey = normalizedOrientation === 'vertical' ? 'thumbYElement' : 'thumbXElement';

    if (root[elementKey] === thumbElement && root[cleanupKey]) {
        queueCompute(root);
        return;
    }

    root[cleanupKey]?.();
    root[elementKey] = thumbElement;

    const onPointerDown = (event) => {
        handlePointerDown(root, normalizedOrientation, event);
    };

    const onPointerMove = (event) => {
        handlePointerMove(root, event);
    };

    const onPointerUp = (event) => {
        if (normalizedOrientation === 'vertical') {
            setScrolling(root, 'y', false);
        } else {
            setScrolling(root, 'x', false);
        }

        handlePointerUp(root, event);
    };

    thumbElement.addEventListener('pointerdown', onPointerDown);
    thumbElement.addEventListener('pointermove', onPointerMove);
    thumbElement.addEventListener('pointerup', onPointerUp);
    thumbElement.addEventListener('pointercancel', onPointerUp);

    root[cleanupKey] = () => {
        thumbElement.removeEventListener('pointerdown', onPointerDown);
        thumbElement.removeEventListener('pointermove', onPointerMove);
        thumbElement.removeEventListener('pointerup', onPointerUp);
        thumbElement.removeEventListener('pointercancel', onPointerUp);
    };

    syncDomState(root);
    queueCompute(root);
}

export function unregisterThumb(rootId, orientation) {
    const root = state.roots.get(rootId);
    const normalizedOrientation = normalizeOrientation(orientation);
    if (!root) {
        const pending = state.pendingRoots.get(rootId);
        if (pending) {
            if (normalizedOrientation === 'vertical') {
                delete pending.thumbY;
            } else {
                delete pending.thumbX;
            }
        }

        return;
    }

    if (normalizedOrientation === 'vertical') {
        root.thumbYCleanup?.();
        root.thumbYCleanup = null;
        root.thumbYElement = null;
    } else {
        root.thumbXCleanup?.();
        root.thumbXCleanup = null;
        root.thumbXElement = null;
    }
}

export function registerCorner(rootId, cornerElement) {
    const root = state.roots.get(rootId);
    if (!root || !cornerElement) {
        if (rootId && cornerElement) {
            getPendingRoot(rootId).cornerElement = cornerElement;
        }

        return;
    }

    root.cornerElement = cornerElement;
    syncDomState(root);
    queueCompute(root);
}

export function unregisterCorner(rootId) {
    const root = state.roots.get(rootId);
    if (!root) {
        const pending = state.pendingRoots.get(rootId);
        if (pending) {
            delete pending.cornerElement;
        }

        return;
    }

    root.cornerElement = null;
}

function attachRootListeners(root) {
    const rootElement = root.rootElement;

    const handlePointerDownEvent = (event) => {
        root.touchModality = event.pointerType === 'touch';
    };

    const handlePointerEnterOrMoveEvent = (event) => {
        root.touchModality = event.pointerType === 'touch';

        if (event.pointerType !== 'touch') {
            setHovering(root, rootElement.contains(event.target));
        }
    };

    const handlePointerLeaveEvent = () => {
        setHovering(root, false);
    };

    rootElement.addEventListener('pointerenter', handlePointerEnterOrMoveEvent);
    rootElement.addEventListener('pointermove', handlePointerEnterOrMoveEvent);
    rootElement.addEventListener('pointerdown', handlePointerDownEvent);
    rootElement.addEventListener('pointerleave', handlePointerLeaveEvent);

    root.rootCleanup = () => {
        rootElement.removeEventListener('pointerenter', handlePointerEnterOrMoveEvent);
        rootElement.removeEventListener('pointermove', handlePointerEnterOrMoveEvent);
        rootElement.removeEventListener('pointerdown', handlePointerDownEvent);
        rootElement.removeEventListener('pointerleave', handlePointerLeaveEvent);
    };
}

function cleanupRoot(root) {
    root.rootCleanup?.();
    root.viewportCleanup?.();
    root.contentCleanup?.();
    root.scrollbarYCleanup?.();
    root.scrollbarXCleanup?.();
    root.thumbYCleanup?.();
    root.thumbXCleanup?.();
    clearTimeout(root.scrollXTimeout);
    clearTimeout(root.scrollYTimeout);
    clearTimeout(root.scrollEndTimeout);
    clearTimeout(root.waitForAnimationsTimeout);
}

function getPendingRoot(rootId) {
    let pending = state.pendingRoots.get(rootId);
    if (!pending) {
        pending = {};
        state.pendingRoots.set(rootId, pending);
    }

    return pending;
}

function replayPendingRegistrations(root) {
    const pending = state.pendingRoots.get(root.rootId);
    if (!pending) {
        return;
    }

    state.pendingRoots.delete(root.rootId);

    if (pending.viewport) {
        registerViewport(root.rootId, pending.viewport.viewportElement, pending.viewport.direction);
    }

    if (pending.contentElement) {
        registerContent(root.rootId, pending.contentElement);
    }

    if (pending.scrollbarY) {
        registerScrollbar(
            root.rootId,
            pending.scrollbarY.scrollbarElement,
            pending.scrollbarY.orientation,
            pending.scrollbarY.keepMounted);
    }

    if (pending.scrollbarX) {
        registerScrollbar(
            root.rootId,
            pending.scrollbarX.scrollbarElement,
            pending.scrollbarX.orientation,
            pending.scrollbarX.keepMounted);
    }

    if (pending.thumbY) {
        registerThumb(root.rootId, pending.thumbY.thumbElement, pending.thumbY.orientation);
    }

    if (pending.thumbX) {
        registerThumb(root.rootId, pending.thumbX.thumbElement, pending.thumbX.orientation);
    }

    if (pending.cornerElement) {
        registerCorner(root.rootId, pending.cornerElement);
    }
}

function computeThumbPosition(root) {
    const viewportEl = root.viewportElement;
    const scrollbarYEl = root.scrollbarYElement;
    const scrollbarXEl = root.scrollbarXElement;
    const thumbYEl = root.thumbYElement;
    const thumbXEl = root.thumbXElement;
    const cornerEl = root.cornerElement;

    if (!viewportEl) {
        return;
    }

    const scrollableContentHeight = viewportEl.scrollHeight;
    const scrollableContentWidth = viewportEl.scrollWidth;
    const viewportHeight = viewportEl.clientHeight;
    const viewportWidth = viewportEl.clientWidth;
    const scrollTop = viewportEl.scrollTop;
    const scrollLeft = viewportEl.scrollLeft;
    const lastMeasuredViewportMetrics = root.lastMeasuredViewportMetrics;
    const isFirstMeasurement = Number.isNaN(lastMeasuredViewportMetrics[0]);

    lastMeasuredViewportMetrics[0] = viewportHeight;
    lastMeasuredViewportMetrics[1] = scrollableContentHeight;
    lastMeasuredViewportMetrics[2] = viewportWidth;
    lastMeasuredViewportMetrics[3] = scrollableContentWidth;

    if (isFirstMeasurement) {
        root.hasMeasuredScrollbar = true;
    }

    if (scrollableContentHeight === 0 || scrollableContentWidth === 0) {
        syncDomState(root);
        notifyDotNet(root);
        return;
    }

    const nextHiddenState = getHiddenState(viewportEl);
    const scrollbarYHidden = nextHiddenState.y;
    const scrollbarXHidden = nextHiddenState.x;
    const ratioX = viewportWidth / scrollableContentWidth;
    const ratioY = viewportHeight / scrollableContentHeight;
    const maxScrollLeft = Math.max(0, scrollableContentWidth - viewportWidth);
    const maxScrollTop = Math.max(0, scrollableContentHeight - viewportHeight);

    let scrollLeftFromStart = 0;
    let scrollLeftFromEnd = 0;
    if (!scrollbarXHidden) {
        let rawScrollLeftFromStart = 0;
        if (root.direction === 'rtl') {
            rawScrollLeftFromStart = clamp(-scrollLeft, 0, maxScrollLeft);
        } else {
            rawScrollLeftFromStart = clamp(scrollLeft, 0, maxScrollLeft);
        }
        scrollLeftFromStart = normalizeScrollOffset(rawScrollLeftFromStart, maxScrollLeft);
        scrollLeftFromEnd = maxScrollLeft - scrollLeftFromStart;
    }

    const rawScrollTopFromStart = !scrollbarYHidden ? clamp(scrollTop, 0, maxScrollTop) : 0;
    const scrollTopFromStart = !scrollbarYHidden
        ? normalizeScrollOffset(rawScrollTopFromStart, maxScrollTop)
        : 0;
    const scrollTopFromEnd = !scrollbarYHidden ? maxScrollTop - scrollTopFromStart : 0;
    const nextWidth = scrollbarXHidden ? 0 : viewportWidth;
    const nextHeight = scrollbarYHidden ? 0 : viewportHeight;

    let nextCornerWidth = 0;
    let nextCornerHeight = 0;
    if (!scrollbarXHidden && !scrollbarYHidden) {
        nextCornerWidth = scrollbarYEl?.offsetWidth || 0;
        nextCornerHeight = scrollbarXEl?.offsetHeight || 0;
    }

    const cornerNotYetSized = root.cornerSize.width === 0 && root.cornerSize.height === 0;
    const cornerWidthOffset = cornerNotYetSized ? nextCornerWidth : 0;
    const cornerHeightOffset = cornerNotYetSized ? nextCornerHeight : 0;

    const scrollbarXOffset = getOffset(scrollbarXEl, 'padding', 'x');
    const scrollbarYOffset = getOffset(scrollbarYEl, 'padding', 'y');
    const thumbXOffset = getOffset(thumbXEl, 'margin', 'x');
    const thumbYOffset = getOffset(thumbYEl, 'margin', 'y');

    const idealNextWidth = nextWidth - scrollbarXOffset - thumbXOffset;
    const idealNextHeight = nextHeight - scrollbarYOffset - thumbYOffset;

    const maxNextWidth = scrollbarXEl
        ? Math.min(scrollbarXEl.offsetWidth - cornerWidthOffset, idealNextWidth)
        : idealNextWidth;
    const maxNextHeight = scrollbarYEl
        ? Math.min(scrollbarYEl.offsetHeight - cornerHeightOffset, idealNextHeight)
        : idealNextHeight;

    const clampedNextWidth = Math.max(MIN_THUMB_SIZE, maxNextWidth * ratioX);
    const clampedNextHeight = Math.max(MIN_THUMB_SIZE, maxNextHeight * ratioY);

    root.thumbSize = {
        width: clampedNextWidth,
        height: clampedNextHeight
    };

    if (scrollbarYEl && thumbYEl) {
        const maxThumbOffsetY =
            scrollbarYEl.offsetHeight - clampedNextHeight - scrollbarYOffset - thumbYOffset;
        const scrollRangeY = scrollableContentHeight - viewportHeight;
        const scrollRatioY = scrollRangeY === 0 ? 0 : scrollTop / scrollRangeY;
        const thumbOffsetY = Math.min(maxThumbOffsetY, Math.max(0, scrollRatioY * maxThumbOffsetY));

        thumbYEl.style.transform = `translate3d(0,${thumbOffsetY}px,0)`;
    }

    if (scrollbarXEl && thumbXEl) {
        const maxThumbOffsetX =
            scrollbarXEl.offsetWidth - clampedNextWidth - scrollbarXOffset - thumbXOffset;
        const scrollRangeX = scrollableContentWidth - viewportWidth;
        const scrollRatioX = scrollRangeX === 0 ? 0 : scrollLeft / scrollRangeX;
        const thumbOffsetX = root.direction === 'rtl'
            ? clamp(scrollRatioX * maxThumbOffsetX, -maxThumbOffsetX, 0)
            : clamp(scrollRatioX * maxThumbOffsetX, 0, maxThumbOffsetX);

        thumbXEl.style.transform = `translate3d(${thumbOffsetX}px,0,0)`;
    }

    const overflowMetricsPx = [
        ['--scroll-area-overflow-x-start', scrollLeftFromStart],
        ['--scroll-area-overflow-x-end', scrollLeftFromEnd],
        ['--scroll-area-overflow-y-start', scrollTopFromStart],
        ['--scroll-area-overflow-y-end', scrollTopFromEnd]
    ];

    for (const [cssVar, value] of overflowMetricsPx) {
        viewportEl.style.setProperty(cssVar, `${value}px`);
    }

    if (cornerEl) {
        if (scrollbarXHidden || scrollbarYHidden) {
            root.cornerSize = { width: 0, height: 0 };
        } else {
            root.cornerSize = { width: nextCornerWidth, height: nextCornerHeight };
        }
    }

    root.hiddenState = nextHiddenState;
    root.overflowEdges = {
        xStart: !scrollbarXHidden && scrollLeftFromStart > root.overflowEdgeThreshold.xStart,
        xEnd: !scrollbarXHidden && scrollLeftFromEnd > root.overflowEdgeThreshold.xEnd,
        yStart: !scrollbarYHidden && scrollTopFromStart > root.overflowEdgeThreshold.yStart,
        yEnd: !scrollbarYHidden && scrollTopFromEnd > root.overflowEdgeThreshold.yEnd
    };

    syncDomState(root);
    notifyDotNet(root);
}

function handleScrollPosition(root, scrollPosition) {
    const offsetX = scrollPosition.x - root.scrollPosition.x;
    const offsetY = scrollPosition.y - root.scrollPosition.y;

    root.scrollPosition = scrollPosition;

    if (offsetY !== 0) {
        setScrolling(root, 'y', true);
        clearTimeout(root.scrollYTimeout);
        root.scrollYTimeout = setTimeout(() => setScrolling(root, 'y', false), SCROLL_TIMEOUT);
    }

    if (offsetX !== 0) {
        setScrolling(root, 'x', true);
        clearTimeout(root.scrollXTimeout);
        root.scrollXTimeout = setTimeout(() => setScrolling(root, 'x', false), SCROLL_TIMEOUT);
    }
}

function handlePointerDown(root, orientation, event) {
    if (event.button !== 0) {
        return;
    }

    root.thumbDragging = true;
    root.startY = event.clientY;
    root.startX = event.clientX;
    root.currentOrientation = orientation;

    if (root.viewportElement) {
        root.startScrollTop = root.viewportElement.scrollTop;
        root.startScrollLeft = root.viewportElement.scrollLeft;
    }

    const thumb = orientation === 'vertical' ? root.thumbYElement : root.thumbXElement;
    if (thumb?.setPointerCapture && event.pointerId != null) {
        try {
            thumb.setPointerCapture(event.pointerId);
        } catch {
            // Pointer capture can fail if the pointer was released first.
        }
    }
}

function handlePointerMove(root, event) {
    if (!root.thumbDragging) {
        return;
    }

    const deltaY = event.clientY - root.startY;
    const deltaX = event.clientX - root.startX;
    const viewportEl = root.viewportElement;

    if (!viewportEl) {
        return;
    }

    const scrollableContentHeight = viewportEl.scrollHeight;
    const viewportHeight = viewportEl.clientHeight;
    const scrollableContentWidth = viewportEl.scrollWidth;
    const viewportWidth = viewportEl.clientWidth;

    if (root.thumbYElement && root.scrollbarYElement && root.currentOrientation === 'vertical') {
        const scrollbarYOffset = getOffset(root.scrollbarYElement, 'padding', 'y');
        const thumbYOffset = getOffset(root.thumbYElement, 'margin', 'y');
        const thumbHeight = root.thumbYElement.offsetHeight;
        const maxThumbOffsetY =
            root.scrollbarYElement.offsetHeight - thumbHeight - scrollbarYOffset - thumbYOffset;
        const scrollRatioY = getThumbScrollRatio(deltaY, maxThumbOffsetY);
        if (scrollRatioY == null) {
            event.preventDefault();
            return;
        }

        viewportEl.scrollTop =
            root.startScrollTop + scrollRatioY * (scrollableContentHeight - viewportHeight);
        event.preventDefault();
        setScrolling(root, 'y', true);
        clearTimeout(root.scrollYTimeout);
        root.scrollYTimeout = setTimeout(() => setScrolling(root, 'y', false), SCROLL_TIMEOUT);
    }

    if (root.thumbXElement && root.scrollbarXElement && root.currentOrientation === 'horizontal') {
        const scrollbarXOffset = getOffset(root.scrollbarXElement, 'padding', 'x');
        const thumbXOffset = getOffset(root.thumbXElement, 'margin', 'x');
        const thumbWidth = root.thumbXElement.offsetWidth;
        const maxThumbOffsetX =
            root.scrollbarXElement.offsetWidth - thumbWidth - scrollbarXOffset - thumbXOffset;
        const scrollRatioX = getThumbScrollRatio(deltaX, maxThumbOffsetX);
        if (scrollRatioX == null) {
            event.preventDefault();
            return;
        }

        viewportEl.scrollLeft =
            root.startScrollLeft + scrollRatioX * (scrollableContentWidth - viewportWidth);
        event.preventDefault();
        setScrolling(root, 'x', true);
        clearTimeout(root.scrollXTimeout);
        root.scrollXTimeout = setTimeout(() => setScrolling(root, 'x', false), SCROLL_TIMEOUT);
    }
}

function handlePointerUp(root, event) {
    root.thumbDragging = false;

    const thumb = root.currentOrientation === 'vertical' ? root.thumbYElement : root.thumbXElement;
    if (thumb?.releasePointerCapture && event.pointerId != null) {
        try {
            thumb.releasePointerCapture(event.pointerId);
        } catch {
            // Pointer capture may already be released.
        }
    }
}

function handleScrollbarPointerDown(root, orientation, event) {
    if (event.button !== 0) {
        return;
    }

    const target = getTarget(event);
    const thumb = orientation === 'vertical' ? root.thumbYElement : root.thumbXElement;
    if (thumb && target && contains(thumb, target)) {
        return;
    }

    const viewportEl = root.viewportElement;
    if (!viewportEl) {
        return;
    }

    if (root.thumbYElement && root.scrollbarYElement && orientation === 'vertical') {
        const thumbYOffset = getOffset(root.thumbYElement, 'margin', 'y');
        const scrollbarYOffset = getOffset(root.scrollbarYElement, 'padding', 'y');
        const thumbHeight = root.thumbYElement.offsetHeight;
        const trackRectY = root.scrollbarYElement.getBoundingClientRect();
        const clickY =
            event.clientY - trackRectY.top - thumbHeight / 2 - scrollbarYOffset + thumbYOffset / 2;
        const maxThumbOffsetY =
            root.scrollbarYElement.offsetHeight - thumbHeight - scrollbarYOffset - thumbYOffset;
        const scrollRatioY = getThumbScrollRatio(clickY, maxThumbOffsetY);

        if (scrollRatioY != null) {
            viewportEl.scrollTop = scrollRatioY * (viewportEl.scrollHeight - viewportEl.clientHeight);
            handleScrollPosition(root, { x: viewportEl.scrollLeft, y: viewportEl.scrollTop });
        }
    }

    if (root.thumbXElement && root.scrollbarXElement && orientation === 'horizontal') {
        const thumbXOffset = getOffset(root.thumbXElement, 'margin', 'x');
        const scrollbarXOffset = getOffset(root.scrollbarXElement, 'padding', 'x');
        const thumbWidth = root.thumbXElement.offsetWidth;
        const trackRectX = root.scrollbarXElement.getBoundingClientRect();
        const clickX =
            event.clientX - trackRectX.left - thumbWidth / 2 - scrollbarXOffset + thumbXOffset / 2;
        const maxThumbOffsetX =
            root.scrollbarXElement.offsetWidth - thumbWidth - scrollbarXOffset - thumbXOffset;
        const scrollRatioX = getThumbScrollRatio(clickX, maxThumbOffsetX);

        if (scrollRatioX != null) {
            const scrollRange = viewportEl.scrollWidth - viewportEl.clientWidth;

            let newScrollLeft;
            if (root.direction === 'rtl') {
                newScrollLeft = (1 - scrollRatioX) * scrollRange;
                if (viewportEl.scrollLeft <= 0) {
                    newScrollLeft = -newScrollLeft;
                }
            } else {
                newScrollLeft = scrollRatioX * scrollRange;
            }

            viewportEl.scrollLeft = newScrollLeft;
            handleScrollPosition(root, { x: viewportEl.scrollLeft, y: viewportEl.scrollTop });
        }
    }

    handlePointerDown(root, orientation, event);
}

function handleScrollbarWheel(root, orientation, event) {
    const viewportEl = root.viewportElement;
    if (!viewportEl || event.ctrlKey) {
        return;
    }

    event.preventDefault();

    const horizontal = orientation === 'horizontal';
    const scrollProperty = horizontal ? 'scrollLeft' : 'scrollTop';
    const delta = horizontal ? event.deltaX : event.deltaY;
    const maxScroll = horizontal
        ? viewportEl.scrollWidth - viewportEl.clientWidth
        : viewportEl.scrollHeight - viewportEl.clientHeight;
    const minScroll = horizontal && root.direction === 'rtl' ? -maxScroll : 0;
    const maxScrollValue = horizontal && root.direction === 'rtl' ? 0 : maxScroll;
    const scrollValue = viewportEl[scrollProperty];

    if ((scrollValue <= minScroll && delta < 0) || (scrollValue >= maxScrollValue && delta > 0)) {
        return;
    }

    viewportEl[scrollProperty] = Math.min(
        maxScrollValue,
        Math.max(minScroll, scrollValue + delta)
    );
    handleScrollPosition(root, { x: viewportEl.scrollLeft, y: viewportEl.scrollTop });
    computeThumbPosition(root);
}

function setHovering(root, hovering) {
    if (root.hovering === hovering) {
        return;
    }

    root.hovering = hovering;
    syncDomState(root);
    notifyDotNet(root);
}

function setScrolling(root, axis, value) {
    const key = axis === 'x' ? 'scrollingX' : 'scrollingY';
    if (root[key] === value) {
        return;
    }

    root[key] = value;
    syncDomState(root);
    notifyDotNet(root);
}

function syncDomState(root) {
    applyRootStateAttributes(root.rootElement, root, root.scrollingX || root.scrollingY);
    applyRootStateAttributes(root.viewportElement, root, root.scrollingX || root.scrollingY);
    applyRootStateAttributes(root.contentElement, root, root.scrollingX || root.scrollingY);
    applyScrollbarStateAttributes(root, 'vertical');
    applyScrollbarStateAttributes(root, 'horizontal');
    applyThumbAttributes(root.thumbYElement, 'vertical');
    applyThumbAttributes(root.thumbXElement, 'horizontal');
    applyCssVars(root);
}

function applyRootStateAttributes(element, root, scrolling) {
    if (!element) {
        return;
    }

    toggleAttribute(element, 'data-scrolling', scrolling);
    toggleAttribute(element, 'data-has-overflow-x', !root.hiddenState.x);
    toggleAttribute(element, 'data-has-overflow-y', !root.hiddenState.y);
    toggleAttribute(element, 'data-overflow-x-start', root.overflowEdges.xStart);
    toggleAttribute(element, 'data-overflow-x-end', root.overflowEdges.xEnd);
    toggleAttribute(element, 'data-overflow-y-start', root.overflowEdges.yStart);
    toggleAttribute(element, 'data-overflow-y-end', root.overflowEdges.yEnd);
}

function applyScrollbarStateAttributes(root, orientation) {
    const element = orientation === 'vertical' ? root.scrollbarYElement : root.scrollbarXElement;
    if (!element) {
        return;
    }

    element.setAttribute('data-orientation', orientation);
    toggleAttribute(element, 'data-hovering', root.hovering);
    applyRootStateAttributes(
        element,
        root,
        orientation === 'vertical' ? root.scrollingY : root.scrollingX
    );
}

function applyThumbAttributes(element, orientation) {
    if (!element) {
        return;
    }

    element.setAttribute('data-orientation', orientation);
    element.style.visibility = stateVisibility(element.style.visibility, true);
}

function applyCssVars(root) {
    if (root.rootElement) {
        root.rootElement.style.setProperty('--scroll-area-corner-height', `${root.cornerSize.height}px`);
        root.rootElement.style.setProperty('--scroll-area-corner-width', `${root.cornerSize.width}px`);
    }

    if (root.scrollbarYElement) {
        root.scrollbarYElement.style.setProperty('--scroll-area-thumb-height', `${root.thumbSize.height}px`);
        root.scrollbarYElement.style.visibility =
            !root.hasMeasuredScrollbar && !root.scrollbarYKeepMounted ? 'hidden' : '';
    }

    if (root.scrollbarXElement) {
        root.scrollbarXElement.style.setProperty('--scroll-area-thumb-width', `${root.thumbSize.width}px`);
        root.scrollbarXElement.style.visibility =
            !root.hasMeasuredScrollbar && !root.scrollbarXKeepMounted ? 'hidden' : '';
    }

    if (root.thumbYElement) {
        root.thumbYElement.style.visibility = root.hasMeasuredScrollbar ? '' : 'hidden';
    }

    if (root.thumbXElement) {
        root.thumbXElement.style.visibility = root.hasMeasuredScrollbar ? '' : 'hidden';
    }

    if (root.cornerElement) {
        root.cornerElement.style.width = `${root.cornerSize.width}px`;
        root.cornerElement.style.height = `${root.cornerSize.height}px`;
    }
}

function notifyDotNet(root) {
    const key = [
        root.scrollingX,
        root.scrollingY,
        root.hovering,
        root.hasMeasuredScrollbar,
        root.hiddenState.x,
        root.hiddenState.y,
        root.hiddenState.corner,
        root.overflowEdges.xStart,
        root.overflowEdges.xEnd,
        root.overflowEdges.yStart,
        root.overflowEdges.yEnd,
        root.cornerSize.width,
        root.cornerSize.height,
        root.thumbSize.width,
        root.thumbSize.height
    ].join('|');

    if (root.lastNotifiedKey === key) {
        return;
    }

    root.lastNotifiedKey = key;

    root.dotNetRef?.invokeMethodAsync(
        'OnScrollAreaStateChanged',
        root.scrollingX,
        root.scrollingY,
        root.hovering,
        root.hasMeasuredScrollbar,
        root.hiddenState.x,
        root.hiddenState.y,
        root.hiddenState.corner,
        root.overflowEdges.xStart,
        root.overflowEdges.xEnd,
        root.overflowEdges.yStart,
        root.overflowEdges.yEnd,
        root.cornerSize.width,
        root.cornerSize.height,
        root.thumbSize.width,
        root.thumbSize.height
    ).catch(() => {});
}

function queueCompute(root) {
    queueMicrotask(() => computeThumbPosition(root));
    requestAnimationFrame(() => computeThumbPosition(root));
}

function getHiddenState(viewport) {
    const y = viewport.clientHeight >= viewport.scrollHeight;
    const x = viewport.clientWidth >= viewport.scrollWidth;

    return {
        y,
        x,
        corner: y || x
    };
}

function getOffset(element, prop, axis) {
    if (!element) {
        return 0;
    }

    const styles = getComputedStyle(element);
    const propAxis = axis === 'x' ? 'Inline' : 'Block';

    if (axis === 'x' && prop === 'margin') {
        return parseCssFloat(styles[`${prop}InlineStart`]) * 2;
    }

    return (
        parseCssFloat(styles[`${prop}${propAxis}Start`]) +
        parseCssFloat(styles[`${prop}${propAxis}End`])
    );
}

function normalizeScrollOffset(value, max) {
    if (max <= 0) {
        return 0;
    }

    const clamped = clamp(value, 0, max);
    const startDistance = clamped;
    const endDistance = max - clamped;
    const withinStartTolerance = startDistance <= SCROLL_EDGE_TOLERANCE_PX;
    const withinEndTolerance = endDistance <= SCROLL_EDGE_TOLERANCE_PX;

    if (withinStartTolerance && withinEndTolerance) {
        return startDistance <= endDistance ? 0 : max;
    }

    if (withinStartTolerance) {
        return 0;
    }

    if (withinEndTolerance) {
        return max;
    }

    return clamped;
}

function removeCSSVariableInheritance() {
    if (state.overflowVarsRegistered || isWebKit()) {
        return;
    }

    if (typeof CSS !== 'undefined' && 'registerProperty' in CSS) {
        for (const name of OVERFLOW_CSS_VARS) {
            try {
                CSS.registerProperty({
                    name,
                    syntax: '<length>',
                    inherits: false,
                    initialValue: '0px'
                });
            } catch {
                // Already registered in this document.
            }
        }
    }

    state.overflowVarsRegistered = true;
}

function injectDisableScrollbarStyle(doc) {
    if (!doc || state.scrollbarStyleInjected) {
        return;
    }

    if (doc.head.querySelector('style[data-blazix-base-ui-scroll-area-disable-scrollbar]')) {
        state.scrollbarStyleInjected = true;
        return;
    }

    const styleEl = doc.createElement('style');
    styleEl.setAttribute('data-blazix-base-ui-scroll-area-disable-scrollbar', '');
    styleEl.textContent =
        `.${DISABLE_SCROLLBAR_CLASS_NAME}{scrollbar-width:none}.` +
        `${DISABLE_SCROLLBAR_CLASS_NAME}::-webkit-scrollbar{display:none}`;
    doc.head.appendChild(styleEl);
    state.scrollbarStyleInjected = true;
}

function normalizeOverflowEdgeThreshold(threshold) {
    if (typeof threshold === 'number') {
        const value = Math.max(0, threshold);
        return { xStart: value, xEnd: value, yStart: value, yEnd: value };
    }

    return {
        xStart: Math.max(0, threshold?.xStart ?? threshold?.XStart ?? 0),
        xEnd: Math.max(0, threshold?.xEnd ?? threshold?.XEnd ?? 0),
        yStart: Math.max(0, threshold?.yStart ?? threshold?.YStart ?? 0),
        yEnd: Math.max(0, threshold?.yEnd ?? threshold?.YEnd ?? 0)
    };
}

function normalizeDirection(direction) {
    return direction === 'rtl' ? 'rtl' : 'ltr';
}

function normalizeOrientation(orientation) {
    return orientation === 'horizontal' ? 'horizontal' : 'vertical';
}

function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
}

function getThumbScrollRatio(offset, maxThumbOffset) {
    if (!Number.isFinite(maxThumbOffset) || maxThumbOffset <= 0) {
        return null;
    }

    return offset / maxThumbOffset;
}

function parseCssFloat(value) {
    const parsed = parseFloat(value);
    return Number.isFinite(parsed) ? parsed : 0;
}

function toggleAttribute(element, attribute, enabled) {
    if (enabled) {
        element.setAttribute(attribute, '');
    } else {
        element.removeAttribute(attribute);
    }
}

function contains(parent, child) {
    if (!parent || !child) {
        return false;
    }

    return parent === child || parent.contains(child);
}

function getTarget(event) {
    const path = event.composedPath?.();
    return path && path.length > 0 ? path[0] : event.target;
}

function isWebKit() {
    if (typeof navigator === 'undefined') {
        return false;
    }

    return /\bAppleWebKit\b/.test(navigator.userAgent) && !/\bChrome\b/.test(navigator.userAgent);
}

function stateVisibility(currentVisibility, allowVisible) {
    if (!allowVisible) {
        return 'hidden';
    }

    return currentVisibility === 'hidden' ? '' : currentVisibility;
}
