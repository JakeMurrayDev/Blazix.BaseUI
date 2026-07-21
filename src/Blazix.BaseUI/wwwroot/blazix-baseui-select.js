/**
 * Blazix.BaseUI Select Component
 *
 * Select-specific functionality that builds on the shared floating infrastructure.
 */

import { acquireScrollLock } from './blazix-baseui-scroll-lock.min.js';
import {
    initializePositioner as floatingInitializePositioner,
    updatePositioner as floatingUpdatePositioner,
    resetPositioner as floatingResetPositioner,
    disposePositioner as floatingDisposePositioner,
    checkForTransitionOrAnimation,
    setupTransitionEndListener,
    cleanupTransitionState,
    contains,
    createVirtualElement,
    updateVirtualElement,
    disposeVirtualElement
} from './blazix-baseui-floating.min.js';

const BOUNDARY_OFFSET = 5;
const ALIGN_ITEM_PLACEMENT_MAX_ATTEMPTS = 3600;
const POINTER_COMPATIBILITY_WINDOW_MS = 750;
const orphanPositionerReadyElements = new WeakSet();

// ─── Popup Placement Constants & Helpers ──────────────────────────────
// alignItemWithTrigger measurement, scroll-growth, and CSS variable
// application live in JS (DOM-heavy-logic rule).

const SCROLL_EDGE_TOLERANCE_PX = 1;
const LIST_FUNCTIONAL_STYLES = 'position:relative;max-height:100%;overflow-x:hidden;overflow-y:auto;';
const TRANSFORM_STYLE_RESETS = [
    ['transform', 'none'],
    ['scale', '1'],
    ['translate', '0 0']
];

function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
}

function getMaxScrollTop(scroller) {
    return Math.max(0, scroller.scrollHeight - scroller.clientHeight);
}

function normalizeScrollOffset(value, max) {
    if (max <= 0) return 0;
    const clamped = Math.max(0, Math.min(value, max));
    const startDistance = clamped;
    const endDistance = max - clamped;
    const withinStartTolerance = startDistance <= SCROLL_EDGE_TOLERANCE_PX;
    const withinEndTolerance = endDistance <= SCROLL_EDGE_TOLERANCE_PX;

    if (withinStartTolerance && withinEndTolerance) {
        return startDistance <= endDistance ? 0 : max;
    }
    if (withinStartTolerance) return 0;
    if (withinEndTolerance) return max;
    return clamped;
}

function getTargetScrollTop(items, isUp, scrollTop, clientHeight, scrollArrowHeight, maxScrollTop) {
    if (isUp) {
        let firstVisibleIndex = 0;
        const visibleTop = scrollTop + scrollArrowHeight - SCROLL_EDGE_TOLERANCE_PX;

        for (let i = 0; i < items.length; i++) {
            const item = items[i];
            if (item && item.offsetTop >= visibleTop) {
                firstVisibleIndex = i;
                break;
            }
        }

        const targetIndex = Math.max(0, firstVisibleIndex - 1);
        const targetItem = items[targetIndex];
        return targetIndex < firstVisibleIndex && targetItem
            ? normalizeScrollOffset(targetItem.offsetTop - scrollArrowHeight, maxScrollTop)
            : 0;
    }

    let lastVisibleIndex = items.length - 1;
    const visibleBottom = scrollTop + clientHeight - scrollArrowHeight + SCROLL_EDGE_TOLERANCE_PX;

    for (let i = 0; i < items.length; i++) {
        const item = items[i];
        if (item && item.offsetTop + item.offsetHeight > visibleBottom) {
            lastVisibleIndex = Math.max(0, i - 1);
            break;
        }
    }

    const targetIndex = Math.min(items.length - 1, lastVisibleIndex + 1);
    const targetItem = items[targetIndex];
    return targetIndex > lastVisibleIndex && targetItem
        ? normalizeScrollOffset(
            targetItem.offsetTop + targetItem.offsetHeight - clientHeight + scrollArrowHeight,
            maxScrollTop
          )
        : maxScrollTop;
}

function scrollItemIntoViewIfNeeded(scroller, item) {
    if (!scroller || !item || !scroller.scrollTo) return;
    if (scroller.clientHeight >= scroller.scrollHeight) return;

    const scrollerStyles = getComputedStyle(scroller);
    const itemStyles = getComputedStyle(item);
    const scrollPaddingTop = parseFloat(scrollerStyles.scrollPaddingTop) || 0;
    const scrollPaddingBottom = parseFloat(scrollerStyles.scrollPaddingBottom) || 0;
    const scrollMarginTop = parseFloat(itemStyles.scrollMarginTop) || 0;
    const scrollMarginBottom = parseFloat(itemStyles.scrollMarginBottom) || 0;
    const scrollerRect = scroller.getBoundingClientRect();
    const itemRect = item.getBoundingClientRect();
    const itemTop = itemRect.top - scrollerRect.top - scroller.clientTop + scroller.scrollTop;
    let targetTop = scroller.scrollTop;

    if (itemTop - scrollMarginTop < scroller.scrollTop + scrollPaddingTop) {
        targetTop = itemTop - scrollMarginTop - scrollPaddingTop;
    } else if (
        itemTop + item.offsetHeight + scrollMarginBottom >
        scroller.scrollTop + scroller.clientHeight - scrollPaddingBottom
    ) {
        targetTop =
            itemTop + item.offsetHeight + scrollMarginBottom -
            scroller.clientHeight + scrollPaddingBottom;
    }

    scroller.scrollTo({ top: targetTop, behavior: 'auto' });
}

function getScale(el) {
    const rect = el.getBoundingClientRect();
    const w = el.offsetWidth;
    const h = el.offsetHeight;
    const x = w > 0 ? Math.round(rect.width) / w : 1;
    const y = h > 0 ? Math.round(rect.height) / h : 1;
    return {
        x: isNaN(x) || !isFinite(x) || x === 0 ? 1 : x,
        y: isNaN(y) || !isFinite(y) || y === 0 ? 1 : y
    };
}

function normalizeRect(rect, scale) {
    const x = rect.x / scale.x;
    const y = rect.y / scale.y;
    const width = rect.width / scale.x;
    const height = rect.height / scale.y;
    return {
        x,
        y,
        width,
        height,
        top: y,
        left: x,
        right: x + width,
        bottom: y + height
    };
}

function isWebKit() {
    if (typeof navigator === 'undefined') return false;
    return /\bAppleWebKit\b/.test(navigator.userAgent) && !/\bChrome\b/.test(navigator.userAgent);
}

function isVirtualClickEvent(event) {
    if (event.pointerType === '' && event.isTrusted) {
        return true;
    }

    if (typeof navigator !== 'undefined' && /\bAndroid\b/i.test(navigator.userAgent) && event.pointerType) {
        return event.type === 'click' && event.buttons === 1;
    }

    return event.detail === 0 && !event.pointerType;
}

function unsetTransformStyles(popupElement) {
    const style = popupElement.style;
    const originalStyles = {};

    for (const [property, value] of TRANSFORM_STYLE_RESETS) {
        originalStyles[property] = style.getPropertyValue(property);
        style.setProperty(property, value, 'important');
    }

    return () => {
        for (const [property] of TRANSFORM_STYLE_RESETS) {
            const originalValue = originalStyles[property];
            if (originalValue) {
                style.setProperty(property, originalValue);
            } else {
                style.removeProperty(property);
            }
        }
    };
}

function getMaxPopupHeight(popupStyles) {
    const maxHeightStyle = popupStyles.maxHeight || '';
    if (maxHeightStyle.endsWith('px')) {
        const parsed = parseFloat(maxHeightStyle);
        return isNaN(parsed) ? Infinity : parsed;
    }
    return Infinity;
}

function ensurePopupState(rootState) {
    if (!rootState.popup) {
        rootState.popup = {
            popupElement: null,
            dotNetRef: null,
            originalPositionerStyles: {},
            savedPositionerStyles: false,
            reachedMaxHeight: false,
            initialPlaced: false,
            scrollArrowRaf: 0,
            pointerLeaveTimer: null,
            pointerLeaveHandler: null,
            pointerDownHandler: null,
            touchStartHandler: null,
            keyDownHandler: null,
            clickHandler: null,
            mouseMoveHandler: null,
            scrollHandler: null,
            resizeHandler: null,
            alignItemWithTriggerActive: false,
            standardFallbackPending: false,
            placementInProgressRevision: -1,
            placementInProgressInputRevision: -1,
            placementCommittedRevision: -1,
            placementCommittedInputRevision: -1
        };
    }
    return rootState.popup;
}

function invalidateAlignItemPlacement(rootState) {
    rootState.placementInputRevision += 1;
}

function isMouseWithinBounds(event) {
    if (event.movementX === 0 && event.movementY === 0) {
        return true;
    }
    return false;
}

function getPseudoElementBounds(el) {
    const rect = el.getBoundingClientRect();
    const win = el.ownerDocument.defaultView;
    if (!win) return rect;
    const before = win.getComputedStyle(el, '::before');
    const after = win.getComputedStyle(el, '::after');
    if (before.content === 'none' && after.content === 'none') return rect;
    const bw = parseFloat(before.width) || 0;
    const bh = parseFloat(before.height) || 0;
    const aw = parseFloat(after.width) || 0;
    const ah = parseFloat(after.height) || 0;
    const w = Math.max(rect.width, bw, aw);
    const h = Math.max(rect.height, bh, ah);
    const dw = (w - rect.width) / 2;
    const dh = (h - rect.height) / 2;
    return {
        left: rect.left - dw,
        right: rect.right + dw,
        top: rect.top - dh,
        bottom: rect.bottom + dh
    };
}

function normalizeInteractionType(pointerType) {
    if (pointerType === 'touch') return 'touch';
    if (pointerType === 'pen') return 'pen';
    return 'mouse';
}

function getEventTimestamp(event) {
    return typeof event.timeStamp === 'number' && event.timeStamp > 0
        ? event.timeStamp
        : performance.now();
}

const STATE_KEY = Symbol.for('Blazix.BaseUI.Select.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        roots: new Map(),
        positioners: new Map(),
        globalListenersInitialized: false
    };
}
const state = window[STATE_KEY];
const partTransitions = new WeakMap();

export function beginPartTransition(element, dotNetRef, isOpen) {
    if (!(element instanceof HTMLElement) || !dotNetRef) return;

    const previous = partTransitions.get(element);
    if (previous) {
        cleanupTransitionState(previous);
        if (previous.rafId) cancelAnimationFrame(previous.rafId);
    }

    const transitionState = {
        popupElement: element,
        dotNetRef,
        transitionCleanup: null,
        fallbackTimeoutId: null,
        rafId: 0
    };
    partTransitions.set(element, transitionState);

    if (isOpen) {
        transitionState.rafId = requestAnimationFrame(() => {
            transitionState.rafId = requestAnimationFrame(() => {
                transitionState.rafId = 0;
                if (partTransitions.get(element) === transitionState) {
                    dotNetRef.invokeMethodAsync('OnTransitionEnd', true).catch(() => { });
                }
            });
        });
        return;
    }

    if (checkForTransitionOrAnimation(element)) {
        setupTransitionEndListener(transitionState, false);
    } else {
        queueMicrotask(() => {
            if (partTransitions.get(element) === transitionState) {
                dotNetRef.invokeMethodAsync('OnTransitionEnd', false).catch(() => { });
            }
        });
    }
}

export function disposePartTransition(element) {
    const transitionState = partTransitions.get(element);
    if (!transitionState) return;
    cleanupTransitionState(transitionState);
    if (transitionState.rafId) cancelAnimationFrame(transitionState.rafId);
    partTransitions.delete(element);
}

function initGlobalListeners() {
    if (state.globalListenersInitialized) return;

    document.addEventListener('keydown', handleGlobalKeyDown, { capture: true });
    document.addEventListener('pointerdown', handleGlobalPointerDown, { capture: true });
    document.addEventListener('touchstart', handleGlobalTouchStart, { capture: true, passive: true });
    document.addEventListener('mousedown', handleGlobalMouseDown);
    state.globalListenersInitialized = true;
}

function handleGlobalKeyDown(e) {
    let topmostRoot = null;

    for (const [id, rootState] of state.roots) {
        if (rootState.isOpen && rootState.dotNetRef) {
            topmostRoot = rootState;
        }
    }

    if (!topmostRoot) return;

    topmostRoot.lastInteractionType = 'keyboard';

    if (!topmostRoot.keyboardActive) {
        topmostRoot.keyboardActive = true;
        topmostRoot.dotNetRef.invokeMethodAsync('OnKeyboardActiveChange', true).catch(() => { });
    }

    if (e.key === 'Escape') {
        e.preventDefault();
        e.stopPropagation();
        topmostRoot.dotNetRef.invokeMethodAsync('OnEscapeKey').catch(() => { });
        return;
    }

    // Match React's `enabled: !readOnly && !disabled` on useListNavigation,
    // useTypeahead, and useClick: a readonly select that is somehow open
    // accepts only Escape and Tab.
    if (topmostRoot.readOnly || topmostRoot.disabled) {
        if (e.key === 'Tab') {
            topmostRoot.dotNetRef.invokeMethodAsync('OnTabKey').catch(() => { });
        }
        return;
    }

    const popupEl = topmostRoot.popupElement;
    const listEl = topmostRoot.listElement;
    const containerEl = listEl || popupEl;
    if (!containerEl) return;

    const items = getNavigableItems(containerEl);
    if (items.length === 0) return;

    const currentIndex = topmostRoot.activeIndex;

    if (e.key === 'ArrowDown') {
        e.preventDefault();
        const nextIndex = findNextIndex(items, currentIndex, 1, topmostRoot.loopFocus);
        if (nextIndex !== -1) {
            setActiveItem(topmostRoot, items, nextIndex);
        }
    } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        const nextIndex = findNextIndex(items, currentIndex, -1, topmostRoot.loopFocus);
        if (nextIndex !== -1) {
            setActiveItem(topmostRoot, items, nextIndex);
        }
    } else if (e.key === 'Home') {
        e.preventDefault();
        const nextIndex = findNextIndex(items, -1, 1, false);
        if (nextIndex !== -1) {
            setActiveItem(topmostRoot, items, nextIndex);
        }
    } else if (e.key === 'End') {
        e.preventDefault();
        const nextIndex = findNextIndex(items, items.length, -1, false);
        if (nextIndex !== -1) {
            setActiveItem(topmostRoot, items, nextIndex);
        }
    } else if (e.key === 'Enter') {
        if (currentIndex >= 0 && currentIndex < items.length) {
            activateItemFromKeydown(e, items[currentIndex]);
        }
    } else if (e.key === ' ') {
        // React parity: when a typeahead query is mid-flight, append the space
        // to the query instead of committing. This lets users search for labels
        // that contain spaces ("San Fr").
        if (topmostRoot.typeaheadBuffer && topmostRoot.typeaheadBuffer.length > 0) {
            e.preventDefault();
            handleTypeahead(topmostRoot, items, ' ');
        } else if (currentIndex >= 0 && currentIndex < items.length) {
            activateItemFromKeydown(e, items[currentIndex]);
        }
    } else if (e.key === 'Tab') {
        topmostRoot.dotNetRef.invokeMethodAsync('OnTabKey').catch(() => { });
    } else if (e.key.length === 1 && !e.ctrlKey && !e.altKey && !e.metaKey) {
        handleTypeahead(topmostRoot, items, e.key);
    }
}

function activateItemFromKeydown(event, item) {
    const originalPreventDefault = event.preventDefault;
    let consumerPrevented = false;
    event.preventDefault = () => {
        consumerPrevented = true;
        originalPreventDefault.call(event);
    };

    queueMicrotask(() => {
        event.preventDefault = originalPreventDefault;
        if (consumerPrevented || event.defaultPrevented) return;

        originalPreventDefault.call(event);
        const win = item.ownerDocument.defaultView;
        item.dispatchEvent(new win.MouseEvent('click', {
            bubbles: true,
            cancelable: true,
            view: win,
            detail: 0,
            shiftKey: event.shiftKey,
            ctrlKey: event.ctrlKey,
            altKey: event.altKey,
            metaKey: event.metaKey
        }));
    });
}

function setPointerInteraction(rootState, interactionType, event = null) {
    rootState.lastInteractionType = interactionType;

    if (event) {
        rootState.lastPointerInteractionType = interactionType;
        rootState.lastPointerInteractionTime = getEventTimestamp(event);
    }

    if (rootState.keyboardActive) {
        rootState.keyboardActive = false;
        rootState.dotNetRef.invokeMethodAsync('OnKeyboardActiveChange', false).catch(() => { });
    }
}

function isEventInsideSelect(rootId, rootState, target) {
    const triggerEl = rootState.triggerElement;
    const popupEl = rootState.popupElement;
    const positionerEl = rootState.positionerElement;

    if (triggerEl && triggerEl.contains(target)) return true;
    if (popupEl && popupEl.contains(target)) return true;
    if (positionerEl && positionerEl.contains(target)) return true;

    for (const [posId, posState] of state.positioners) {
        if (posState.rootId === rootId && posState.element && posState.element.contains(target)) {
            return true;
        }
    }

    return false;
}

function rememberOutsidePointer(rootState, event, interactionType, handled) {
    rootState.lastOutsidePointerType = interactionType;
    rootState.lastOutsidePointerTime = getEventTimestamp(event);
    rootState.lastOutsidePointerHandled = handled;
}

function getRecentOutsidePointer(rootState, event) {
    if (!rootState.lastOutsidePointerType) return null;

    const elapsed = getEventTimestamp(event) - rootState.lastOutsidePointerTime;
    if (elapsed < 0 || elapsed > POINTER_COMPATIBILITY_WINDOW_MS) return null;

    return {
        interactionType: rootState.lastOutsidePointerType,
        handled: rootState.lastOutsidePointerHandled
    };
}

function getRecentPointerInteraction(rootState, event) {
    if (!rootState.lastPointerInteractionType) return null;

    const elapsed = getEventTimestamp(event) - rootState.lastPointerInteractionTime;
    if (elapsed < 0 || elapsed > POINTER_COMPATIBILITY_WINDOW_MS) return null;

    return rootState.lastPointerInteractionType;
}

function clearOutsidePointer(rootState) {
    rootState.lastOutsidePointerType = null;
    rootState.lastOutsidePointerTime = 0;
    rootState.lastOutsidePointerHandled = false;
}

function handleGlobalPointerDown(e) {
    for (const [id, rootState] of state.roots) {
        if (!rootState.dotNetRef) continue;

        const interactionType = normalizeInteractionType(e.pointerType);
        setPointerInteraction(rootState, interactionType, e);

        if (!rootState.isOpen) continue;
        if (isEventInsideSelect(id, rootState, e.target)) continue;

        rememberOutsidePointer(rootState, e, interactionType, true);
        rootState.dotNetRef.invokeMethodAsync('OnOutsidePress').catch(() => { });
    }
}

function handleGlobalTouchStart(e) {
    for (const [id, rootState] of state.roots) {
        if (!rootState.dotNetRef) continue;

        setPointerInteraction(rootState, 'touch', e);

        if (!rootState.isOpen) continue;
        if (isEventInsideSelect(id, rootState, e.target)) continue;

        if (getRecentOutsidePointer(rootState, e)?.handled) continue;

        rememberOutsidePointer(rootState, e, 'touch', false);
    }
}

function handleGlobalMouseDown(e) {
    for (const [id, rootState] of state.roots) {
        if (!rootState.dotNetRef) continue;

        const recentOutsidePointer = getRecentOutsidePointer(rootState, e);
        const recentPointerInteraction = recentOutsidePointer ? null : getRecentPointerInteraction(rootState, e);
        const interactionType = recentOutsidePointer?.interactionType || recentPointerInteraction || 'mouse';
        setPointerInteraction(rootState, interactionType, interactionType === 'mouse' ? e : null);

        if (recentOutsidePointer?.handled) {
            clearOutsidePointer(rootState);
            continue;
        }

        clearOutsidePointer(rootState);

        if (!rootState.isOpen) continue;
        if (isEventInsideSelect(id, rootState, e.target)) continue;

        rootState.dotNetRef.invokeMethodAsync('OnOutsidePress').catch(() => { });
    }
}

function getNavigableItems(container) {
    return Array.from(container.querySelectorAll('[role="option"]'));
}

function isItemDisabled(item) {
    return item.hasAttribute('disabled') || item.getAttribute('aria-disabled') === 'true';
}

function findNextIndex(items, currentIndex, direction, loop) {
    const length = items.length;
    if (length === 0) return -1;

    let index = currentIndex + direction;

    for (let i = 0; i < length; i++) {
        if (index < 0) {
            index = loop ? length - 1 : 0;
            if (!loop) return -1;
        } else if (index >= length) {
            index = loop ? 0 : length - 1;
            if (!loop) return -1;
        }

        return index;
    }

    return -1;
}

function findNextEnabledIndex(items, currentIndex, direction, loop) {
    const length = items.length;
    if (length === 0) return -1;

    let index = currentIndex + direction;
    for (let i = 0; i < length; i++) {
        if (index < 0) {
            index = loop ? length - 1 : 0;
            if (!loop) return -1;
        } else if (index >= length) {
            index = loop ? 0 : length - 1;
            if (!loop) return -1;
        }

        if (!isItemDisabled(items[index])) return index;
        index += direction;
    }

    return -1;
}

function handleTypeahead(rootState, items, char) {
    clearTimeout(rootState.typeaheadTimer);
    rootState.typeaheadBuffer += char.toLowerCase();

    rootState.typeaheadTimer = setTimeout(() => {
        rootState.typeaheadBuffer = '';
    }, 500);

    const startIndex = rootState.activeIndex >= 0 ? rootState.activeIndex : -1;
    const query = rootState.typeaheadBuffer.length > 1 &&
        Array.from(rootState.typeaheadBuffer).every(value => value === rootState.typeaheadBuffer[0])
        ? rootState.typeaheadBuffer[0]
        : rootState.typeaheadBuffer;

    for (let offset = 1; offset <= items.length; offset++) {
        const index = (startIndex + offset) % items.length;
        const item = items[index];
        if (isItemDisabled(item)) {
            continue;
        }

        // Prefer an explicit author-provided label via data-blazix-base-ui-label;
        // fall back to SelectItemText's rendered textContent. Mirrors React
        // `useCompositeListItem({ label, textRef })` where `label` wins and textRef
        // is used when no explicit label is supplied.
        // React parity: no trim — explicit `data-blazix-base-ui-label` is consumed verbatim,
        // and `textContent` is used as-is to mirror React's `useCompositeListItem({ textRef })`.
        const label = item.getAttribute('data-blazix-base-ui-label') || item.textContent || '';
        if (label.toLowerCase().startsWith(query)) {
            setActiveItem(rootState, items, index);
            return;
        }
    }
}

function setActiveItem(rootState, items, index) {
    const renderedList = items[index]?.closest('[role="listbox"]');
    const registeredList = rootState.listElement?.isConnected ? rootState.listElement : null;
    const container = registeredList || renderedList || rootState.popupElement;

    items.forEach((item, i) => {
        if (i === index) {
            item.setAttribute('data-highlighted', '');
            item.setAttribute('tabindex', '0');
            item.focus({ preventScroll: true });
            scrollItemIntoViewIfNeeded(container, item);
        } else {
            item.removeAttribute('data-highlighted');
            item.setAttribute('tabindex', '-1');
        }
    });

    rootState.activeIndex = index;
    rootState.dotNetRef.invokeMethodAsync('OnActiveIndexChange', index).catch(() => { });
}

function scheduleOpenAlignItemPlacement(rootId, attempt = 0) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !rootState.isOpen) return;

    const popupState = rootState.popup;
    const popupElement = popupState?.popupElement || rootState.popupElement;
    const positionerElement = rootState.positionerElement;
    const alignItemDomActive =
        positionerElement?.getAttribute('data-side') === 'none' ||
        popupElement?.classList.contains('blazix-base-ui-disable-scrollbar');
    if (attempt > ALIGN_ITEM_PLACEMENT_MAX_ATTEMPTS) return;
    if (!popupState && !alignItemDomActive) {
        requestAnimationFrame(() => scheduleOpenAlignItemPlacement(rootId, attempt + 1));
        return;
    }
    if (popupState && !popupState.alignItemWithTriggerActive && !alignItemDomActive) {
        return;
    }
    if (popupState?.standardFallbackPending) return;
    if (
        popupState?.placementCommittedRevision === rootState.openRevision &&
        popupState?.placementCommittedInputRevision === rootState.placementInputRevision
    ) return;

    requestAnimationFrame(() => {
        const nextPopupState = rootState.popup;
        const nextPopupElement = nextPopupState?.popupElement || rootState.popupElement;
        const nextPositionerElement = rootState.positionerElement;
        const nextAlignItemDomActive =
            nextPositionerElement?.getAttribute('data-side') === 'none' ||
            nextPopupElement?.classList.contains('blazix-base-ui-disable-scrollbar');
        if (!rootState.isOpen) return;
        if (nextPopupState && !nextPopupState.alignItemWithTriggerActive && !nextAlignItemDomActive) {
            return;
        }
        if (nextPopupState?.standardFallbackPending) return;
        if (
            nextPopupState?.placementCommittedRevision === rootState.openRevision &&
            nextPopupState?.placementCommittedInputRevision === rootState.placementInputRevision
        ) return;
        if (!nextPopupState && !nextAlignItemDomActive) {
            scheduleOpenAlignItemPlacement(rootId, attempt + 1);
            return;
        }

        const triggerElement = rootState.triggerElement;
        if (!nextPopupElement || !nextPositionerElement || !triggerElement) {
            scheduleOpenAlignItemPlacement(rootId, attempt + 1);
            return;
        }

        if (
            rootState.positionerReadyElement !== nextPositionerElement ||
            rootState.positionerReadyRevision !== rootState.openRevision
        ) {
            scheduleOpenAlignItemPlacement(rootId, attempt + 1);
            return;
        }

        const positionerRect = nextPositionerElement.getBoundingClientRect();
        const popupRect = nextPopupElement.getBoundingClientRect();
        const scroller = rootState.listElement || nextPopupElement;
        if (
            positionerRect.width === 0 ||
            positionerRect.height === 0 ||
            popupRect.width === 0 ||
            popupRect.height === 0 ||
            scroller.scrollHeight === 0
        ) {
            scheduleOpenAlignItemPlacement(rootId, attempt + 1);
            return;
        }

        beginAlignItemWithTriggerPlacement(rootId, true);
    });
}

function queueOpenAlignItemPlacement(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !rootState.isOpen) return;

    scheduleOpenAlignItemPlacement(rootId);
    startOpenAlignItemPlacementWatchdog(rootId);
}

function startOpenAlignItemPlacementWatchdog(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !rootState.isOpen || rootState.alignItemPlacementWatchdog) return;

    rootState.alignItemPlacementWatchdog = true;
    const startedAt = performance.now();

    const tick = () => {
        const currentRootState = state.roots.get(rootId);
        if (!currentRootState || !currentRootState.isOpen) {
            if (currentRootState) currentRootState.alignItemPlacementWatchdog = false;
            return;
        }

        const popupState = currentRootState.popup;
        const popupElement = popupState?.popupElement || currentRootState.popupElement;
        const positionerElement = currentRootState.positionerElement;
        const alignItemDomActive =
            positionerElement?.getAttribute('data-side') === 'none' ||
            popupElement?.classList.contains('blazix-base-ui-disable-scrollbar');
        const alignItemActive =
            !!popupState?.alignItemWithTriggerActive || alignItemDomActive;
        const positionerReady =
            !!positionerElement &&
            currentRootState.positionerReadyElement === positionerElement &&
            currentRootState.positionerReadyRevision === currentRootState.openRevision;
        const positioned = !!positionerElement?.hasAttribute('data-positioned');

        if (
            alignItemActive &&
            positionerReady &&
            !positioned &&
            popupElement &&
            positionerElement &&
            currentRootState.triggerElement
        ) {
            try {
                beginAlignItemWithTriggerPlacement(rootId, true);
            } catch {
                // Placement is idempotent and retried below while the popup stays open.
            }

            // A current-revision Floating pass is ready, so the local align
            // commit runs synchronously. Re-read the DOM ownership token before
            // deciding it failed; the value captured before the call is stale
            // when a loaded WASM open first becomes ready after 250 ms. If it
            // still cannot commit, request the standard Floating UI path through the
            // component callback. Never expose geometry directly from here.
            if (
                !positionerElement.hasAttribute('data-positioned') &&
                performance.now() - startedAt > 250 &&
                requestStandardPositioningFallback(currentRootState, popupState)
            ) {
                currentRootState.alignItemPlacementWatchdog = false;
                return;
            }

        }

        const elapsed = performance.now() - startedAt;
        if (!positioned && elapsed < 30000) {
            requestAnimationFrame(tick);
            return;
        }

        currentRootState.alignItemPlacementWatchdog = false;
    };

    requestAnimationFrame(tick);
}

function requestStandardPositioningFallback(rootState, popupState) {
    if (!popupState?.dotNetRef) return false;
    // A committed align-item layout remains authoritative until a real
    // placement input changes. A late watchdog or duplicate callback from the
    // same revision must not replace it with standard Floating placement and
    // visibly collapse the popup after it has opened.
    if (
        popupState.placementCommittedRevision === rootState.openRevision &&
        popupState.placementCommittedInputRevision === rootState.placementInputRevision
    ) return false;
    if (popupState.standardFallbackPending) return true;

    popupState.standardFallbackPending = true;
    clearPopupStylesInternal(rootState);
    popupState.initialPlaced = true;
    popupState.alignItemWithTriggerActive = false;

    // Do not make completed standard placement depend on a Blazor round-trip.
    // Under WASM suite load (and on a congested Server circuit), the callback
    // can be processed after the visibility watchdog has yielded. Switch the
    // live Floating registration to the standard path immediately; Floating UI
    // remains the sole owner of data-positioned and releases visibility only
    // after its size middleware and coordinate writes have completed.
    for (const [positionerId, registration] of state.positioners) {
        if (registration.positionerElement !== rootState.positionerElement) continue;

        registration.alignItemWithTriggerActive = false;
        floatingUpdatePositioner(positionerId, {
            alignItemWithTriggerActive: false,
            disableAnchorTracking: registration.disableAnchorTracking
        }).catch(() => { });
        break;
    }

    popupState.dotNetRef.invokeMethodAsync('OnFallbackToAlignPopupToTrigger').catch(() => { });
    return true;
}

// ─── Popup Placement ──────────────────────────────────────────────────
// alignItemWithTrigger pipeline (measurement, scroll-growth, pinch-zoom
// fallback, --transform-origin). C# wiring calls
// `beginAlignItemWithTriggerPlacement` once Floating UI has committed the
// current open/input revision. DOM readiness retries stay in requestAnimationFrame.

function saveOriginalPositionerStyles(popupState, positionerElement) {
    if (popupState.savedPositionerStyles) return;
    popupState.originalPositionerStyles = {
        position: positionerElement.style.position,
        top: positionerElement.style.top || '0',
        left: positionerElement.style.left || '0',
        right: positionerElement.style.right,
        height: positionerElement.style.height,
        bottom: positionerElement.style.bottom,
        minHeight: positionerElement.style.minHeight,
        maxHeight: positionerElement.style.maxHeight,
        marginTop: positionerElement.style.marginTop,
        marginBottom: positionerElement.style.marginBottom
    };
    popupState.savedPositionerStyles = true;
}

function handlePopupScrollInternal(rootState, scroller) {
    const popupState = rootState.popup;
    if (!popupState) return;
    const popupElement = popupState.popupElement;
    const positionerElement = rootState.positionerElement;
    if (!popupElement || !positionerElement || !popupState.initialPlaced) {
        return;
    }

    const isTopPositioned = positionerElement.style.top === '0px';
    const isBottomPositioned = positionerElement.style.bottom === '0px';

    if (
        popupState.reachedMaxHeight ||
        !popupState.alignItemWithTriggerActive ||
        (!isTopPositioned && !isBottomPositioned)
    ) {
        notifyScrollArrowVisibility(rootState);
        return;
    }

    const scale = getScale(positionerElement);
    const currentHeight = positionerElement.getBoundingClientRect().height / scale.y;
    const doc = positionerElement.ownerDocument;
    const positionerStyles = getComputedStyle(positionerElement);
    const marginTop = parseFloat(positionerStyles.marginTop) || 0;
    const marginBottom = parseFloat(positionerStyles.marginBottom) || 0;
    const maxPopupHeight = getMaxPopupHeight(getComputedStyle(popupElement));
    const maxAvailableHeight = Math.min(
        doc.documentElement.clientHeight - marginTop - marginBottom,
        maxPopupHeight
    );

    const scrollTop = scroller.scrollTop;
    const maxScrollTop = getMaxScrollTop(scroller);

    // `Infinity` requests a scroll to the recomputed maximum offset.
    let nextScrollTop = null;

    const setHeight = (height) => {
        positionerElement.style.height = `${height}px`;
    };

    const diff = isTopPositioned ? maxScrollTop - scrollTop : scrollTop;
    const nextHeight = Math.min(currentHeight + diff, maxAvailableHeight);

    if (diff <= SCROLL_EDGE_TOLERANCE_PX) {
        const heightDelta = clamp(diff, 0, maxAvailableHeight - currentHeight);
        if (heightDelta > 0) {
            setHeight(currentHeight + heightDelta);
        }
        scroller.scrollTop = isTopPositioned ? maxScrollTop : 0;
        if (maxAvailableHeight - (currentHeight + heightDelta) <= SCROLL_EDGE_TOLERANCE_PX) {
            popupState.reachedMaxHeight = true;
        }
        notifyScrollArrowVisibility(rootState);
        return;
    }

    if (maxAvailableHeight - nextHeight > SCROLL_EDGE_TOLERANCE_PX) {
        nextScrollTop = isTopPositioned ? Infinity : 0;
    } else if (isBottomPositioned && scrollTop < maxScrollTop) {
        const overshoot = currentHeight + diff - maxAvailableHeight;
        nextScrollTop = scrollTop - (diff - overshoot);
    }

    const nextPositionerHeight = Math.ceil(nextHeight);

    if (nextPositionerHeight !== 0) {
        setHeight(nextPositionerHeight);
    }

    if (nextScrollTop != null) {
        const target = clamp(nextScrollTop, 0, getMaxScrollTop(scroller));
        if (Math.abs(scroller.scrollTop - target) > SCROLL_EDGE_TOLERANCE_PX) {
            scroller.scrollTop = target;
        }
    }

    if (nextPositionerHeight >= maxAvailableHeight - SCROLL_EDGE_TOLERANCE_PX) {
        popupState.reachedMaxHeight = true;
    }

    notifyScrollArrowVisibility(rootState);
}

// ─── Public API ───────────────────────────────────────────────────────

export function initializeRoot(rootId, dotNetRef, loopFocus, modal, direction, readOnly, disabled) {
    initGlobalListeners();

    state.roots.set(rootId, {
        rootId,
        dotNetRef,
        isOpen: false,
        loopFocus: loopFocus ?? false,
        modal: modal ?? false,
        direction: direction ?? 'ltr',
        readOnly: !!readOnly,
        disabled: !!disabled,
        activeIndex: -1,
        selectedIndex: -1,
        keyboardActive: false,
        triggerElement: null,
        popupElement: null,
        listElement: null,
        positionerElement: null,
        positionerReadyElement: null,
        openRevision: 0,
        positionerReadyRevision: -1,
        pendingPositionerReadyElement: null,
        pendingPositionerReadyRevision: -1,
        acceptNextOpenPositioning: false,
        triggerCleanup: null,
        triggerDotNetRef: null,
        scrollLockCleanup: null,
        typeaheadBuffer: '',
        typeaheadTimer: null,
        scrollListener: null,
        continuousScrollInterval: null,
        scrollUpArrow: null,
        scrollDownArrow: null,
        transitionCleanup: null,
        fallbackTimeoutId: null,
        finalFocusManaged: false,
        lastInteractionType: 'none',
        lastPointerInteractionType: null,
        lastPointerInteractionTime: 0,
        lastOutsidePointerType: null,
        lastOutsidePointerTime: 0,
        lastOutsidePointerHandled: false,
        alignItemPlacementWatchdog: false,
        placementInputRevision: 0,
        optionRegistrations: new Map(),
        optionClickMetadata: new WeakMap(),
        observedOptions: [],
        optionObserver: null
    });
}

export function disposeRoot(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        clearTimeout(rootState.typeaheadTimer);
        clearInterval(rootState.continuousScrollInterval);
        cleanupTransitionState(rootState);
        rootState.optionObserver?.disconnect();
        for (const [element, registration] of rootState.optionRegistrations) {
            element.removeEventListener('click', registration.clickHandler, true);
        }
        rootState.optionRegistrations.clear();
        removeScrollListener(rootState);
        if (rootState.scrollLockCleanup) {
            rootState.scrollLockCleanup();
        }
        if (rootState.triggerCleanup) {
            rootState.triggerCleanup();
            rootState.triggerCleanup = null;
        }
        if (rootState.scrollUpArrow) {
            disposeScrollArrow(rootId, 'up');
        }
        if (rootState.scrollDownArrow) {
            disposeScrollArrow(rootId, 'down');
        }
        if (rootState.popup) {
            disposePopup(rootId);
        }
        state.roots.delete(rootId);
    }
}

export function renameRoot(previousRootId, nextRootId) {
    if (!previousRootId || !nextRootId || previousRootId === nextRootId) return;
    const rootState = state.roots.get(previousRootId);
    if (!rootState) return;
    state.roots.delete(previousRootId);
    rootState.rootId = nextRootId;
    state.roots.set(nextRootId, rootState);
    for (const registration of state.positioners.values()) {
        if (registration.rootId === previousRootId) {
            registration.rootId = nextRootId;
        }
    }
}

export function setRootOpen(rootId, isOpen, reason, selectedIndex = -1) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    const wasOpen = rootState.isOpen;
    rootState.isOpen = isOpen;
    rootState.selectedIndex = Number.isInteger(selectedIndex) ? selectedIndex : -1;

    if (isOpen && !wasOpen) {
        rootState.openRevision += 1;
        rootState.positionerReadyElement = null;
        rootState.positionerReadyRevision = -1;
        rootState.positionerElement?.removeAttribute('data-positioned');

        if (
            rootState.pendingPositionerReadyElement === rootState.positionerElement &&
            rootState.pendingPositionerReadyRevision === rootState.openRevision
        ) {
            rootState.positionerReadyElement = rootState.positionerElement;
            rootState.positionerReadyRevision = rootState.openRevision;
        } else if (
            rootState.openRevision === 1 &&
            rootState.positionerElement &&
            orphanPositionerReadyElements.has(rootState.positionerElement)
        ) {
            rootState.positionerReadyElement = rootState.positionerElement;
            rootState.positionerReadyRevision = rootState.openRevision;
            orphanPositionerReadyElements.delete(rootState.positionerElement);
        }
        rootState.pendingPositionerReadyElement = null;
        rootState.pendingPositionerReadyRevision = -1;
        rootState.acceptNextOpenPositioning = false;
        if (rootState.popup) rootState.popup.standardFallbackPending = false;

        queueOpenAlignItemPlacement(rootId);

        // Scroll lock is owned exclusively by SelectPositioner (C#-side) to cover
        // both modal and alignItemWithTrigger cases, and to avoid double-locking.

        function waitForPopupAndFocus() {
            let attempts = 0;
            const maxAttempts = 60;

            function check() {
                attempts++;
                const renderedList = rootState.popupElement?.querySelector('[role="listbox"]');
                const registeredList = rootState.listElement?.isConnected ? rootState.listElement : null;
                const containerEl = registeredList || renderedList || rootState.popupElement;

                if (
                    containerEl &&
                    document.contains(containerEl) &&
                    rootState.popupElement?.hasAttribute('data-open') &&
                    containerEl.clientHeight > 0
                ) {
                    const items = getNavigableItems(containerEl);
                    if (items.length > 0) {
                        let targetIndex = rootState.selectedIndex;
                        if (targetIndex < 0 || targetIndex >= items.length) {
                            targetIndex = -1;
                            for (let i = 0; i < items.length; i++) {
                                if (items[i].getAttribute('aria-selected') === 'true') {
                                    targetIndex = i;
                                    break;
                                }
                            }
                        }
                        if (targetIndex === -1) {
                            targetIndex = findNextEnabledIndex(items, -1, 1, false);
                        }
                        if (targetIndex >= 0) {
                            setActiveItem(rootState, items, targetIndex);
                        }
                        attachScrollListener(rootState);
                        notifyScrollArrowVisibility(rootState);
                        return;
                    }
                }

                if (attempts < maxAttempts && rootState.isOpen) {
                    requestAnimationFrame(check);
                }
            }

            requestAnimationFrame(check);
        }

        waitForPopupAndFocus();

        function setupOpenTransition() {
            const popupEl = rootState.listElement || rootState.popupElement;
            const hasTransition = popupEl ? checkForTransitionOrAnimation(popupEl) : false;

            // Double-RAF ensures the browser paints the starting styles first,
            // then observes the attribute change that triggers the opening CSS transition.
            requestAnimationFrame(() => {
                requestAnimationFrame(() => {
                    if (!rootState.isOpen) return;

                    if (hasTransition) {
                        setupTransitionEndListener(rootState, true);
                    } else if (rootState.dotNetRef) {
                        rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', true).catch(() => { });
                    }
                    if (rootState.dotNetRef) {
                        rootState.dotNetRef.invokeMethodAsync('OnStartingStyleApplied').catch(() => { });
                    }
                });
            });
        }

        requestAnimationFrame(() => {
            if (rootState.isOpen) {
                setupOpenTransition();
            }
        });
    } else {
        rootState.alignItemPlacementWatchdog = false;

        // Safety net: release any legacy scroll lock that may have been set prior to
        // the positioner taking exclusive ownership. New code paths never set this.
        if (rootState.scrollLockCleanup) {
            rootState.scrollLockCleanup();
            rootState.scrollLockCleanup = null;
        }

        clearInterval(rootState.continuousScrollInterval);
        rootState.continuousScrollInterval = null;
        removeScrollListener(rootState);

        rootState.activeIndex = -1;

        const popupEl = rootState.listElement || rootState.popupElement;
        if (popupEl && checkForTransitionOrAnimation(popupEl)) {
            setupTransitionEndListener(rootState, false);
        } else if (rootState.dotNetRef && !rootState.isOpen) {
            rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', false).catch(() => { });
        }
    }
}

export function setTriggerElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    const previousElement = rootState.triggerElement;
    const triggerChanged = previousElement !== element;

    if (triggerChanged && rootState.triggerCleanup) {
        rootState.triggerCleanup();
        rootState.triggerCleanup = null;
    }

    rootState.triggerElement = element;
    if (triggerChanged) {
        invalidateAlignItemPlacement(rootState);
    }

    if (element && rootState.dotNetRef && (!rootState.triggerCleanup || triggerChanged)) {
        initializeTrigger(rootId, element, rootState.dotNetRef);
    }
    queueOpenAlignItemPlacement(rootId);
}

export function setPopupElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        if (rootState.popupElement !== element && !rootState.listElement) {
            rootState.optionObserver?.disconnect();
            rootState.optionObserver = null;
        }
        if (rootState.popupElement !== element) {
            rootState.popupElement = element;
            invalidateAlignItemPlacement(rootState);
        }
        ensureOptionObserver(rootState);
        queueOpenAlignItemPlacement(rootId);
    }
}

export function setListElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    const previous = rootState.listElement;
    if (previous !== element) {
        rootState.optionObserver?.disconnect();
        rootState.optionObserver = null;
    }
    rootState.listElement = element;
    if (previous !== element) {
        invalidateAlignItemPlacement(rootState);
    }
    ensureOptionObserver(rootState);

    // If the scroll listener is currently attached to a stale target (e.g. the
    // popup before the list registered), re-point it at `listElement || popup`
    // so scroll-arrow visibility tracks the correct scroll container. Mirrors
    // the React `scrollHandlerRef.current?.(event.currentTarget)` bridge that
    // is wired via onScroll on SelectList itself.
    if (previous === element) return;
    queueOpenAlignItemPlacement(rootId);
    if (rootState.isOpen && element) {
        requestAnimationFrame(() => {
            if (!rootState.isOpen || rootState.listElement !== element) return;
            const items = getNavigableItems(element);
            const selectedIndex = items.findIndex(item => item.getAttribute('aria-selected') === 'true');
            const targetIndex = selectedIndex >= 0 ? selectedIndex : rootState.selectedIndex;
            if (targetIndex >= 0 && targetIndex < items.length) {
                setActiveItem(rootState, items, targetIndex);
            }
        });
    }
    if (rootState.scrollListener) {
        attachScrollListener(rootState);
        notifyScrollArrowVisibility(rootState);
    }
}

export function registerPositioner(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        if (
            rootState.isOpen &&
            rootState.openRevision === 1 &&
            orphanPositionerReadyElements.has(element)
        ) {
            rootState.positionerReadyElement = element;
            rootState.positionerReadyRevision = rootState.openRevision;
            orphanPositionerReadyElements.delete(element);
        } else if (rootState.positionerElement !== element) {
            rootState.positionerReadyElement = null;
            rootState.positionerReadyRevision = -1;
        }
        if (rootState.positionerElement !== element) {
            rootState.positionerElement = element;
            invalidateAlignItemPlacement(rootState);
        }
        queueOpenAlignItemPlacement(rootId);
    }
}

export function unregisterPositioner(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.positionerElement = null;
        rootState.positionerReadyElement = null;
        rootState.positionerReadyRevision = -1;
        rootState.pendingPositionerReadyElement = null;
        rootState.pendingPositionerReadyRevision = -1;
    }
}

/**
 * Wires trigger-side DOM listeners that the React source handles inline on the
 * button (pointermove, mousedown→mouseup-outside-bounds cancel-open, focusout
 * with relatedTarget containment). Keeping this in JS avoids one interop
 * round-trip per pointermove and matches the project's "DOM-heavy logic stays
 * in JS" guidance.
 */
export function initializeTrigger(rootId, triggerElement, triggerDotNetRef) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !triggerElement) return;

    if (rootState.triggerCleanup) {
        rootState.triggerCleanup();
        rootState.triggerCleanup = null;
    }

    rootState.triggerDotNetRef = triggerDotNetRef;

    const onPointerMove = () => {
        triggerDotNetRef.invokeMethodAsync('NotifyPointerMove').catch(() => { });
    };

    const onPointerDown = (event) => {
        setPointerInteraction(rootState, normalizeInteractionType(event.pointerType), event);
        if (event.pointerType === 'touch') {
            triggerDotNetRef.invokeMethodAsync('NotifyTouchOpen').catch(() => { });
        }
    };

    const onMouseDown = (event) => {
        if (event.button !== 0) return;

        const notifyCancelOpen = () => {
            const dotNetRef = rootState.triggerDotNetRef || triggerDotNetRef;
            dotNetRef?.invokeMethodAsync('NotifyCancelOpen').catch(() => { });
        };

        // Firefox can fire `mouseup` synchronously with `mousedown`; defer the
        // one-shot listener registration by a tick to match the React source.
        setTimeout(() => {
            const doc = triggerElement.ownerDocument;
            const handler = (mu) => {
                const currentTrigger = rootState.triggerElement || triggerElement;
                if (!currentTrigger || !currentTrigger.isConnected) {
                    notifyCancelOpen();
                    return;
                }

                const tgt = mu.target;

                if (contains(currentTrigger, tgt) || tgt === currentTrigger) return;
                if (rootState.positionerElement && contains(rootState.positionerElement, tgt)) return;

                const b = getPseudoElementBounds(currentTrigger);
                if (mu.clientX >= b.left - BOUNDARY_OFFSET &&
                    mu.clientX <= b.right + BOUNDARY_OFFSET &&
                    mu.clientY >= b.top - BOUNDARY_OFFSET &&
                    mu.clientY <= b.bottom + BOUNDARY_OFFSET) {
                    return;
                }

                notifyCancelOpen();
            };
            doc.addEventListener('mouseup', handler, { once: true });
        }, 0);
    };

    const onFocusOut = (e) => {
        if (rootState.positionerElement && contains(rootState.positionerElement, e.relatedTarget)) {
            return;
        }
        triggerDotNetRef.invokeMethodAsync('NotifyRealBlur').catch(() => { });
    };

    triggerElement.addEventListener('pointermove', onPointerMove);
    triggerElement.addEventListener('pointerdown', onPointerDown);
    triggerElement.addEventListener('mousedown', onMouseDown, true);
    triggerElement.addEventListener('focusout', onFocusOut);

    rootState.triggerCleanup = () => {
        triggerElement.removeEventListener('pointermove', onPointerMove);
        triggerElement.removeEventListener('pointerdown', onPointerDown);
        triggerElement.removeEventListener('mousedown', onMouseDown, true);
        triggerElement.removeEventListener('focusout', onFocusOut);
    };
}

export function getLastInteractionType(rootId) {
    const rootState = state.roots.get(rootId);
    return rootState?.lastInteractionType || 'none';
}

export function disposeTrigger(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;
    if (rootState.triggerCleanup) {
        rootState.triggerCleanup();
        rootState.triggerCleanup = null;
    }
    rootState.triggerDotNetRef = null;
}

export function setActiveIndex(rootId, index) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.activeIndex = index;
    }
}

export function setReadOnly(rootId, readOnly) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.readOnly = !!readOnly;
    }
}

export function setDisabled(rootId, disabled) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.disabled = !!disabled;
    }
}

export function setDirection(rootId, direction) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.direction = direction || 'ltr';
    }
}

export function focusTrigger(element) {
    if (element) element.focus();
}

export function isKeyboardActive(rootId) {
    const rootState = state.roots.get(rootId);
    return rootState?.keyboardActive ?? false;
}

export function clearHighlights(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    const containerEl = rootState.listElement || rootState.popupElement;
    if (!containerEl) return;

    const items = containerEl.querySelectorAll('[role="option"]');
    items.forEach(item => {
        item.removeAttribute('data-highlighted');
        item.setAttribute('tabindex', '-1');
    });

    rootState.activeIndex = -1;
}

// ─── Scroll Arrow Helpers ─────────────────────────────────────────────

function checkScrollArrows(rootState) {
    const containerEl = rootState.listElement || rootState.popupElement;
    if (!containerEl) return { up: false, down: false };

    const maxScrollTop = getMaxScrollTop(containerEl);
    const scrollTop = normalizeScrollOffset(containerEl.scrollTop, maxScrollTop);
    return {
        up: scrollTop > 0,
        down: scrollTop < maxScrollTop
    };
}

function notifyScrollArrowVisibility(rootState) {
    if (!rootState.dotNetRef) return;

    const visibility = checkScrollArrows(rootState);
    rootState.dotNetRef.invokeMethodAsync('OnScrollArrowVisibilityChange', visibility.up, visibility.down).catch(() => { });
}

function attachScrollListener(rootState) {
    removeScrollListener(rootState);

    const containerEl = rootState.listElement || rootState.popupElement;
    if (!containerEl) return;

    const handler = () => handlePopupScrollInternal(rootState, containerEl);
    containerEl.addEventListener('scroll', handler, { passive: true });
    rootState.scrollListener = { element: containerEl, handler };
}

function removeScrollListener(rootState) {
    if (rootState.scrollListener) {
        rootState.scrollListener.element.removeEventListener('scroll', rootState.scrollListener.handler);
        rootState.scrollListener = null;
    }
}

export function startContinuousScroll(rootId, direction) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    clearInterval(rootState.continuousScrollInterval);

    const containerEl = rootState.listElement || rootState.popupElement;
    if (!containerEl) return;

    const scrollStep = () => {
        const items = getNavigableItems(containerEl);
        if (items.length === 0) return;

        if (direction === 'up') {
            const scrollTop = containerEl.scrollTop;
            let targetItem = null;
            for (let i = 0; i < items.length; i++) {
                if (items[i].offsetTop >= scrollTop) {
                    targetItem = items[Math.max(0, i - 1)];
                    break;
                }
            }
            if (targetItem) {
                containerEl.scrollTop = targetItem.offsetTop;
            } else {
                containerEl.scrollTop = 0;
            }
        } else {
            const scrollBottom = containerEl.scrollTop + containerEl.clientHeight;
            let targetItem = null;
            for (let i = items.length - 1; i >= 0; i--) {
                const itemBottom = items[i].offsetTop + items[i].offsetHeight;
                if (itemBottom <= scrollBottom) {
                    targetItem = items[Math.min(items.length - 1, i + 1)];
                    break;
                }
            }
            if (targetItem) {
                containerEl.scrollTop = targetItem.offsetTop + targetItem.offsetHeight - containerEl.clientHeight;
            } else {
                containerEl.scrollTop = containerEl.scrollHeight - containerEl.clientHeight;
            }
        }

        const visibility = checkScrollArrows(rootState);
        if ((direction === 'up' && !visibility.up) || (direction === 'down' && !visibility.down)) {
            clearInterval(rootState.continuousScrollInterval);
            rootState.continuousScrollInterval = null;
        }
    };

    scrollStep();
    rootState.continuousScrollInterval = setInterval(scrollStep, 40);
}

export function stopContinuousScroll(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    clearInterval(rootState.continuousScrollInterval);
    rootState.continuousScrollInterval = null;
}

export function initScrollArrow(rootId, arrowElement, direction) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !arrowElement) return;

    const isUp = direction === 'up';
    const arrowKey = isUp ? 'scrollUpArrow' : 'scrollDownArrow';

    // Clean up any prior init for this direction.
    disposeScrollArrow(rootId, direction);

    rootState[arrowKey] = {
        element: arrowElement,
        timeoutId: null,
        mousemoveHandler: null,
        mouseleaveHandler: null
    };

    const arrowState = rootState[arrowKey];

    function clearScrollTimeout() {
        if (arrowState.timeoutId != null) {
            clearTimeout(arrowState.timeoutId);
            arrowState.timeoutId = null;
        }
    }

    function resetActiveIndex() {
        rootState.activeIndex = -1;
        rootState.dotNetRef?.invokeMethodAsync('OnActiveIndexChange', -1).catch(() => { });
    }

    function scrollNextItem() {
        const scroller = rootState.listElement || rootState.popupElement;
        if (!scroller) return;

        resetActiveIndex();

        notifyScrollArrowVisibility(rootState);

        const maxScrollTop = getMaxScrollTop(scroller);
        const scrollTop = normalizeScrollOffset(scroller.scrollTop, maxScrollTop);
        const isScrolledToEdge = scrollTop === (isUp ? 0 : maxScrollTop);

        if (scrollTop !== scroller.scrollTop) {
            scroller.scrollTop = scrollTop;
        }

        const items = getNavigableItems(scroller);

        // Empty items fallback: pixel-based scroll.
        if (items.length === 0) {
            if (isScrolledToEdge) {
                clearScrollTimeout();
                return;
            }
            scroller.scrollTop = isUp
                ? Math.max(0, scroller.scrollTop - 40)
                : Math.min(maxScrollTop, scroller.scrollTop + 40);
            arrowState.timeoutId = setTimeout(scrollNextItem, 40);
            return;
        }

        if (isScrolledToEdge) {
            clearScrollTimeout();
            return;
        }

        // Use arrow height compensation in scroll calculations.
        const scrollArrowHeight = arrowState.element?.offsetHeight || 0;
        scroller.scrollTop = getTargetScrollTop(
            items, isUp, scrollTop, scroller.clientHeight,
            scrollArrowHeight, maxScrollTop
        );

        arrowState.timeoutId = setTimeout(scrollNextItem, 40);
    }

    // mousemove handler with React movementX/Y guard and timeout-started check.
    arrowState.mousemoveHandler = function (event) {
        if ((event.movementX === 0 && event.movementY === 0) || arrowState.timeoutId != null) {
            return;
        }

        resetActiveIndex();
        arrowState.timeoutId = setTimeout(scrollNextItem, 40);
    };

    arrowState.mouseleaveHandler = function () {
        clearScrollTimeout();
    };

    arrowElement.addEventListener('mousemove', arrowState.mousemoveHandler);
    arrowElement.addEventListener('mouseleave', arrowState.mouseleaveHandler);
}

export function disposeScrollArrow(rootId, direction) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    const isUp = direction === 'up';
    const arrowKey = isUp ? 'scrollUpArrow' : 'scrollDownArrow';
    const arrowState = rootState[arrowKey];
    if (!arrowState) return;

    if (arrowState.timeoutId != null) {
        clearTimeout(arrowState.timeoutId);
    }

    const el = arrowState.element;
    if (el) {
        if (arrowState.mousemoveHandler) el.removeEventListener('mousemove', arrowState.mousemoveHandler);
        if (arrowState.mouseleaveHandler) el.removeEventListener('mouseleave', arrowState.mouseleaveHandler);
    }

    rootState[arrowKey] = null;
}

// ─── Positioner API ───────────────────────────────────────────────────

export async function initializePositioner(positionerElement, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, disableAnchorTracking, collisionAvoidance, alignItemWithTrigger, dotNetRef, rootId, hasSideOffsetFn, hasAlignOffsetFn, virtualAnchor, controlledDisableAnchorTracking = false) {
    const initialRootState = rootId ? state.roots.get(rootId) : null;
    if (initialRootState?.positionerElement === positionerElement) {
        initialRootState.positionerReadyElement = null;
        initialRootState.positionerReadyRevision = -1;
        positionerElement.removeAttribute('data-positioned');
    }

    const registration = {
        virtualId: null,
        rootId,
        positionerElement,
        alignItemWithTriggerActive: !!alignItemWithTrigger,
        disableAnchorTracking: !!controlledDisableAnchorTracking,
        sideObserver: null
    };
    let onPositionUpdated = null;
    if (dotNetRef) {
        onPositionUpdated = (effectiveSide, effectiveAlign, anchorHidden, arrowUncentered) => {
            // Root initialization and positioner initialization can complete in
            // either order under WASM/default-open rendering. Resolve the live
            // root at callback time rather than capturing a missing root.
            const currentRootId = registration.rootId;
            const currentRootState = currentRootId ? state.roots.get(currentRootId) : null;
            if (
                currentRootState?.isOpen &&
                currentRootState.positionerElement === positionerElement
            ) {
                currentRootState.positionerReadyElement = positionerElement;
                currentRootState.positionerReadyRevision = currentRootState.openRevision;
            } else if (
                currentRootState &&
                !currentRootState.isOpen &&
                currentRootState.positionerElement === positionerElement &&
                currentRootState.acceptNextOpenPositioning &&
                registration.alignItemWithTriggerActive
            ) {
                // Controlled parameter changes can render descendants before
                // the root's asynchronous setRootOpen interop completes. This
                // fresh pass belongs to the immediately upcoming open revision.
                currentRootState.pendingPositionerReadyElement = positionerElement;
                currentRootState.pendingPositionerReadyRevision = currentRootState.openRevision + 1;
            } else if (
                !currentRootState?.positionerElement ||
                (!currentRootState.isOpen &&
                    currentRootState.openRevision === 0 &&
                    currentRootState.positionerElement === positionerElement)
            ) {
                // Child initialization can finish before the root has received
                // its RenderElement reference or before the first setRootOpen.
                // Preserve only that initial registration-order result; a
                // different non-null element is a stale callback.
                orphanPositionerReadyElements.add(positionerElement);
            }

            if (registration.alignItemWithTriggerActive) {
                positionerElement.setAttribute('data-side', 'none');
            }

            dotNetRef.invokeMethodAsync('OnPositionUpdated', effectiveSide, effectiveAlign, anchorHidden, arrowUncentered).catch(() => { });

            // When align-item-with-trigger is active, immediately drive the
            // popup-level align-item commit from JS. floating.js's
            // updatePositionInternal deliberately skipped writing
            // `data-positioned` (so the FOUC CSS keeps the popup invisible),
            // so the align-item commit is the sole owner of releasing the
            // hide. Calling it here — synchronously in the same JS frame as
            // Floating UI's pass — ensures the popup becomes visible at the
            // correct placement on the very next paint, without depending on
            // the C# round-trip that flips PositionerContext.IsPositioned.
            // On Server (InteractiveServer/SSR) that round-trip costs at least
            // one SignalR hop and can stall on slow circuits, leaving the
            // popup invisible indefinitely.
            //
            // The C# Popup-side gate at SelectPopup.razor still runs on later
            // re-renders (e.g., once items mount and the placement should
            // refine), so this JS-side invocation is purely the "first paint"
            // accelerator, not a replacement for the gate.
            if (
                registration.alignItemWithTriggerActive &&
                currentRootId &&
                currentRootState?.isOpen &&
                currentRootState.positionerElement === positionerElement &&
                currentRootState.positionerReadyRevision === currentRootState.openRevision
            ) {
                try { beginAlignItemWithTriggerPlacement(currentRootId, true); } catch { /* idempotent */ }
            }
        };
    }

    const virtualElement = virtualAnchor
        ? createVirtualElement(virtualAnchor.x, virtualAnchor.y, virtualAnchor.width, virtualAnchor.height)
        : null;
    registration.virtualId = virtualElement?.virtualId ?? null;
    const positionerId = await floatingInitializePositioner({
        positionerElement,
        triggerElement: virtualElement ? null : triggerElement,
        virtualId: virtualElement?.virtualId ?? null,
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
        collisionAvoidance: collisionAvoidance || 'flip-shift',
        // Forward to floating so updatePositionInternal skips visual style writes
        // and the data-positioned attribute (the align-item commit owns those).
        alignItemWithTriggerActive: !!alignItemWithTrigger,
        onPositionUpdated,
        dotNetRef: dotNetRef || null,
        hasSideOffsetFn: !!hasSideOffsetFn,
        hasAlignOffsetFn: !!hasAlignOffsetFn
    });

    // alignItemWithTrigger placement is invoked from SelectPopup via
    // `beginAlignItemWithTriggerPlacement` so the popup-level layout pass
    // can read selectedItemText / valueElement refs.
    if (positionerId) {
        registration.sideObserver = new MutationObserver(() => {
            if (
                registration.alignItemWithTriggerActive &&
                positionerElement.getAttribute('data-side') !== 'none'
            ) {
                positionerElement.setAttribute('data-side', 'none');
            }
        });
        registration.sideObserver.observe(positionerElement, {
            attributes: true,
            attributeFilter: ['data-side']
        });
        if (registration.alignItemWithTriggerActive) {
            positionerElement.setAttribute('data-side', 'none');
        }
        state.positioners.set(positionerId, registration);
    }
    return positionerId;
}

export async function updatePosition(positionerId, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, collisionAvoidance, alignItemWithTrigger, hasSideOffsetFn, hasAlignOffsetFn, virtualAnchor, controlledDisableAnchorTracking = false) {
    const registration = state.positioners.get(positionerId);
    if (registration) {
        registration.alignItemWithTriggerActive = !!alignItemWithTrigger;
        registration.disableAnchorTracking = !!controlledDisableAnchorTracking;
    }
    if (registration?.virtualId && virtualAnchor) {
        updateVirtualElement(registration.virtualId, virtualAnchor.x, virtualAnchor.y, virtualAnchor.width, virtualAnchor.height);
    }
    await floatingUpdatePositioner(positionerId, {
        ...(registration?.virtualId ? {} : { triggerElement }),
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
        collisionAvoidance: collisionAvoidance || 'flip-shift',
        alignItemWithTriggerActive: !!alignItemWithTrigger,
        disableAnchorTracking: !!controlledDisableAnchorTracking,
        hasSideOffsetFn: !!hasSideOffsetFn,
        hasAlignOffsetFn: !!hasAlignOffsetFn
    });

    // alignItemWithTrigger placement is invoked from SelectPopup, not here,
    // because it needs the popup-level selectedItemText / valueElement refs.
}

export function resetPositionerForOpen(positionerId, rootId) {
    const rootState = rootId ? state.roots.get(rootId) : null;
    if (rootState) {
        rootState.positionerReadyElement = null;
        rootState.positionerReadyRevision = -1;
        rootState.pendingPositionerReadyElement = null;
        rootState.pendingPositionerReadyRevision = -1;
        rootState.acceptNextOpenPositioning = !rootState.isOpen;
        rootState.positionerElement?.removeAttribute('data-positioned');
    }

    floatingResetPositioner(positionerId);
}

export function disposePositioner(positionerId) {
    const registration = state.positioners.get(positionerId);
    if (registration?.virtualId) disposeVirtualElement(registration.virtualId);
    registration?.sideObserver?.disconnect();
    state.positioners.delete(positionerId);
    floatingDisposePositioner(positionerId);
}

// ─── Popup Placement Public API ───────────────────────────────────────

export function initializePopup(rootId, popupElement, dotNetRef, finalFocusManaged = false) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !popupElement) return;

    const popupState = ensurePopupState(rootState);

    // Idempotent: re-registration replaces handlers but keeps saved styles / flags.
    if (popupState.pointerLeaveHandler && popupState.popupElement) {
        popupState.popupElement.removeEventListener('pointerleave', popupState.pointerLeaveHandler);
        popupState.popupElement.removeEventListener('pointerdown', popupState.pointerDownHandler);
        popupState.popupElement.removeEventListener('touchstart', popupState.touchStartHandler);
        popupState.popupElement.removeEventListener('keydown', popupState.keyDownHandler);
        popupState.popupElement.removeEventListener('click', popupState.clickHandler, true);
        popupState.popupElement.removeEventListener('mousemove', popupState.mouseMoveHandler);
        popupState.popupElement.removeEventListener('scroll', popupState.scrollHandler);
    }

    if (rootState.popupElement !== popupElement) {
        rootState.popupElement = popupElement;
        invalidateAlignItemPlacement(rootState);
    }
    rootState.finalFocusManaged = !!finalFocusManaged;
    popupState.popupElement = popupElement;
    popupState.dotNetRef = dotNetRef;
    queueOpenAlignItemPlacement(rootId);

    popupState.clickHandler = (event) => {
        const option = event.target?.closest?.('[role="option"]');
        if (!option || !popupElement.contains(option)) return;
        rootState.optionClickMetadata.set(option, {
            isVirtualClick: isVirtualClickEvent(event),
            pointerTypeDefined: event.pointerType !== undefined
        });
    };
    popupElement.addEventListener('click', popupState.clickHandler, true);

    popupState.pointerLeaveHandler = (event) => {
        const dotNet = popupState.dotNetRef;
        if (!dotNet) return;
        if (!rootState.highlightItemOnHover) return;
        if (isMouseWithinBounds(event)) return;
        if (event.pointerType === 'touch') return;

        const popup = event.currentTarget;
        if (popupState.pointerLeaveTimer !== null) {
            clearTimeout(popupState.pointerLeaveTimer);
        }
        popupState.pointerLeaveTimer = setTimeout(() => {
            popupState.pointerLeaveTimer = null;
            dotNet.invokeMethodAsync('OnPopupPointerLeave').catch(() => { });
            try { popup.focus({ preventScroll: true }); } catch { /* ignore */ }
        }, 0);
    };

    popupState.pointerDownHandler = (event) => {
        setPointerInteraction(rootState, normalizeInteractionType(event.pointerType), event);
    };

    popupState.touchStartHandler = (event) => {
        setPointerInteraction(rootState, 'touch', event);
    };

    popupState.keyDownHandler = (event) => {
        rootState.keyboardActive = true;
        rootState.lastInteractionType = 'keyboard';
        if (rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnKeyboardActiveChange', true).catch(() => { });
        }

        const inToolbar = popupElement.closest && popupElement.closest('[role="toolbar"]');
        if (inToolbar) {
            const compositeKeys = new Set([
                'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight',
                'Home', 'End', 'PageUp', 'PageDown', 'Enter', ' '
            ]);
            if (compositeKeys.has(event.key)) {
                event.stopPropagation();
            }
        }
    };

    popupState.mouseMoveHandler = () => {
        if (rootState.keyboardActive) {
            rootState.keyboardActive = false;
            if (rootState.dotNetRef) {
                rootState.dotNetRef.invokeMethodAsync('OnKeyboardActiveChange', false).catch(() => { });
            }
        }
    };

    popupState.scrollHandler = (event) => {
        if (rootState.listElement) {
            // List owns the scroll container; this scroll is not ours.
            return;
        }
        handlePopupScrollInternal(rootState, event.currentTarget);
    };

    popupElement.addEventListener('pointerleave', popupState.pointerLeaveHandler);
    popupElement.addEventListener('pointerdown', popupState.pointerDownHandler);
    popupElement.addEventListener('touchstart', popupState.touchStartHandler, { passive: true });
    popupElement.addEventListener('keydown', popupState.keyDownHandler);
    popupElement.addEventListener('mousemove', popupState.mouseMoveHandler);
    popupElement.addEventListener('scroll', popupState.scrollHandler, { passive: true });
}

export function setFinalFocusManaged(rootId, managed) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.finalFocusManaged = !!managed;
    }
}

export function focusElement(rootId, element) {
    if (!(element instanceof HTMLElement)) return;

    const rootState = state.roots.get(rootId);
    const active = document.activeElement;
    if (rootState && active && active !== document.body && active !== rootState.triggerElement) {
        const popup = rootState.popupElement;
        if (popup && !popup.contains(active)) return;
    }

    let attempts = 0;
    const maxAttempts = 5;

    const applyFocus = () => {
        attempts++;
        if (document.contains(element)) {
            element.focus();
            if (document.activeElement === element || attempts >= maxAttempts) return;
        }

        if (attempts < maxAttempts) {
            requestAnimationFrame(applyFocus);
        }
    };

    requestAnimationFrame(applyFocus);
}

export function disposePopup(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !rootState.popup) return;

    const popupState = rootState.popup;

    if (popupState.pointerLeaveTimer !== null) {
        clearTimeout(popupState.pointerLeaveTimer);
        popupState.pointerLeaveTimer = null;
    }

    if (popupState.popupElement) {
        if (popupState.pointerLeaveHandler) {
            popupState.popupElement.removeEventListener('pointerleave', popupState.pointerLeaveHandler);
        }
        if (popupState.pointerDownHandler) {
            popupState.popupElement.removeEventListener('pointerdown', popupState.pointerDownHandler);
        }
        if (popupState.touchStartHandler) {
            popupState.popupElement.removeEventListener('touchstart', popupState.touchStartHandler);
        }
        if (popupState.keyDownHandler) {
            popupState.popupElement.removeEventListener('keydown', popupState.keyDownHandler);
        }
        if (popupState.clickHandler) {
            popupState.popupElement.removeEventListener('click', popupState.clickHandler, true);
        }
        if (popupState.mouseMoveHandler) {
            popupState.popupElement.removeEventListener('mousemove', popupState.mouseMoveHandler);
        }
        if (popupState.scrollHandler) {
            popupState.popupElement.removeEventListener('scroll', popupState.scrollHandler);
        }
    }

    if (popupState.resizeHandler) {
        try {
            const win = popupState.popupElement && popupState.popupElement.ownerDocument.defaultView;
            if (win) win.removeEventListener('resize', popupState.resizeHandler);
        } catch { /* ignore */ }
        popupState.resizeHandler = null;
    }

    if (popupState.scrollArrowRaf) {
        cancelAnimationFrame(popupState.scrollArrowRaf);
        popupState.scrollArrowRaf = 0;
    }

    clearPopupStylesInternal(rootState);

    popupState.popupElement = null;
    popupState.dotNetRef = null;
    popupState.pointerLeaveHandler = null;
    popupState.pointerDownHandler = null;
    popupState.touchStartHandler = null;
    popupState.keyDownHandler = null;
    popupState.clickHandler = null;
    popupState.mouseMoveHandler = null;
    popupState.scrollHandler = null;
    popupState.alignItemWithTriggerActive = false;
    popupState.standardFallbackPending = false;
    popupState.initialPlaced = false;
    popupState.reachedMaxHeight = false;
    popupState.savedPositionerStyles = false;
    popupState.originalPositionerStyles = {};
    rootState.finalFocusManaged = false;

    rootState.popup = null;
}

export function beginAlignItemWithTriggerPlacement(rootId, alignItemWithTriggerActive) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;
    const popupState = ensurePopupState(rootState);

    if (alignItemWithTriggerActive && popupState.standardFallbackPending) return;

    popupState.alignItemWithTriggerActive = !!alignItemWithTriggerActive;

    const popupElement = popupState.popupElement || rootState.popupElement;
    const positionerElement = rootState.positionerElement;
    const triggerElement = rootState.triggerElement;
    if (popupElement && !popupState.popupElement) {
        popupState.popupElement = popupElement;
    }

    if (!rootState.isOpen || !popupElement || !positionerElement || !triggerElement) {
        scheduleOpenAlignItemPlacement(rootId);
        return;
    }

    // A rendered SelectList can exist one lifecycle turn before its element
    // reference reaches JS. Measuring the popup as the scroller during that
    // gap exposes the full intrinsic list height, then visibly shrinks it when
    // the real constrained list registers. React's layout effect already has
    // the list ref before it releases the popup.
    const renderedList = popupElement.querySelector('[role="listbox"]');
    if (renderedList && rootState.listElement !== renderedList) {
        scheduleOpenAlignItemPlacement(rootId);
        return;
    }
    if (
        popupElement.querySelector('[role="option"]') &&
        !rootState.selectedItemTextElement?.isConnected
    ) {
        scheduleOpenAlignItemPlacement(rootId);
        return;
    }

    // React waits for useFloating's first positioned result before measuring
    // the align-item popup. Nonzero DOM geometry is not sufficient: before the
    // size middleware runs the popup still has its intrinsic width/height and
    // --anchor-width/--available-height are unresolved. Releasing the FOUC
    // hide at that point exposes a narrow, fully populated popup for a frame.
    if (
        popupState.alignItemWithTriggerActive &&
        (rootState.positionerReadyElement !== positionerElement ||
            rootState.positionerReadyRevision !== rootState.openRevision)
    ) {
        scheduleOpenAlignItemPlacement(rootId);
        return;
    }

    if (popupState.alignItemWithTriggerActive) {
        positionerElement.setAttribute('data-side', 'none');
    }

    saveOriginalPositionerStyles(popupState, positionerElement);
    popupState.initialPlaced = true;
    popupElement.style.removeProperty('--transform-origin');

    if (!popupState.alignItemWithTriggerActive) {
        popupState.initialPlaced = true;
        notifyScrollArrowVisibility(rootState);
        return;
    }

    const placementRevision = rootState.openRevision;
    const placementInputRevision = rootState.placementInputRevision;
    if (
        popupState.placementCommittedRevision === placementRevision &&
        popupState.placementCommittedInputRevision === placementInputRevision
    ) {
        // Floating may transiently clear its visibility token while processing
        // an update whose inputs are unchanged. The committed align geometry
        // remains valid, so preserve visibility instead of hiding and later
        // falling back to a differently sized standard layout.
        positionerElement.setAttribute('data-positioned', '');
        return;
    }
    if (
        popupState.placementInProgressRevision === placementRevision &&
        popupState.placementInProgressInputRevision === placementInputRevision
    ) return;

    popupState.placementInProgressRevision = placementRevision;
    popupState.placementInProgressInputRevision = placementInputRevision;

    const restoreTransformStyles = unsetTransformStyles(popupElement);

    try {
            if (!rootState.isOpen || rootState.openRevision !== placementRevision) return;
            if (rootState.placementInputRevision !== placementInputRevision) return;
            if (!popupState.alignItemWithTriggerActive || popupState.standardFallbackPending) return;
            if (!popupState.popupElement || !rootState.positionerElement) return;

            const positionerStyles = getComputedStyle(positionerElement);
            const popupStyles = getComputedStyle(popupElement);
            const doc = triggerElement.ownerDocument;
            const win = positionerElement.ownerDocument.defaultView;

            const scale = getScale(triggerElement);
            const triggerRect = normalizeRect(triggerElement.getBoundingClientRect(), scale);
            const positionerRect = normalizeRect(positionerElement.getBoundingClientRect(), scale);
            const triggerX = triggerRect.left;
            const triggerHeight = triggerRect.height;
            const isRtl = rootState.direction === 'rtl';
            const scroller = rootState.listElement || popupElement;
            const scrollHeight = scroller.scrollHeight;

            if (
                positionerRect.width === 0 ||
                positionerRect.height === 0 ||
                scrollHeight === 0
            ) {
                scheduleOpenAlignItemPlacement(rootId);
                return;
            }

            const borderBottom = parseFloat(popupStyles.borderBottomWidth) || 0;
            const marginTop = parseFloat(positionerStyles.marginTop) || 10;
            const marginBottom = parseFloat(positionerStyles.marginBottom) || 10;
            const minHeight = parseFloat(positionerStyles.minHeight) || 100;
            const maxPopupHeight = getMaxPopupHeight(popupStyles);

            const paddingLeft = 5;
            const paddingRight = 5;
            const triggerCollisionThreshold = 20;

            const viewportHeight = doc.documentElement.clientHeight - marginTop - marginBottom;
            const viewportWidth = doc.documentElement.clientWidth;
            const availableSpaceBeneathTrigger = viewportHeight - triggerRect.bottom + triggerHeight;

            const textElement = rootState.selectedItemTextElement || null;
            const valueElement = rootState.valueElement || null;

            let textRect;
            let offsetX = 0;
            let offsetY = 0;

            if (textElement && valueElement) {
                const valueRect = normalizeRect(valueElement.getBoundingClientRect(), scale);
                textRect = normalizeRect(textElement.getBoundingClientRect(), scale);

                if (isRtl) {
                    // Mirror the alignment math from the right edges so the popup's
                    // text/right anchor lines up with the trigger value's right anchor.
                    const valueRightFromTriggerRight = triggerRect.right - valueRect.right;
                    const textRightFromPositionerRight = positionerRect.right - textRect.right;
                    offsetX = valueRightFromTriggerRight - textRightFromPositionerRight;
                } else {
                    const valueLeftFromTriggerLeft = valueRect.left - triggerX;
                    const textLeftFromPositionerLeft = textRect.left - positionerRect.left;
                    offsetX = valueLeftFromTriggerLeft - textLeftFromPositionerLeft;
                }

                const valueCenterFromPositionerTop =
                    valueRect.top - triggerRect.top + valueRect.height / 2;
                const textCenterFromTriggerTop =
                    textRect.top - positionerRect.top + textRect.height / 2;

                offsetY = textCenterFromTriggerTop - valueCenterFromPositionerTop;
            }

            const idealHeight = availableSpaceBeneathTrigger + offsetY + marginBottom + borderBottom;
            let height = Math.min(viewportHeight, idealHeight);
            const maxHeight = viewportHeight - marginTop - marginBottom;
            const scrollTop = idealHeight - height;

            const maxRight = viewportWidth - paddingRight;
            let left;
            let leftOverflow = 0;
            let rightOverflow = 0;
            if (isRtl) {
                // Anchor the popup's right edge to the trigger's right edge
                // (with the alignment offset). Clamp to viewport on both sides.
                left = triggerRect.right - offsetX - positionerRect.width;
                leftOverflow = Math.max(0, paddingLeft - left);
                rightOverflow = Math.max(0, left + leftOverflow + positionerRect.width - maxRight);
            } else {
                left = Math.max(paddingLeft, triggerX + offsetX);
                rightOverflow = Math.max(0, left + positionerRect.width - maxRight);
            }

            // React applies the governing height before reading maxScrollTop. A
            // separate SelectList, padding, borders, and box sizing can make
            // `scrollHeight - height` differ from the browser's live result.
            positionerElement.style.position = 'fixed';
            positionerElement.style.left = `${left + leftOverflow - rightOverflow}px`;
            positionerElement.style.height = `${height}px`;
            positionerElement.style.maxHeight = 'none';
            positionerElement.style.marginTop = `${marginTop}px`;
            positionerElement.style.marginBottom = `${marginBottom}px`;
            popupElement.style.height = '100%';

            const maxScrollTop = getMaxScrollTop(scroller);
            const isTopPositioned = scrollTop >= maxScrollTop - SCROLL_EDGE_TOLERANCE_PX;

            if (isTopPositioned) {
                height = Math.min(viewportHeight, positionerRect.height) - (scrollTop - maxScrollTop);
            }

            const fallbackToAlignPopupToTrigger =
                triggerRect.top < triggerCollisionThreshold ||
                triggerRect.bottom > viewportHeight - triggerCollisionThreshold ||
                Math.ceil(height) + SCROLL_EDGE_TOLERANCE_PX < Math.min(scrollHeight, minHeight);

            const visualScale = (win && win.visualViewport && win.visualViewport.scale) || 1;
            const isPinchZoomed = visualScale !== 1 && isWebKit();

            if (fallbackToAlignPopupToTrigger || isPinchZoomed) {
                if (!requestStandardPositioningFallback(rootState, popupState)) {
                    scheduleOpenAlignItemPlacement(rootId);
                }
                return;
            }

            attachWindowResizeListener(rootId);

            // === Commit phase ===
            // Force `position: fixed` so the placement coordinates are resolved
            // against the viewport instead of the nearest positioned ancestor.
            // The body scroll-lock applies `position: relative` to <body>, which
            // would otherwise cause the absolutely-positioned popup to resolve
            // against the (internally-scrolled) body — pushing it off-screen /
            // "to the top of the page" for sections reached by scrolling.
            // Mirrors React's `FIXED = { position: 'fixed' }` branch for
            // `alignItemWithTriggerActive` in SelectPositioner.tsx.
            positionerElement.style.height = `${height}px`;

            const initialHeight = Math.max(minHeight, height);

            if (isTopPositioned) {
                const topOffset = Math.max(0, viewportHeight - idealHeight);
                positionerElement.style.top = positionerRect.height >= maxHeight ? '0' : `${topOffset}px`;
                positionerElement.style.height = `${height}px`;
                scroller.scrollTop = getMaxScrollTop(scroller);
            } else {
                positionerElement.style.bottom = '0';
                scroller.scrollTop = scrollTop;
            }

            // Mark the positioner as positioned now that the align-item commit
            // has finished writing its placement styles. While
            // `alignItemWithTriggerActive` is true on the positionerState,
            // floating.js's updatePositionInternal deliberately skips writing
            // `data-positioned`, so this site is the sole owner. The FOUC CSS
            // (`[role="presentation"][data-side]:not([data-positioned])`) keeps
            // the popup invisible until this attribute lands — eliminating the
            // brief flash of the floating-default placement.
            positionerElement.setAttribute('data-positioned', '');
            popupState.placementCommittedRevision = placementRevision;
            popupState.placementCommittedInputRevision = placementInputRevision;

            if (textRect) {
                const popupTop = positionerRect.top;
                const popupHeight = positionerRect.height;
                const textCenterY = textRect.top + textRect.height / 2;

                const transformOriginY =
                    popupHeight > 0 ? ((textCenterY - popupTop) / popupHeight) * 100 : 50;
                const clampedY = clamp(transformOriginY, 0, 100);
                popupElement.style.setProperty('--transform-origin', `50% ${clampedY}%`);
            }

            if (initialHeight === viewportHeight || height >= maxPopupHeight) {
                popupState.reachedMaxHeight = true;
            }

            notifyScrollArrowVisibility(rootState);

    } catch (error) {
        void error;
        scheduleOpenAlignItemPlacement(rootId);
    } finally {
        restoreTransformStyles();
        if (
            popupState.placementInProgressRevision === placementRevision &&
            popupState.placementInProgressInputRevision === placementInputRevision
        ) {
            popupState.placementInProgressRevision = -1;
            popupState.placementInProgressInputRevision = -1;
        }
    }
}

export function handlePopupScroll(rootId, scroller) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !scroller) return;
    handlePopupScrollInternal(rootState, scroller);
}

function clearPopupStylesInternal(rootState) {
    const popupState = rootState.popup;
    if (!popupState) return;
    const positionerElement = rootState.positionerElement;
    if (positionerElement && popupState.savedPositionerStyles) {
        for (const [key, value] of Object.entries(popupState.originalPositionerStyles)) {
            if (value === undefined || value === null) {
                positionerElement.style.removeProperty(toKebabCase(key));
            } else {
                positionerElement.style[key] = value;
            }
        }
    }
    // Reset popup-level styles applied during alignItemWithTrigger placement.
    // Without this, `height: 100%` and `--transform-origin` leak across open cycles
    // and cause the popup to render at 0 height (invisible) after the fallback path
    // runs when the trigger is near a viewport edge.
    const popupElement = popupState.popupElement;
    if (popupElement) {
        popupElement.style.height = '';
        popupElement.style.removeProperty('--transform-origin');
    }
    // Reset the saved-styles flag so the next `saveOriginalPositionerStyles` call
    // re-captures the current layout. Critical for the fallback-then-close flow:
    // when `beginAlignItemWithTriggerPlacement` detects a fallback, it calls this
    // function — if we left the flag set, the close-time clearPopupStyles call
    // would restore pre-alignItem floating-ui coordinates on top of the
    // post-fallback floating-ui layout, causing the popup to briefly jump from
    // its fallback placement (above the trigger) back to its initial placement
    // (below the trigger) during the exit transition — the "appears on top of
    // the input for a moment" flash reported for near-bottom-of-viewport selects.
    popupState.savedPositionerStyles = false;
    popupState.originalPositionerStyles = {};
    popupState.initialPlaced = false;
    popupState.reachedMaxHeight = false;
}

function toKebabCase(camel) {
    return camel.replace(/[A-Z]/g, (c) => '-' + c.toLowerCase());
}

export function clearPopupStyles(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;
    clearPopupStylesInternal(rootState);
}

export function attachWindowResizeListener(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;
    const popupState = ensurePopupState(rootState);
    if (popupState.resizeHandler) return;
    const popupElement = popupState.popupElement || rootState.popupElement;
    const win = popupElement ? popupElement.ownerDocument.defaultView : window;
    if (!win) return;

    popupState.resizeHandler = () => {
        if (popupState.dotNetRef) {
            popupState.dotNetRef.invokeMethodAsync('OnWindowResize').catch(() => { });
        }
    };
    win.addEventListener('resize', popupState.resizeHandler);
}

export function detachWindowResizeListener(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !rootState.popup || !rootState.popup.resizeHandler) return;
    const popupElement = rootState.popup.popupElement || rootState.popupElement;
    const win = popupElement ? popupElement.ownerDocument.defaultView : window;
    if (win) {
        win.removeEventListener('resize', rootState.popup.resizeHandler);
    }
    rootState.popup.resizeHandler = null;
}

const scrollbarStyleInjectedKey = Symbol.for('Blazix.BaseUI.Select.ScrollbarStyleInjected');

export function injectScrollbarDisableStyle(nonce) {
    if (typeof document === 'undefined') return;
    if (window[scrollbarStyleInjectedKey]) return;
    if (document.head.querySelector('style[data-blazix-base-ui-disable-scrollbar]')) {
        window[scrollbarStyleInjectedKey] = true;
        return;
    }

    const styleEl = document.createElement('style');
    styleEl.setAttribute('data-blazix-base-ui-disable-scrollbar', '');
    if (nonce) styleEl.setAttribute('nonce', nonce);
    styleEl.textContent = '.blazix-base-ui-disable-scrollbar::-webkit-scrollbar{display:none;}.blazix-base-ui-disable-scrollbar{scrollbar-width:none;}';
    document.head.appendChild(styleEl);
    window[scrollbarStyleInjectedKey] = true;
}

export function setSelectedItemTextElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState && rootState.selectedItemTextElement !== element) {
        rootState.selectedItemTextElement = element;
        invalidateAlignItemPlacement(rootState);
        if (
            rootState.isOpen &&
            rootState.positionerReadyElement === rootState.positionerElement &&
            rootState.positionerReadyRevision === rootState.openRevision
        ) {
            beginAlignItemWithTriggerPlacement(rootId, true);
        } else {
            queueOpenAlignItemPlacement(rootId);
        }
        if (rootState.isOpen && element) {
            requestAnimationFrame(() => {
                if (!rootState.isOpen || rootState.selectedItemTextElement !== element) return;
                const item = element.closest('[role="option"]');
                const scroller = rootState.listElement || item?.closest('[role="listbox"]');
                if (item && scroller) {
                    scrollItemIntoViewIfNeeded(scroller, item);
                }
            });
        }
    }
}

// Returns the textContent of a captured element (used by SelectItem as a lazy
// typeahead label fallback when the user did not supply a Label prop).
// Mirrors React's use of textRef.current?.textContent through useCompositeListItem.
// Returns the DOM-order index of an `[role="option"]` element within its enclosing
// `[role="listbox"]` (falling back to the immediate parent). This is the authoritative
// source of truth for an item's ordinal — JS keyboard navigation uses the same
// `querySelectorAll('[role="option"]')` order, so deriving `highlighted` from this
// value keeps C# and JS in agreement even when items are inserted mid-list.
export function getOptionDomIndex(element) {
    if (!element) return -1;
    const container = element.closest('[role="listbox"]') || element.parentElement;
    if (!container) return -1;
    const options = container.querySelectorAll('[role="option"]');
    for (let i = 0; i < options.length; i++) {
        if (options[i] === element) return i;
    }
    return -1;
}

function notifyOptionIndexes(rootState) {
    const container = rootState.listElement || rootState.popupElement;
    if (!container) return false;
    const items = Array.from(container.querySelectorAll('[role="option"]'));
    const layoutChanged =
        items.length !== rootState.observedOptions.length ||
        items.some((item, index) => rootState.observedOptions[index] !== item);
    rootState.observedOptions = items;
    for (let index = 0; index < items.length; index++) {
        const registration = rootState.optionRegistrations.get(items[index]);
        registration?.dotNetRef.invokeMethodAsync('OnDomIndexChanged', index).catch(() => { });
    }
    return layoutChanged;
}

function ensureOptionObserver(rootState) {
    const container = rootState.listElement || rootState.popupElement;
    if (!container || rootState.optionObserver) return;
    rootState.optionObserver = new MutationObserver(() => {
        if (notifyOptionIndexes(rootState)) {
            invalidateAlignItemPlacement(rootState);
            queueOpenAlignItemPlacement(rootState.rootId);
        }
    });
    notifyOptionIndexes(rootState);
    rootState.optionObserver.observe(container, { childList: true, subtree: true });
}

export function registerOption(rootId, element, dotNetRef) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !element) return -1;
    const previousRegistration = rootState.optionRegistrations.get(element);
    if (previousRegistration) {
        element.removeEventListener('click', previousRegistration.clickHandler, true);
    }
    const registration = {
        dotNetRef,
        clickMetadata: null,
        clickHandler(event) {
            registration.clickMetadata = {
                isVirtualClick: isVirtualClickEvent(event),
                pointerTypeDefined: event.pointerType !== undefined
            };
        }
    };
    element.addEventListener('click', registration.clickHandler, true);
    rootState.optionRegistrations.set(element, registration);
    ensureOptionObserver(rootState);
    const index = getOptionDomIndex(element);
    queueMicrotask(() => notifyOptionIndexes(rootState));
    return index;
}

export function unregisterOption(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !element) return;
    const registration = rootState.optionRegistrations.get(element);
    if (registration) {
        element.removeEventListener('click', registration.clickHandler, true);
    }
    rootState.optionRegistrations.delete(element);
    queueMicrotask(() => notifyOptionIndexes(rootState));
}

export function consumeOptionClickMetadata(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return null;
    const registration = rootState.optionRegistrations.get(element);
    const metadata = registration?.clickMetadata || rootState.optionClickMetadata.get(element) || null;
    if (registration) registration.clickMetadata = null;
    rootState.optionClickMetadata.delete(element);
    return metadata;
}

export function getElementText(element) {
    return element?.textContent ?? null;
}

export function setValueElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState && rootState.valueElement !== element) {
        rootState.valueElement = element;
        invalidateAlignItemPlacement(rootState);
        queueOpenAlignItemPlacement(rootId);
    }
}

export function setHighlightItemOnHover(rootId, value) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.highlightItemOnHover = !!value;
    }
}

// ─── Scroll Lock Bridge (for the positioner's body scroll lock) ───────
// The scroll-lock module returns a cleanup function; JS-to-Blazor interop
// cannot invoke raw functions, so we store them under an ID and expose
// acquire/release by-ID wrappers.

const scrollLocksKey = Symbol.for('Blazix.BaseUI.Select.ScrollLocks');
if (!window[scrollLocksKey]) {
    window[scrollLocksKey] = { counter: 0, map: new Map() };
}
const scrollLocks = window[scrollLocksKey];

export function applyScrollLock(referenceElement) {
    const release = acquireScrollLock(referenceElement);
    const id = `sel-sl-${++scrollLocks.counter}`;
    scrollLocks.map.set(id, release);
    return id;
}

export function shouldLockTouchScroll(positionerElement) {
    if (!(positionerElement instanceof HTMLElement)) return false;
    const win = positionerElement.ownerDocument.defaultView;
    if (!win) return false;
    return positionerElement.getBoundingClientRect().width >= win.innerWidth - 20;
}

export function releaseScrollLock(id) {
    if (!id) return;
    const release = scrollLocks.map.get(id);
    if (release) {
        try {
            release();
        } finally {
            scrollLocks.map.delete(id);
        }
    }
}
