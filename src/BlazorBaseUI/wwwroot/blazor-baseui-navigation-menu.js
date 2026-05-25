/**
 * BlazorBaseUI Navigation Menu Component
 *
 * Handles hover delays, outside click dismiss, escape key,
 * transition detection, and positioning via shared floating module.
 */

import {
    initializePositioner as initializeFloatingPositioner,
    updatePositioner as updateFloatingPositioner,
    disposePositioner as disposeFloatingPositioner,
    cleanupTransitionState,
    startSimpleTransition
} from './blazor-baseui-floating.min.js';

const STATE_KEY = Symbol.for('BlazorBaseUI.NavigationMenu.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        roots: new Map(),
        positioners: new Map(),
        globalListenersInitialized: false
    };
}
const state = window[STATE_KEY];

function initGlobalListeners() {
    if (state.globalListenersInitialized) return;

    document.addEventListener('keydown', handleGlobalKeyDown, { capture: true });
    document.addEventListener('mousedown', handleGlobalMouseDown);
    document.addEventListener('focusout', handleGlobalFocusOut, { capture: true });
    state.globalListenersInitialized = true;
}

function handleGlobalKeyDown(e) {
    // Find the open navigation menu root
    let openRoot = null;
    for (const [id, rootState] of state.roots) {
        if (rootState.isOpen && rootState.dotNetRef) {
            openRoot = rootState;
            break;
        }
    }

    if (!openRoot) return;

    if (e.key === 'Escape') {
        e.preventDefault();
        e.stopPropagation();
        openRoot.closeReason = 'escape-key';
        openRoot.dotNetRef.invokeMethodAsync('OnEscapeKey').catch(() => { });
    }
}

function handleGlobalMouseDown(e) {
    for (const [id, rootState] of state.roots) {
        if (!rootState.isOpen || !rootState.dotNetRef) continue;

        if (!isInsideNavigationMenu(rootState, e.target) && !isNavigationMenuTrigger(e.target)) {
            rootState.closeReason = 'outside-press';
            rootState.dotNetRef.invokeMethodAsync('OnOutsidePress').catch(() => { });
        }
    }
}

function handleGlobalFocusOut(e) {
    const target = e.target;
    const relatedTarget = e.relatedTarget || target?.ownerDocument?.activeElement || null;

    for (const [, rootState] of state.roots) {
        if (!rootState.isOpen || !rootState.dotNetRef) continue;
        if (!isInsideNavigationMenu(rootState, target)) continue;
        if (isInsideNavigationMenu(rootState, relatedTarget) || isInsideAnyNavigationMenu(relatedTarget)) continue;

        rootState.closeReason = 'focus-out';
        rootState.dotNetRef.invokeMethodAsync('OnFocusOut').catch(() => { });
    }
}

function isNavigationMenuTrigger(target) {
    // lint-ignore:RULE-05 React parity selector emitted by NavigationMenuTrigger.
    return target instanceof Element && target.closest('[data-base-ui-navigation-menu-trigger]') != null;
}

function isInsideAnyNavigationMenu(target) {
    for (const [, rootState] of state.roots) {
        if (isInsideNavigationMenu(rootState, target)) {
            return true;
        }
    }

    return false;
}

function isInsideNavigationMenu(rootState, target) {
    if (!(target instanceof Node)) {
        return false;
    }

    if (rootState.rootElement?.contains(target)) {
        return true;
    }

    for (const [, triggerEl] of rootState.triggerElements) {
        if (triggerEl?.contains(target)) {
            return true;
        }
    }

    if (rootState.popupElement?.contains(target)) {
        return true;
    }

    if (rootState.viewportElement?.contains(target)) {
        return true;
    }

    if (rootState.viewportTargetElement?.contains(target)) {
        return true;
    }

    if (rootState.contentElements) {
        for (const [, contentEl] of rootState.contentElements) {
            if (contentEl?.contains(target)) {
                return true;
            }
        }
    }

    return false;
}

// --- Root Management ---

export function initializeRoot(rootId, dotNetRef, orientation, delay, closeDelay, isNested) {
    initGlobalListeners();

    const rootState = {
        rootId,
        dotNetRef,
        orientation,
        delay: delay ?? 200,
        closeDelay: closeDelay ?? 300,
        isOpen: false,
        value: null,
        isNested: !!isNested,
        rootElement: null,
        activeTriggerElement: null,
        closeReason: null,
        openedByHover: false,
        pointerType: '',
        stickIfOpenUntil: 0,
        triggerElements: new Map(),
        popupElement: null,
        viewportElement: null,
        viewportTargetElement: null,
        positionerElement: null,
        currentContentElement: null,
        resizeObserver: null,
        mutationObserver: null,
        autoSizeAbortController: null,
        resizeInstantTimer: null,
        resizeListener: null,
        openTimer: null,
        closeTimer: null,
        pendingOpen: false,
        transitionCleanup: null,
        fallbackTimeoutId: null
    };

    state.roots.set(rootId, rootState);
}

export function disposeRoot(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        clearTimeout(rootState.openTimer);
        clearTimeout(rootState.closeTimer);
        clearTimeout(rootState.resizeInstantTimer);
        clearSafePolygon(rootState);
        cleanupTransitionState(rootState);

        // Remove hover listeners from all triggers
        for (const [itemValue, triggerEl] of rootState.triggerElements) {
            if (triggerEl) {
                removeHoverListeners(rootState, itemValue, triggerEl);
            }
        }

        // Remove hover listeners from all content elements
        if (rootState.contentElements) {
            for (const [, contentEl] of rootState.contentElements) {
                if (contentEl) {
                    removeContentHoverListeners(contentEl);
                }
            }
        }

        // Remove hover listeners from popup element
        if (rootState.popupElement) {
            removePopupHoverListeners(rootState.popupElement);
        }

        // Remove hover listeners from viewport element
        if (rootState.viewportElement) {
            removeViewportHoverListeners(rootState.viewportElement);
        }

        disconnectSizeObservers(rootState);
        removePositionerResizeListener(rootState);

        state.roots.delete(rootId);
    }
}

export function setRootValue(rootId, value) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    const wasOpen = rootState.isOpen;
    rootState.isOpen = value != null;
    rootState.value = value;
    rootState.pendingOpen = rootState.isOpen;

    if (!rootState.isOpen) {
        clearSafePolygon(rootState);
        syncContentVisibility(rootState);
        setSharedFixedSize(rootState);
        maybeReturnFocus(rootState, wasOpen);
    } else {
        revealPositioningSurface(rootState);
        syncContentVisibility(rootState);
        syncCurrentContentElement(rootState);
        syncPopupAutoSize(rootState);
    }

    if (!rootState.isOpen) {
        rootState.closeReason = null;
        rootState.openedByHover = false;
        rootState.pointerType = '';
    }

    if (wasOpen !== rootState.isOpen) {
        startSimpleTransition(rootState, rootState.isOpen).catch(() => { });
    }
}

// --- Trigger Element Management ---

export function setRootElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.rootElement = element;
}

export function setTriggerElement(rootId, itemValue, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    // Remove old listeners if replacing
    const oldElement = rootState.triggerElements.get(itemValue);
    if (oldElement) {
        removeHoverListeners(rootState, itemValue, oldElement);
    }

    rootState.triggerElements.set(itemValue, element);
    addHoverListeners(rootState, itemValue, element);
}

export function disposeTriggerElement(rootId, itemValue) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    const element = rootState.triggerElements.get(itemValue);
    if (element) {
        removeHoverListeners(rootState, itemValue, element);
    }
    rootState.triggerElements.delete(itemValue);
}

function addHoverListeners(rootState, itemValue, element) {
    const onPointerEnter = (event) => {
        rootState.pointerType = event.pointerType || '';
    };
    const onPointerDown = (event) => {
        rootState.pointerType = event.pointerType || '';
    };
    const onEnter = () => {
        if (rootState.pointerType === 'touch') {
            return;
        }

        clearTimeout(rootState.closeTimer);
        rootState.closeTimer = null;
        clearSafePolygon(rootState);

        rootState.openTimer = setTimeout(() => {
            const prev = rootState.activeTriggerElement;
            if (prev && prev !== element) {
                rootState.prevTriggerElement = prev;
                rootState.activationDirection = computeRectDirection(prev, element, rootState.orientation);
            } else {
                rootState.prevTriggerElement = null;
                rootState.activationDirection = 'none';
            }
            rootState.value = itemValue;
            rootState.activeTriggerElement = element;
            rootState.openedByHover = true;
            rootState.stickIfOpenUntil = Date.now() + 500;
            rootState.pendingOpen = true;
            revealPositioningSurface(rootState);
            syncContentVisibility(rootState);
            syncCurrentContentElement(rootState);
            syncPopupAutoSize(rootState);
            rootState.dotNetRef
                .invokeMethodAsync('OnHoverOpen', itemValue, rootState.activationDirection)
                .catch(() => { });
        }, rootState.delay);
    };

    const onLeave = (event) => {
        clearTimeout(rootState.openTimer);
        rootState.openTimer = null;

        if (tryStartSafePolygon(rootState, element, event)) {
            return;
        }

        scheduleHoverClose(rootState);
    };

    const onClickCapture = (event) => {
        clearTimeout(rootState.openTimer);
        rootState.openTimer = null;
        rootState.activeTriggerElement = element;

        if (rootState.openedByHover && rootState.isOpen && rootState.value === itemValue && Date.now() < rootState.stickIfOpenUntil) {
            rootState.openedByHover = false;
            event.preventDefault();
            event.stopImmediatePropagation();
            return;
        }
        rootState.openedByHover = false;
    };
    const onKeyDown = (event) => {
        if (rootState.isNested) {
            return;
        }

        const openHorizontal = rootState.orientation === 'horizontal' && event.key === 'ArrowDown';
        const openVertical = rootState.orientation === 'vertical' && event.key === 'ArrowRight';

        if (!openHorizontal && !openVertical) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        rootState.value = itemValue;
        rootState.activeTriggerElement = element;
        rootState.closeReason = 'list-navigation';
        rootState.pendingOpen = true;
        revealPositioningSurface(rootState);
        syncContentVisibility(rootState);
        syncCurrentContentElement(rootState);
        syncPopupAutoSize(rootState);
        rootState.dotNetRef.invokeMethodAsync('OnKeyboardOpen', itemValue).catch(() => { });
    };

    element._navMenuPointerEnter = onPointerEnter;
    element._navMenuPointerDown = onPointerDown;
    element._navMenuEnter = onEnter;
    element._navMenuLeave = onLeave;
    element._navMenuClickCapture = onClickCapture;
    element._navMenuKeyDown = onKeyDown;
    element.addEventListener('pointerenter', onPointerEnter);
    element.addEventListener('pointerdown', onPointerDown);
    element.addEventListener('mouseenter', onEnter);
    element.addEventListener('mouseleave', onLeave);
    element.addEventListener('click', onClickCapture, true);
    element.addEventListener('keydown', onKeyDown);
}

function scheduleHoverClose(rootState) {
    clearTimeout(rootState.closeTimer);
    rootState.closeTimer = setTimeout(() => {
        rootState.closeTimer = null;
        rootState.closeReason = 'trigger-hover';
        rootState.dotNetRef.invokeMethodAsync('OnHoverClose').catch(() => { });
    }, rootState.closeDelay);
}

function computeRectDirection(prevElement, nextElement, orientation) {
    if (!prevElement || !nextElement) return 'none';
    const prev = prevElement.getBoundingClientRect();
    const next = nextElement.getBoundingClientRect();
    if (orientation === 'vertical') {
        return next.top > prev.top ? 'down' : 'up';
    }
    return next.left > prev.left ? 'right' : 'left';
}

// --- Safe Polygon (cone-of-tolerance for diagonal trigger->content path) ---

function tryStartSafePolygon(rootState, triggerElement, event) {
    if (!rootState.isOpen) return false;

    const targetEl = rootState.viewportElement || rootState.popupElement;
    if (!targetEl) return false;

    const targetRect = targetEl.getBoundingClientRect();
    const triggerRect = triggerElement.getBoundingClientRect();
    if (targetRect.width === 0 || targetRect.height === 0) return false;

    const startX = event && typeof event.clientX === 'number' ? event.clientX : (triggerRect.left + triggerRect.width / 2);
    const startY = event && typeof event.clientY === 'number' ? event.clientY : (triggerRect.bottom);
    if (!pointNearRect(startX, startY, triggerRect)) {
        return false;
    }

    const polygon = getSafePolygon(startX, startY, triggerRect, targetRect);

    clearSafePolygon(rootState);

    const onMove = (e) => {
        if (pointInPolygon(e.clientX, e.clientY, polygon)) {
            // Still inside the safe path; cancel any pending close.
            clearTimeout(rootState.closeTimer);
            rootState.closeTimer = null;
            return;
        }
        // Pointer left the polygon → schedule close as usual.
        clearSafePolygon(rootState);
        scheduleHoverClose(rootState);
    };

    rootState.safePolygon = { onMove };
    document.addEventListener('mousemove', onMove);

    // Schedule the original close in case the pointer never moves.
    scheduleHoverClose(rootState);
    return true;
}

function clearSafePolygon(rootState) {
    if (rootState.safePolygon) {
        document.removeEventListener('mousemove', rootState.safePolygon.onMove);
        rootState.safePolygon = null;
    }
}

function pointInPolygon(x, y, polygon) {
    if (!pointInPolygonBounds(x, y, polygon)) {
        return false;
    }

    let inside = false;
    for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
        const [xi, yi] = polygon[i];
        const [xj, yj] = polygon[j];
        const intersect =
            (yi > y) !== (yj > y) &&
            x < ((xj - xi) * (y - yi)) / (yj - yi + 1e-9) + xi;
        if (intersect) inside = !inside;
    }
    return inside;
}

function pointInPolygonBounds(x, y, polygon) {
    const padding = 16;
    const xs = polygon.map(([pointX]) => pointX);
    const ys = polygon.map(([, pointY]) => pointY);
    const minX = Math.min(...xs) - padding;
    const maxX = Math.max(...xs) + padding;
    const minY = Math.min(...ys) - padding;
    const maxY = Math.max(...ys) + padding;

    return x >= minX && x <= maxX && y >= minY && y <= maxY;
}

function removeHoverListeners(rootState, itemValue, element) {
    if (element._navMenuPointerEnter) {
        element.removeEventListener('pointerenter', element._navMenuPointerEnter);
        delete element._navMenuPointerEnter;
    }
    if (element._navMenuPointerDown) {
        element.removeEventListener('pointerdown', element._navMenuPointerDown);
        delete element._navMenuPointerDown;
    }
    if (element._navMenuEnter) {
        element.removeEventListener('mouseenter', element._navMenuEnter);
        delete element._navMenuEnter;
    }
    if (element._navMenuLeave) {
        element.removeEventListener('mouseleave', element._navMenuLeave);
        delete element._navMenuLeave;
    }
    if (element._navMenuClickCapture) {
        element.removeEventListener('click', element._navMenuClickCapture, true);
        delete element._navMenuClickCapture;
    }
    if (element._navMenuKeyDown) {
        element.removeEventListener('keydown', element._navMenuKeyDown);
        delete element._navMenuKeyDown;
    }
}

function pointNearRect(x, y, rect) {
    const padding = 16;

    return (
        x >= rect.left - padding &&
        x <= rect.right + padding &&
        y >= rect.top - padding &&
        y <= rect.bottom + padding
    );
}

function getSafePolygon(startX, startY, triggerRect, targetRect) {
    const padding = 8;
    const triggerCenterX = triggerRect.left + triggerRect.width / 2;
    const triggerCenterY = triggerRect.top + triggerRect.height / 2;
    const targetCenterX = targetRect.left + targetRect.width / 2;
    const targetCenterY = targetRect.top + targetRect.height / 2;
    const deltaX = targetCenterX - triggerCenterX;
    const deltaY = targetCenterY - triggerCenterY;

    if (Math.abs(deltaX) > Math.abs(deltaY)) {
        const targetX = deltaX > 0 ? targetRect.left - padding : targetRect.right + padding;

        return [
            [startX, startY],
            [targetX, targetRect.top - padding],
            [targetX, targetRect.bottom + padding]
        ];
    }

    const targetY = deltaY > 0 ? targetRect.top - padding : targetRect.bottom + padding;

    return [
        [startX, startY],
        [targetRect.left - padding, targetY],
        [targetRect.right + padding, targetY]
    ];
}

// --- Content Element Management ---

export function setContentElement(rootId, itemValue, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    if (!rootState.contentElements) {
        rootState.contentElements = new Map();
    }

    const oldElement = rootState.contentElements.get(itemValue);
    if (oldElement) {
        removeContentHoverListeners(oldElement);
    }

    const onEnter = () => {
        clearTimeout(rootState.closeTimer);
        rootState.closeTimer = null;
    };
    const onLeave = () => {
        rootState.closeTimer = setTimeout(() => {
            rootState.closeReason = 'trigger-hover';
            rootState.dotNetRef.invokeMethodAsync('OnHoverClose').catch(() => { });
        }, rootState.closeDelay);
    };

    element._navMenuContentEnter = onEnter;
    element._navMenuContentLeave = onLeave;
    element.addEventListener('mouseenter', onEnter);
    element.addEventListener('mouseleave', onLeave);

    rootState.contentElements.set(itemValue, element);
    moveContentIntoViewportTarget(rootState, element);
    syncContentVisibility(rootState);

    if (rootState.isOpen && rootState.value === itemValue) {
        observeContentSize(rootState, element);
        syncPopupAutoSize(rootState);
    }
}

export function disposeContentElement(rootId, itemValue) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !rootState.contentElements) return;

    const element = rootState.contentElements.get(itemValue);
    if (element) {
        removeContentHoverListeners(element);
        restoreContentParent(element);
        if (rootState.currentContentElement === element) {
            rootState.currentContentElement = null;
            rootState.mutationObserver?.disconnect();
            rootState.mutationObserver = null;
        }
    }
    rootState.contentElements.delete(itemValue);
    syncContentVisibility(rootState);
}

function restoreContentParent(element) {
    const parent = element._navMenuOriginalParent;
    const nextSibling = element._navMenuOriginalNextSibling;

    if (parent?.isConnected) {
        parent.insertBefore(element, nextSibling?.isConnected ? nextSibling : null);
    }

    delete element._navMenuOriginalParent;
    delete element._navMenuOriginalNextSibling;
}

function moveContentIntoViewportTarget(rootState, element) {
    const target = rootState.viewportTargetElement || rootState.viewportElement;
    if (!target || !element || element.parentNode === target) {
        return;
    }

    if (!element._navMenuOriginalParent) {
        element._navMenuOriginalParent = element.parentNode;
        element._navMenuOriginalNextSibling = element.nextSibling;
    }

    target.appendChild(element);
}

function removeContentHoverListeners(element) {
    if (element._navMenuContentEnter) {
        element.removeEventListener('mouseenter', element._navMenuContentEnter);
        delete element._navMenuContentEnter;
    }
    if (element._navMenuContentLeave) {
        element.removeEventListener('mouseleave', element._navMenuContentLeave);
        delete element._navMenuContentLeave;
    }
}

function revealPositioningSurface(rootState) {
    const elements = [
        rootState.positionerElement,
        rootState.popupElement,
        rootState.viewportElement,
        rootState.viewportTargetElement
    ];

    for (const element of elements) {
        if (!element) {
            continue;
        }

        element.removeAttribute('hidden');
        element.style.removeProperty('visibility');
    }

    if (rootState.positionerElement) {
        rootState.positionerElement.setAttribute('data-positioned', '');
    }
}

function syncContentVisibility(rootState) {
    if (!rootState.contentElements) {
        return;
    }

    const activeValue = rootState.value;
    const shouldShowActive = rootState.isOpen || rootState.pendingOpen;

    for (const [itemValue, element] of rootState.contentElements) {
        if (!element) {
            continue;
        }

        const active = shouldShowActive && itemValue === activeValue;
        if (active) {
            revealPositioningSurface(rootState);
            if (element.style.position === 'absolute') {
                element.style.removeProperty('position');
            }
            if (element.style.top === '0px') {
                element.style.removeProperty('top');
            }
            if (element.style.left === '0px') {
                element.style.removeProperty('left');
            }
            element.style.removeProperty('visibility');
            element.style.removeProperty('pointer-events');
            element.removeAttribute('inert');
            element.setAttribute('data-open', '');
            element.removeAttribute('data-closed');
        } else {
            element.style.setProperty('position', 'absolute');
            element.style.setProperty('top', '0px');
            element.style.setProperty('left', '0px');
            element.style.setProperty('visibility', 'hidden');
            element.style.setProperty('pointer-events', 'none');
            element.setAttribute('inert', '');
            element.setAttribute('data-closed', '');
            element.removeAttribute('data-open');
        }
    }
}

// --- Popup / Viewport Element Management ---

export function setPopupElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    // Clean up old popup listeners
    if (rootState.popupElement) {
        removePopupHoverListeners(rootState.popupElement);
    }

    rootState.popupElement = element;
    observePopupSize(rootState);

    // Cancel close timer when entering popup area
    if (element) {
        const onEnter = () => {
            clearTimeout(rootState.closeTimer);
            rootState.closeTimer = null;
        };
        const onLeave = () => {
            rootState.closeTimer = setTimeout(() => {
                rootState.closeReason = 'trigger-hover';
                rootState.dotNetRef.invokeMethodAsync('OnHoverClose').catch(() => { });
            }, rootState.closeDelay);
        };

        element._navMenuPopupEnter = onEnter;
        element._navMenuPopupLeave = onLeave;
        element.addEventListener('mouseenter', onEnter);
        element.addEventListener('mouseleave', onLeave);
    }
}

export function setPositionerElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.positionerElement = element;
    installPositionerResizeListener(rootState);
    syncPopupAutoSize(rootState);
}

function removePopupHoverListeners(element) {
    if (element._navMenuPopupEnter) {
        element.removeEventListener('mouseenter', element._navMenuPopupEnter);
        delete element._navMenuPopupEnter;
    }
    if (element._navMenuPopupLeave) {
        element.removeEventListener('mouseleave', element._navMenuPopupLeave);
        delete element._navMenuPopupLeave;
    }
}

export function setViewportElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    // Clean up old viewport listeners
    if (rootState.viewportElement) {
        removeViewportHoverListeners(rootState.viewportElement);
    }

    rootState.viewportElement = element;

    // Also keep open when hovering viewport
    if (element) {
        const onEnter = () => {
            clearTimeout(rootState.closeTimer);
            rootState.closeTimer = null;
        };
        const onLeave = () => {
            rootState.closeTimer = setTimeout(() => {
                rootState.closeReason = 'trigger-hover';
                rootState.dotNetRef.invokeMethodAsync('OnHoverClose').catch(() => { });
            }, rootState.closeDelay);
        };

        element._navMenuViewportEnter = onEnter;
        element._navMenuViewportLeave = onLeave;
        element.addEventListener('mouseenter', onEnter);
        element.addEventListener('mouseleave', onLeave);
    }
}

export function setViewportTargetElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.viewportTargetElement = element;

    if (rootState.contentElements) {
        for (const [, contentEl] of rootState.contentElements) {
            moveContentIntoViewportTarget(rootState, contentEl);
        }
    }
}

// --- Focus Management ---

export function focusPreviousTabbable(guardElement) {
    const previous = getPreviousTabbable(guardElement);
    previous?.focus({ preventScroll: true });
}

export function focusNavigationMenuContent(rootId, guardElement, fallbackElement) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    const focusTarget =
        getNextTabbable(rootState.popupElement) ||
        getNextTabbable(rootState.viewportElement) ||
        fallbackElement ||
        guardElement;

    focusTarget?.focus?.({ preventScroll: true });
}

function getTabbableElements(root) {
    if (!root?.ownerDocument) return [];

    const selector = [
        'a[href]',
        'button:not([disabled])',
        'input:not([disabled])',
        'select:not([disabled])',
        'textarea:not([disabled])',
        '[tabindex]:not([tabindex="-1"])'
    ].join(',');

    return Array.from(root.querySelectorAll(selector))
        .filter((element) => isFocusable(element));
}

function getPreviousTabbable(element) {
    if (!element?.ownerDocument) return null;

    const tabbables = getTabbableElements(element.ownerDocument.body);
    const index = tabbables.indexOf(element);
    return index > 0 ? tabbables[index - 1] : null;
}

function getNextTabbable(root) {
    return getTabbableElements(root)[0] ?? null;
}

function isFocusable(element) {
    if (!(element instanceof HTMLElement)) return false;
    if (element.hidden || element.getAttribute('aria-hidden') === 'true') return false;
    if (element.closest('[inert]')) return false;

    const style = element.ownerDocument.defaultView.getComputedStyle(element);
    return style.visibility !== 'hidden' && style.display !== 'none';
}

function maybeReturnFocus(rootState, wasOpen) {
    const blockedReasons = new Set(['trigger-hover', 'outside-press', 'focus-out']);
    if (!wasOpen || blockedReasons.has(rootState.closeReason)) {
        return;
    }

    const activeElement = rootState.popupElement?.ownerDocument?.activeElement;
    const shouldReturnFocus =
        activeElement === rootState.popupElement?.ownerDocument?.body ||
        rootState.popupElement?.contains(activeElement) ||
        rootState.viewportElement?.contains(activeElement);

    if (shouldReturnFocus) {
        rootState.activeTriggerElement?.focus?.({ preventScroll: true });
    }
}

function installPositionerResizeListener(rootState) {
    removePositionerResizeListener(rootState);

    if (!rootState.positionerElement?.ownerDocument) {
        return;
    }

    const win = rootState.positionerElement.ownerDocument.defaultView;
    const onResize = () => {
        if (!rootState.isOpen || !rootState.positionerElement) {
            return;
        }

        rootState.positionerElement.setAttribute('data-instant', '');
        clearTimeout(rootState.resizeInstantTimer);
        rootState.resizeInstantTimer = setTimeout(() => {
            rootState.positionerElement?.removeAttribute('data-instant');
        }, 100);
    };

    win.addEventListener('resize', onResize);
    rootState.resizeListener = { win, onResize };
}

function removePositionerResizeListener(rootState) {
    if (rootState.resizeListener) {
        rootState.resizeListener.win.removeEventListener('resize', rootState.resizeListener.onResize);
        rootState.resizeListener = null;
    }
}

// --- Popup Sizing ---

function observePopupSize(rootState) {
    rootState.resizeObserver?.disconnect();
    rootState.resizeObserver = null;

    if (!rootState.popupElement || typeof ResizeObserver !== 'function') {
        return;
    }

    rootState.resizeObserver = new ResizeObserver(() => {
        syncPopupAutoSize(rootState);
    });
    rootState.resizeObserver.observe(rootState.popupElement);
}

function observeContentSize(rootState, element) {
    rootState.currentContentElement = element;
    rootState.mutationObserver?.disconnect();
    rootState.mutationObserver = null;

    if (!element || typeof MutationObserver !== 'function') {
        return;
    }

    rootState.mutationObserver = new MutationObserver(() => {
        syncPopupAutoSize(rootState);
    });
    rootState.mutationObserver.observe(element, {
        childList: true,
        subtree: true,
        characterData: true
    });
}

function syncCurrentContentElement(rootState) {
    const contentElement = rootState.value != null
        ? (rootState.contentElements?.get(rootState.value) ?? null)
        : null;

    if (rootState.currentContentElement !== contentElement) {
        if (contentElement) {
            observeContentSize(rootState, contentElement);
        } else {
            rootState.currentContentElement = null;
            rootState.mutationObserver?.disconnect();
            rootState.mutationObserver = null;
        }
    }

    return contentElement;
}

function disconnectSizeObservers(rootState) {
    rootState.resizeObserver?.disconnect();
    rootState.mutationObserver?.disconnect();
    rootState.autoSizeAbortController?.abort();
    rootState.resizeObserver = null;
    rootState.mutationObserver = null;
    rootState.autoSizeAbortController = null;
}

function setSharedFixedSize(rootState) {
    const popupElement = rootState.popupElement;
    const positionerElement = rootState.positionerElement;

    if (!popupElement || !positionerElement) {
        return;
    }

    const width = popupElement.offsetWidth;
    const height = popupElement.offsetHeight;

    if (width === 0 || height === 0) {
        return;
    }

    popupElement.style.setProperty('--popup-width', `${width}px`);
    popupElement.style.setProperty('--popup-height', `${height}px`);
    positionerElement.style.setProperty('--positioner-width', `${width}px`);
    positionerElement.style.setProperty('--positioner-height', `${height}px`);
}

function syncPopupAutoSize(rootState) {
    const popupElement = rootState.popupElement;
    const positionerElement = rootState.positionerElement;

    if (!rootState.isOpen || !popupElement || !positionerElement) {
        return;
    }

    rootState.autoSizeAbortController?.abort();
    const abortController = new AbortController();
    rootState.autoSizeAbortController = abortController;

    requestAnimationFrame(() => {
        if (rootState.autoSizeAbortController !== abortController || abortController.signal.aborted) {
            return;
        }

        popupElement.style.setProperty('--popup-width', 'auto');
        popupElement.style.setProperty('--popup-height', 'auto');

        const measurementElement = syncCurrentContentElement(rootState) ?? popupElement;
        const width = measurementElement.offsetWidth;
        const height = measurementElement.offsetHeight;

        if (width === 0 || height === 0) {
            return;
        }

        popupElement.style.setProperty('--popup-width', `${width}px`);
        popupElement.style.setProperty('--popup-height', `${height}px`);
        positionerElement.style.setProperty('--positioner-width', `${width}px`);
        positionerElement.style.setProperty('--positioner-height', `${height}px`);
    });
}

function removeViewportHoverListeners(element) {
    if (element._navMenuViewportEnter) {
        element.removeEventListener('mouseenter', element._navMenuViewportEnter);
        delete element._navMenuViewportEnter;
    }
    if (element._navMenuViewportLeave) {
        element.removeEventListener('mouseleave', element._navMenuViewportLeave);
        delete element._navMenuViewportLeave;
    }
}

// --- Positioning ---

if (state.positionerIdCounter == null) {
    state.positionerIdCounter = 0;
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
    collisionAvoidance,
    dotNetRef
) {
    const id = `nav-pos-${++state.positionerIdCounter}`;

    const positionerState = {
        id,
        positionerElement,
        anchorElement,
        cleanup: null,
        dotNetRef,
        options: { side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, disableAnchorTracking, collisionAvoidance }
    };

    state.positioners.set(id, positionerState);

    positionerState.floatingPositionerId = await initializeFloatingPositioner({
        positionerElement,
        triggerElement: anchorElement,
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
        collisionAvoidance,
        preservePositionerStyles: false,
        hasViewport: true,
        onPositionUpdated: (effectiveSide, effectiveAlign, anchorHidden, arrowUncentered) => {
            dotNetRef?.invokeMethodAsync('OnPositionUpdated', effectiveSide, effectiveAlign, anchorHidden, arrowUncentered).catch(() => { });
        }
    });

    return id;
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
    collisionAvoidance
) {
    const positionerState = state.positioners.get(positionerId);
    if (!positionerState) return;

    positionerState.anchorElement = anchorElement;
    positionerState.options = { side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, collisionAvoidance };

    await updateFloatingPositioner(positionerState.floatingPositionerId, {
        triggerElement: anchorElement,
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
        collisionAvoidance
    });
}

export function disposePositioner(positionerId) {
    const positionerState = state.positioners.get(positionerId);
    if (!positionerState) return;

    if (positionerState.floatingPositionerId) {
        disposeFloatingPositioner(positionerState.floatingPositionerId);
    }

    state.positioners.delete(positionerId);
}
