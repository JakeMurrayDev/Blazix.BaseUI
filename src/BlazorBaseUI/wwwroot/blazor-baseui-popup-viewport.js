/**
 * Popup viewport module.
 * Provides auto-resize (ResizeObserver + CSS variable sizing) and viewport morphing
 * (DOM cloning, directional transitions) for overlay components.
 * Based on Base UI usePopupAutoResize + usePopupViewport.
 */

const STATE_KEY = Symbol.for('BlazorBaseUI.PopupViewport.State');
if (!window[STATE_KEY]) {
    window[STATE_KEY] = new WeakMap();
}
const state = window[STATE_KEY];

const DIRECTION_TOLERANCE = 5;

// ============================================================================
// CSS Dimension Helpers
// ============================================================================

function setPopupCssSize(el, size) {
    if (!el) return;
    if (size === 'auto') {
        el.style.setProperty('--popup-width', 'auto');
        el.style.setProperty('--popup-height', 'auto');
    } else {
        el.style.setProperty('--popup-width', `${size.width}px`);
        el.style.setProperty('--popup-height', `${size.height}px`);
    }
}

function setPositionerCssSize(el, size) {
    if (!el) return;
    if (size === 'max-content') {
        el.style.setProperty('--positioner-width', 'max-content');
        el.style.setProperty('--positioner-height', 'max-content');
    } else {
        el.style.setProperty('--positioner-width', `${size.width}px`);
        el.style.setProperty('--positioner-height', `${size.height}px`);
    }
}

function getCssDimensions(el) {
    if (!el) return { width: 0, height: 0 };
    const style = getComputedStyle(el);
    return {
        width: Math.ceil(parseFloat(style.width) || 0),
        height: Math.ceil(parseFloat(style.height) || 0)
    };
}

// ============================================================================
// Anchoring Styles
// ============================================================================

function applyAnchoringStyles(el, side, direction) {
    if (!el) return () => {};
    const isRtl = direction === 'rtl';
    const originals = {
        position: el.style.position,
        top: el.style.top,
        bottom: el.style.bottom,
        left: el.style.left,
        right: el.style.right
    };

    if (side === 'top') {
        el.style.position = 'absolute';
        el.style.bottom = '0';
        el.style.top = 'auto';
    }

    const isPhysicalLeft = (side === 'left' && !isRtl) || (side === 'right' && isRtl);
    if (isPhysicalLeft) {
        el.style.position = 'absolute';
        el.style.right = '0';
        el.style.left = 'auto';
    }

    return () => {
        Object.assign(el.style, originals);
    };
}

// ============================================================================
// Direction Calculation
// ============================================================================

function getActivationDirection(prevElement, newElement) {
    if (!prevElement || !newElement) return '';

    const prevRect = prevElement.getBoundingClientRect();
    const newRect = newElement.getBoundingClientRect();

    const dx = (newRect.left + newRect.width / 2) - (prevRect.left + prevRect.width / 2);
    const dy = (newRect.top + newRect.height / 2) - (prevRect.top + prevRect.height / 2);

    const horizontal = Math.abs(dx) < DIRECTION_TOLERANCE ? '' : (dx > 0 ? 'right' : 'left');
    const vertical = Math.abs(dy) < DIRECTION_TOLERANCE ? '' : (dy > 0 ? 'down' : 'up');

    return `${horizontal} ${vertical}`.trim();
}

// ============================================================================
// Auto-Resize
// ============================================================================

function setupAutoResize(entry) {
    cleanupAutoResize(entry);

    const { popupElement, positionerElement, side, direction } = entry;
    if (!popupElement || !positionerElement || typeof ResizeObserver === 'undefined') return;

    entry.restoreAnchoringStyles = applyAnchoringStyles(popupElement, side, direction);

    const observer = new ResizeObserver((entries) => {
        const resizeEntry = entries[0];
        if (resizeEntry) {
            entry.liveDimensions = {
                width: Math.ceil(resizeEntry.borderBoxSize[0]?.inlineSize || resizeEntry.contentRect.width),
                height: Math.ceil(resizeEntry.borderBoxSize[0]?.blockSize || resizeEntry.contentRect.height)
            };
        }
    });
    observer.observe(popupElement);
    entry.resizeObserver = observer;

    setPopupCssSize(popupElement, 'auto');
    setPositionerCssSize(positionerElement, 'max-content');
    const dims = getCssDimensions(popupElement);
    entry.committedDimensions = dims;
    setPositionerCssSize(positionerElement, dims);
}

function cleanupAutoResize(entry) {
    if (entry.resizeObserver) {
        entry.resizeObserver.disconnect();
        entry.resizeObserver = null;
    }
    if (entry.restoreAnchoringStyles) {
        entry.restoreAnchoringStyles();
        entry.restoreAnchoringStyles = null;
    }
    entry.committedDimensions = null;
    entry.liveDimensions = null;
}

function remeasureAutoResize(entry) {
    const { popupElement, positionerElement } = entry;
    if (!popupElement || !positionerElement) return;

    const previousDimensions = entry.committedDimensions || entry.liveDimensions;

    setPopupCssSize(popupElement, 'auto');
    setPositionerCssSize(positionerElement, 'max-content');
    const newDimensions = getCssDimensions(popupElement);
    entry.committedDimensions = newDimensions;

    if (!previousDimensions) {
        setPositionerCssSize(positionerElement, newDimensions);
        return;
    }

    setPopupCssSize(popupElement, previousDimensions);
    setPositionerCssSize(positionerElement, newDimensions);

    requestAnimationFrame(() => {
        setPopupCssSize(popupElement, newDimensions);

        const animations = popupElement.getAnimations?.() || [];
        if (animations.length > 0) {
            Promise.all(animations.map(a => a.finished.catch(() => {}))).then(() => {
                setPopupCssSize(popupElement, 'auto');
            });
        } else {
            setPopupCssSize(popupElement, 'auto');
        }
    });
}

// ============================================================================
// Viewport Morphing
// ============================================================================

function performViewportTransition(entry, previousTriggerElement, newTriggerElement) {
    const { viewportElement, dotNetRef } = entry;
    if (!viewportElement || !dotNetRef) return;

    const parent = viewportElement.parentNode;
    if (!parent) return;

    const clone = viewportElement.cloneNode(true);
    clone.removeAttribute('data-current');
    clone.setAttribute('data-previous', '');
    clone.setAttribute('inert', '');

    const width = viewportElement.offsetWidth;
    const height = viewportElement.offsetHeight;
    clone.style.setProperty('--popup-width', `${width}px`);
    clone.style.setProperty('--popup-height', `${height}px`);
    clone.style.position = 'absolute';

    const directionStr = getActivationDirection(previousTriggerElement, newTriggerElement);

    clone.setAttribute('data-ending-style', '');
    viewportElement.setAttribute('data-starting-style', '');

    parent.insertBefore(clone, viewportElement);

    dotNetRef.invokeMethodAsync('OnViewportTransitionStart', directionStr).catch(() => {});

    requestAnimationFrame(() => {
        requestAnimationFrame(() => {
            viewportElement.removeAttribute('data-starting-style');

            let ended = false;
            const onEnd = (event) => {
                if (event && event.target !== clone) return;
                if (ended) return;
                ended = true;
                clone.removeEventListener('transitionend', onEnd);
                clone.removeEventListener('animationend', onEnd);
                clearTimeout(fallbackId);
                clone.remove();
                if (entry.dotNetRef) {
                    entry.dotNetRef.invokeMethodAsync('OnViewportTransitionEnd').catch(() => {});
                }
            };

            clone.addEventListener('transitionend', onEnd);
            clone.addEventListener('animationend', onEnd);

            const fallbackId = setTimeout(onEnd, 500);
        });
    });

    remeasureAutoResize(entry);
}

// ============================================================================
// Public API
// ============================================================================

export function initialize(viewportElement, options) {
    dispose(viewportElement);

    const entry = {
        viewportElement,
        dotNetRef: options.dotNetRef || null,
        popupElement: options.popupElement || null,
        positionerElement: options.positionerElement || null,
        side: options.side || 'bottom',
        direction: options.direction || 'ltr',
        cssVars: options.cssVars || { popupWidth: '--popup-width', popupHeight: '--popup-height' },
        committedDimensions: null,
        liveDimensions: null,
        resizeObserver: null,
        restoreAnchoringStyles: null
    };

    setupAutoResize(entry);
    state.set(viewportElement, entry);
}

export function onTriggerChange(viewportElement, previousTriggerElement, newTriggerElement) {
    const entry = state.get(viewportElement);
    if (!entry) return;
    performViewportTransition(entry, previousTriggerElement, newTriggerElement);
}

export function setDotNetRef(viewportElement, dotNetRef) {
    const entry = state.get(viewportElement);
    if (entry) {
        entry.dotNetRef = dotNetRef;
    }
}

export function contentChanged(viewportElement) {
    const entry = state.get(viewportElement);
    if (entry) {
        remeasureAutoResize(entry);
    }
}

export function dispose(viewportElement) {
    if (!viewportElement) return;
    const entry = state.get(viewportElement);
    if (!entry) return;

    cleanupAutoResize(entry);

    const parent = viewportElement.parentNode;
    if (parent) {
        const clones = parent.querySelectorAll('[data-previous]');
        clones.forEach(clone => clone.remove());
    }

    state.delete(viewportElement);
}
