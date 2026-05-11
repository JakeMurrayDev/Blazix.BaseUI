/**
 * BlazorBaseUI Scroll Lock Module
 *
 * Unified scroll-lock functionality shared across all overlay components.
 * Based on the Base UI useScrollLock implementation.
 */

const STATE_KEY = Symbol.for('BlazorBaseUI.ScrollLock.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        scrollLocker: null
    };
}
const state = window[STATE_KEY];

// ============================================================================
// Browser Detection
// ============================================================================

const hasNavigator = typeof navigator !== 'undefined';

function getNavigatorData() {
    if (!hasNavigator) {
        return { platform: '', maxTouchPoints: -1 };
    }

    const uaData = navigator.userAgentData;

    if (uaData?.platform) {
        return {
            platform: uaData.platform,
            maxTouchPoints: navigator.maxTouchPoints
        };
    }

    return {
        platform: navigator.platform ?? '',
        maxTouchPoints: navigator.maxTouchPoints ?? -1
    };
}

const nav = getNavigatorData();

export const isWebKit =
    typeof CSS === 'undefined' || !CSS.supports
        ? false
        : CSS.supports('-webkit-backdrop-filter', 'none');

export const isIOS =
    // iPads can claim to be MacIntel
    nav.platform === 'MacIntel' && nav.maxTouchPoints > 1
        ? true
        : /iP(hone|ad|od)|iOS/.test(nav.platform);

// ============================================================================
// Owner Utilities
// ============================================================================

function ownerDocument(node) {
    return node?.ownerDocument || document;
}

function ownerWindow(node) {
    const doc = ownerDocument(node);
    return doc.defaultView || window;
}

// ============================================================================
// Helper Functions
// ============================================================================

function hasInsetScrollbars(referenceElement) {
    if (typeof document === 'undefined') {
        return false;
    }
    const doc = ownerDocument(referenceElement);
    const win = ownerWindow(doc);
    return win.innerWidth - doc.documentElement.clientWidth > 0;
}

function supportsStableScrollbarGutter(referenceElement) {
    const supported =
        typeof CSS !== 'undefined' && CSS.supports && CSS.supports('scrollbar-gutter', 'stable');

    if (!supported || typeof document === 'undefined') {
        return false;
    }

    const doc = ownerDocument(referenceElement);
    const html = doc.documentElement;

    const originalStyles = {
        scrollbarGutter: html.style.scrollbarGutter,
        overflowY: html.style.overflowY
    };

    html.style.scrollbarGutter = 'stable';
    html.style.overflowY = 'scroll';
    const before = html.offsetWidth;

    html.style.overflowY = 'hidden';
    const after = html.offsetWidth;

    Object.assign(html.style, originalStyles);
    return before === after;
}

function isOverflowElement(element) {
    if (!element) return false;
    const style = getComputedStyle(element);
    const overflow = style.overflow;
    const overflowX = style.overflowX;
    const overflowY = style.overflowY;
    return (
        overflow !== 'visible' ||
        overflowX !== 'visible' ||
        overflowY !== 'visible'
    );
}

// ============================================================================
// Scroll Lock Strategies
// ============================================================================

/**
 * Simple strategy for overlay scrollbars (iOS, macOS with overlay scrollbars)
 * Just sets overflow: hidden on the appropriate element
 */
function preventScrollOverlayScrollbars(referenceElement) {
    const doc = ownerDocument(referenceElement);
    const html = doc.documentElement;
    const body = doc.body;

    // If an `overflow` style is present on <html>, we need to lock it, because a lock on <body>
    // won't have any effect.
    // But if <body> has an `overflow` style (like `overflow-x: hidden`), we need to lock it
    // instead, as sticky elements shift otherwise.
    const elementToLock = isOverflowElement(html) ? html : body;
    const originalOverflow = elementToLock.style.overflow;
    elementToLock.style.overflow = 'hidden';

    return () => {
        elementToLock.style.overflow = originalOverflow;
    };
}

// Store for inset scrollbar strategy
let originalHtmlStyles = {};
let originalBodyStyles = {};
let originalHtmlScrollBehavior = '';

/**
 * Complex strategy for inset scrollbars (Windows, macOS with classic scrollbars)
 * Preserves scroll position, handles scrollbar width compensation
 */
function preventScrollInsetScrollbars(referenceElement) {
    const doc = ownerDocument(referenceElement);
    const html = doc.documentElement;
    const body = doc.body;
    const win = ownerWindow(html);

    let scrollTop = 0;
    let scrollLeft = 0;
    let updateGutterOnly = false;
    let resizeFrameId = null;

    // Pinch-zoom in Safari causes a shift. Just don't lock scroll if there's any pinch-zoom.
    if (isWebKit && (win.visualViewport?.scale ?? 1) !== 1) {
        return () => {};
    }

    function lockScroll() {
        /* DOM reads: */

        const htmlStyles = win.getComputedStyle(html);
        const bodyStyles = win.getComputedStyle(body);
        const htmlScrollbarGutterValue = htmlStyles.scrollbarGutter || '';
        const hasBothEdges = htmlScrollbarGutterValue.includes('both-edges');
        const scrollbarGutterValue = hasBothEdges ? 'stable both-edges' : 'stable';

        scrollTop = html.scrollTop;
        scrollLeft = html.scrollLeft;

        originalHtmlStyles = {
            scrollbarGutter: html.style.scrollbarGutter,
            overflowY: html.style.overflowY,
            overflowX: html.style.overflowX
        };
        originalHtmlScrollBehavior = html.style.scrollBehavior;

        originalBodyStyles = {
            position: body.style.position,
            height: body.style.height,
            width: body.style.width,
            boxSizing: body.style.boxSizing,
            overflowY: body.style.overflowY,
            overflowX: body.style.overflowX,
            scrollBehavior: body.style.scrollBehavior
        };

        const isScrollableY = html.scrollHeight > html.clientHeight;
        const isScrollableX = html.scrollWidth > html.clientWidth;
        const hasConstantOverflowY =
            htmlStyles.overflowY === 'scroll' || bodyStyles.overflowY === 'scroll';
        const hasConstantOverflowX =
            htmlStyles.overflowX === 'scroll' || bodyStyles.overflowX === 'scroll';

        // Values can be negative in Firefox
        const scrollbarWidth = Math.max(0, win.innerWidth - body.clientWidth);
        const scrollbarHeight = Math.max(0, win.innerHeight - body.clientHeight);

        // Avoid shift due to the default <body> margin.
        const marginY = parseFloat(bodyStyles.marginTop) + parseFloat(bodyStyles.marginBottom);
        const marginX = parseFloat(bodyStyles.marginLeft) + parseFloat(bodyStyles.marginRight);
        const elementToLock = isOverflowElement(html) ? html : body;

        updateGutterOnly = supportsStableScrollbarGutter(referenceElement);

        /*
         * DOM writes:
         * Do not read the DOM past this point!
         */

        if (updateGutterOnly) {
            html.style.scrollbarGutter = scrollbarGutterValue;
            elementToLock.style.overflowY = 'hidden';
            elementToLock.style.overflowX = 'hidden';
            return;
        }

        Object.assign(html.style, {
            scrollbarGutter: scrollbarGutterValue,
            overflowY: 'hidden',
            overflowX: 'hidden'
        });

        if (isScrollableY || hasConstantOverflowY) {
            html.style.overflowY = 'scroll';
        }
        if (isScrollableX || hasConstantOverflowX) {
            html.style.overflowX = 'scroll';
        }

        Object.assign(body.style, {
            position: 'relative',
            height:
                marginY || scrollbarHeight ? `calc(100dvh - ${marginY + scrollbarHeight}px)` : '100dvh',
            width: marginX || scrollbarWidth ? `calc(100vw - ${marginX + scrollbarWidth}px)` : '100vw',
            boxSizing: 'border-box',
            overflow: 'hidden',
            scrollBehavior: 'unset'
        });

        body.scrollTop = scrollTop;
        body.scrollLeft = scrollLeft;
        html.setAttribute('data-base-ui-scroll-locked', '');
        html.style.scrollBehavior = 'unset';
    }

    function cleanup() {
        Object.assign(html.style, originalHtmlStyles);
        Object.assign(body.style, originalBodyStyles);

        if (!updateGutterOnly) {
            html.scrollTop = scrollTop;
            html.scrollLeft = scrollLeft;
            html.removeAttribute('data-base-ui-scroll-locked');
            html.style.scrollBehavior = originalHtmlScrollBehavior;
        }
    }

    function handleResize() {
        cleanup();
        if (resizeFrameId) {
            cancelAnimationFrame(resizeFrameId);
        }
        resizeFrameId = requestAnimationFrame(lockScroll);
    }

    lockScroll();
    win.addEventListener('resize', handleResize);

    return () => {
        if (resizeFrameId) {
            cancelAnimationFrame(resizeFrameId);
        }
        cleanup();
        // Sometimes this cleanup can be run after test teardown
        // because it is called in a `setTimeout(fn, 0)`,
        // in which case `removeEventListener` wouldn't be available,
        // so we check for it to avoid test failures.
        if (typeof win.removeEventListener === 'function') {
            win.removeEventListener('resize', handleResize);
        }
    };
}

// ============================================================================
// ScrollLocker Class (Singleton)
// ============================================================================

class ScrollLocker {
    constructor() {
        this.lockCount = 0;
        this.restore = null;
        this.timeoutLock = null;
        this.timeoutUnlock = null;
    }

    acquire(referenceElement) {
        this.lockCount += 1;
        if (this.lockCount === 1 && this.restore === null) {
            // Matches base-ui's Timeout.start behavior: clear pending before scheduling
            if (this.timeoutLock !== null) {
                clearTimeout(this.timeoutLock);
                this.timeoutLock = null;
            }
            // Defer lock to next tick
            this.timeoutLock = setTimeout(() => {
                this.timeoutLock = null;
                this.lock(referenceElement);
            }, 0);
        }
        return () => this.release();
    }

    release() {
        if (this.lockCount === 0) {
            return;
        }
        this.lockCount -= 1;
        if (this.lockCount === 0 && this.restore) {
            // Matches base-ui's Timeout.start behavior: clear pending before scheduling
            if (this.timeoutUnlock !== null) {
                clearTimeout(this.timeoutUnlock);
                this.timeoutUnlock = null;
            }
            // Defer unlock to next tick
            this.timeoutUnlock = setTimeout(() => {
                this.timeoutUnlock = null;
                this.unlock();
            }, 0);
        }
    }

    unlock() {
        if (this.lockCount === 0 && this.restore) {
            this.restore();
            this.restore = null;
        }
    }

    lock(referenceElement) {
        if (this.lockCount === 0 || this.restore !== null) {
            return;
        }

        const doc = ownerDocument(referenceElement);
        const html = doc.documentElement;
        const htmlOverflowY = ownerWindow(html).getComputedStyle(html).overflowY;

        // If the site author already hid overflow on <html>, respect it and bail out.
        if (htmlOverflowY === 'hidden' || htmlOverflowY === 'clip') {
            this.restore = () => {};
            return;
        }

        const hasOverlayScrollbars = isIOS || !hasInsetScrollbars(referenceElement);

        // On iOS, scroll locking does not work if the navbar is collapsed. Due to numerous
        // side effects and bugs that arise on iOS, it must be researched extensively before
        // being enabled to ensure it doesn't cause the following issues:
        // - Textboxes must scroll into view when focused, nor cause a glitchy scroll animation.
        // - The navbar must not force itself into view and cause layout shift.
        // - Scroll containers must not flicker upon closing a popup when it has an exit animation.
        this.restore = hasOverlayScrollbars
            ? preventScrollOverlayScrollbars(referenceElement)
            : preventScrollInsetScrollbars(referenceElement);
    }
}

// Initialize singleton
if (!state.scrollLocker) {
    state.scrollLocker = new ScrollLocker();
}

// ============================================================================
// Public API
// ============================================================================

/**
 * Acquires a scroll lock. Call the returned function to release.
 * Uses reference counting so nested overlays work correctly.
 *
 * @param {Element|null} referenceElement - Element to use as a reference for lock calculations
 * @returns {Function} Release function to call when done
 */
export function acquireScrollLock(referenceElement = null) {
    return state.scrollLocker.acquire(referenceElement);
}

/**
 * Checks if scroll is currently locked.
 * @returns {boolean}
 */
export function isScrollLocked() {
    return state.scrollLocker.lockCount > 0;
}

/**
 * Gets the current lock count (for debugging/testing).
 * @returns {number}
 */
export function getLockCount() {
    return state.scrollLocker.lockCount;
}
