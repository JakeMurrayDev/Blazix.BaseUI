/**
 * Blazix.BaseUI Popover Component
 *
 * Popover-specific functionality that builds on the shared floating infrastructure.
 */

import { acquireScrollLock } from './blazix-baseui-scroll-lock.min.js';
import { requestDoubleAnimationFrame } from './blazix-baseui-animations.min.js';

const PATIENT_CLICK_THRESHOLD = 500;
const VIEWPORT_WIDTH_TOLERANCE_PX = 20;

import {
    TABBABLE_SELECTOR,
    getTabbableElements,
    getPreviousTabbable,
    enableFocusInside,
    createHoverInteraction,
    checkForTransitionOrAnimation,
    setupTransitionEndListener,
    cleanupTransitionState,
    disposeHoverInteractionOnRoot,
    updateHoverInteractionFloatingOnRoot,
    setHoverInteractionOpenOnRoot,
    initializePositioner as floatingInitializePositioner,
    updatePositioner as floatingUpdatePositioner,
    disposePositioner as floatingDisposePositioner
} from './blazix-baseui-floating.min.js';

const STATE_KEY = Symbol.for('Blazix.BaseUI.Popover.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        roots: new Map(),
        positioners: new Map(),
        popups: new WeakMap(),
        globalListenersInitialized: false,
        openOrderCounter: 0
    };
}
const state = window[STATE_KEY];

// Track where the pointer press started for intentional-mode drag-out suppression
let pointerDownTarget = null;
let pointerDownTime = 0;

function initGlobalListeners() {
    if (state.globalListenersInitialized) return;

    document.addEventListener('keydown', handleGlobalKeyDown);
    document.addEventListener('pointerdown', handleGlobalPointerDown);
    document.addEventListener('click', handleGlobalClick);
    state.globalListenersInitialized = true;
}

// ============================================================================
// Hover Interaction Support
// ============================================================================

export async function initializeHoverInteraction(rootId, triggerElement, openDelay, closeDelay, callbackDotNetRef) {
    let rootState = state.roots.get(rootId);

    // For handle-based triggers, create a lightweight state entry if root doesn't exist
    if (!rootState && callbackDotNetRef) {
        rootState = { triggerElement, isOpen: false };
        state.roots.set(rootId, rootState);
    }

    // If root state doesn't exist yet, wait briefly for it to be initialized
    if (!rootState) {
        await new Promise(resolve => setTimeout(resolve, 50));
        rootState = state.roots.get(rootId);
        if (!rootState) return;
    }

    // Clean up existing hover interaction before replacing the trigger element.
    if (rootState.hoverInteraction) {
        rootState.hoverInteraction.cleanup();
    }
    rootState.triggerHoverStickCleanup?.();
    rootState.triggerHoverStickCleanup = null;

    // Store the trigger element if provided
    if (triggerElement) {
        rootState.triggerElement = triggerElement;
    }

    if (!rootState.triggerElement) return;

    // Use callback dotnet ref if provided, otherwise fall back to root dotnet ref
    const dotNetRef = callbackDotNetRef || rootState.dotNetRef;

    rootState.hoverInteraction = createHoverInteraction({
        interactionId: `popover-hover-${rootId}`,
        triggerElement: rootState.triggerElement,
        floatingElement: rootState.popupElement,
        openDelay: openDelay || 0,
        closeDelay: closeDelay || 0,
        mouseOnly: true,
        useSafePolygon: true,
        safePolygonOptions: { blockPointerEvents: false },
        onOpen: (reason) => {
            if (dotNetRef && !rootState.isOpen) {
                dotNetRef.invokeMethodAsync('OnHoverOpen').catch(() => { });
            }
        },
        onClose: (reason) => {
            if (dotNetRef && rootState.isOpen) {
                dotNetRef.invokeMethodAsync('OnHoverClose').catch(() => { });
            }
        }
    });

    const handleTriggerReEnter = () => {
        if (rootState.isOpen) {
            startPatientClickProtection(rootState);
        }
    };
    const triggerElementForCleanup = rootState.triggerElement;
    triggerElementForCleanup.addEventListener('mouseenter', handleTriggerReEnter);
    rootState.triggerHoverStickCleanup = () => {
        triggerElementForCleanup.removeEventListener('mouseenter', handleTriggerReEnter);
    };
}

export function disposeHoverInteraction(rootId) {
    const rootState = state.roots.get(rootId);
    rootState?.triggerHoverStickCleanup?.();
    if (rootState) {
        rootState.triggerHoverStickCleanup = null;
    }

    disposeHoverInteractionOnRoot(state.roots, rootId);
}

export function updateHoverInteractionFloatingElement(rootId) {
    updateHoverInteractionFloatingOnRoot(state.roots, rootId);
}

export function setHoverInteractionOpen(rootId, isOpen) {
    setHoverInteractionOpenOnRoot(state.roots, rootId, isOpen);
}

// ============================================================================
// Trigger Focus Guard Support
// ============================================================================

/**
 * Focuses the previous tabbable element before the given guard element.
 * Used by the trigger's pre-focus guard on Shift+Tab.
 */
export function focusPreviousTabbable(guardElement) {
    const prev = getPreviousTabbable(guardElement, document.body, document.body);
    if (prev) prev.focus();
}

function isFocusGuard(element) {
    return element instanceof HTMLElement && element.hasAttribute('data-blazix-base-ui-focus-guard');
}

function isOutsideFocusGuard(element) {
    return isFocusGuard(element) && element.getAttribute('data-blazix-base-ui-focus-guard-type') === 'outside';
}

function isInsideRootElementsTarget(target, rootState) {
    if (!(target instanceof Node)) return false;
    return (rootState.positionerElement && rootState.positionerElement.contains(target))
        || (rootState.popupElement && rootState.popupElement.contains(target));
}

function isVisibleTabStop(element) {
    if (!(element instanceof HTMLElement)) return false;
    if (element.disabled) return false;
    if (element.getAttribute('tabindex') !== null && parseInt(element.getAttribute('tabindex'), 10) < 0 && !isFocusGuard(element)) {
        return false;
    }
    return element.offsetParent !== null || getComputedStyle(element).position === 'fixed';
}

function getDocumentTabOrder() {
    return Array.from(document.body.querySelectorAll(`${TABBABLE_SELECTOR}, [data-blazix-base-ui-focus-guard]`))
        .filter(isVisibleTabStop);
}

function getNextDocumentTabStop(element) {
    const order = getDocumentTabOrder();
    const index = order.indexOf(element);
    if (index === -1 || index >= order.length - 1) return null;
    return order[index + 1];
}

function getPreviousDocumentTabStop(element) {
    const order = getDocumentTabOrder();
    const index = order.indexOf(element);
    if (index <= 0) return null;
    return order[index - 1];
}

function getProgrammaticPopupFocusables(rootState) {
    const container = rootState.positionerElement || rootState.popupElement;
    if (!container) return [];

    return Array.from(container.querySelectorAll(TABBABLE_SELECTOR))
        .filter((element) => element instanceof HTMLElement)
        .filter((element) => !isFocusGuard(element))
        .filter((element) => !element.disabled)
        .filter((element) => element.offsetParent !== null || getComputedStyle(element).position === 'fixed');
}

function focusLastPopoverContent(rootState) {
    const focusables = getProgrammaticPopupFocusables(rootState);
    const last = focusables[focusables.length - 1] || rootState.popupElement || rootState.positionerElement;
    if (last?.focus) {
        last.focus();
        return true;
    }
    return false;
}

function focusNextAfterPopoverSource(rootState, fallbackElement) {
    const order = getDocumentTabOrder();
    const index = order.indexOf(fallbackElement);
    if (index === -1) return false;

    let sawOutsideGuard = false;
    for (let i = index + 1; i < order.length; i += 1) {
        const candidate = order[i];
        if (isInsideRootElementsTarget(candidate, rootState)) {
            continue;
        }

        if (isOutsideFocusGuard(candidate)) {
            sawOutsideGuard = true;
            continue;
        }

        if (isFocusGuard(candidate)) {
            continue;
        }

        if (sawOutsideGuard) {
            candidate.focus();
            return true;
        }
    }

    for (let i = index + 1; i < order.length; i += 1) {
        const candidate = order[i];
        if (!isFocusGuard(candidate) && !isInsideRootElementsTarget(candidate, rootState)) {
            candidate.focus();
            return true;
        }
    }

    return false;
}

export function handleTriggerPreGuardFocus(rootId, guardElement) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !guardElement) return 'close-previous';

    const previous = getPreviousDocumentTabStop(guardElement);
    if (isOutsideFocusGuard(previous)) {
        return focusLastPopoverContent(rootState) ? 'handled' : 'close-previous';
    }

    return 'close-previous';
}

export function handleTriggerPostGuardFocus(rootId, guardElement) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !guardElement) return 'handled';

    if (rootState.lastFocusOutTarget === guardElement) {
        rootState.lastFocusOutTarget = null;
        rootState.lastFocusOutFromPopover = false;
        focusNextAfterPopoverSource(rootState, guardElement);
        return 'close';
    }

    const next = getNextDocumentTabStop(guardElement);
    if (next && !isFocusGuard(next) && !isInsideRootElementsTarget(next, rootState)) {
        next.focus();
        return 'close';
    }

    focusPopoverContent(rootId);
    return 'handled';
}

/**
 * Redirects focus into the popover content.
 * Finds the FloatingFocusManager's before-content guard inside the positioner
 * and focuses it, which in turn redirects to the first tabbable in the popup.
 * Used by the trigger's post-focus guard on Tab forward.
 */
export function focusPopoverContent(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState?.positionerElement) return;

    enableFocusInside(rootState.positionerElement);

    const focusables = getProgrammaticPopupFocusables(rootState);
    if (focusables.length > 0) {
        focusables[0].focus();
        return;
    }

    rootState.popupElement?.focus();
}

function handleGlobalKeyDown(e) {
    if (e.key !== 'Escape') return;

    // Close the topmost (most recently opened) popover
    let topmost = null;
    let highestOrder = -1;

    for (const [id, rootState] of state.roots) {
        if (rootState.isOpen && rootState.dotNetRef && rootState.openOrderStamp > highestOrder) {
            highestOrder = rootState.openOrderStamp;
            topmost = rootState;
        }
    }

    if (topmost) {
        topmost.dotNetRef.invokeMethodAsync('OnEscapeKey').catch(() => { });
    }
}

function isInsideRootElements(target, rootState) {
    return isInsideRootOwnedElements(target, rootState) || isInsideDescendantRootElements(target, rootState);
}

function isInsideRootOwnedElements(target, rootState) {
    const { triggerElement, popupElement, positionerElement, triggerElements } = rootState;
    return (positionerElement && positionerElement.contains(target))
        || (popupElement && popupElement.contains(target))
        || (triggerElement && triggerElement.contains(target))
        || (triggerElements && Array.from(triggerElements.values()).some((element) => element && element.contains(target)));
}

function getRootTriggerElements(rootState) {
    const elements = [];
    if (rootState.triggerElement) {
        elements.push(rootState.triggerElement);
    }

    if (rootState.triggerElements) {
        for (const trigger of rootState.triggerElements.values()) {
            if (trigger && !elements.includes(trigger)) {
                elements.push(trigger);
            }
        }
    }

    return elements;
}

function isRootDirectChildOf(parentRootState, childRootState) {
    return getRootTriggerElements(childRootState).some((trigger) => isInsideRootOwnedElements(trigger, parentRootState));
}

function isRootDescendantOf(parentRootState, childRootState, visited = new Set()) {
    if (!parentRootState || !childRootState || parentRootState === childRootState || visited.has(childRootState)) {
        return false;
    }

    if (isRootDirectChildOf(parentRootState, childRootState)) {
        return true;
    }

    visited.add(childRootState);

    for (const candidateParent of state.roots.values()) {
        if (candidateParent === childRootState) {
            continue;
        }

        if (isRootDirectChildOf(candidateParent, childRootState) && isRootDescendantOf(parentRootState, candidateParent, visited)) {
            return true;
        }
    }

    return false;
}

function isInsideDescendantRootElements(target, rootState) {
    for (const candidateRootState of state.roots.values()) {
        if (candidateRootState === rootState) {
            continue;
        }

        if (isRootDescendantOf(rootState, candidateRootState) && isInsideRootOwnedElements(target, candidateRootState)) {
            return true;
        }
    }

    return false;
}

function closeDescendantRoots(rootState) {
    for (const candidateRootState of state.roots.values()) {
        if (
            candidateRootState !== rootState &&
            candidateRootState.isOpen &&
            candidateRootState.dotNetRef &&
            isRootDescendantOf(rootState, candidateRootState)
        ) {
            candidateRootState.dotNetRef.invokeMethodAsync('OnOutsidePress').catch(() => { });
        }
    }
}

/**
 * Shared outside-press processing loop.
 * @param {Event} e - The pointer or click event.
 * @param {function} rootFilter - Predicate selecting which roots to process.
 * @param {boolean} checkDragOut - When true, suppress if pointerdown started inside.
 */
function processOutsidePressForRoots(e, rootFilter, checkDragOut) {
    const openRoots = [];
    const now = performance.now();
    for (const [id, rootState] of state.roots) {
        const openedAfterCurrentPointerDown = checkDragOut && pointerDownTime > 0 && rootState.openedAt >= pointerDownTime;
        if (
            rootState.isOpen &&
            rootState.dotNetRef &&
            rootFilter(rootState) &&
            !openedAfterCurrentPointerDown &&
            (!rootState.ignoreOutsidePressUntil || now > rootState.ignoreOutsidePressUntil)
        ) {
            openRoots.push({ id, rootState });
        }
    }

    if (openRoots.length === 0) return;

    openRoots.sort((a, b) => b.rootState.openOrderStamp - a.rootState.openOrderStamp);

    // Process from topmost to outermost — stop once a root "contains" the press
    for (const { id, rootState } of openRoots) {
        if (isInsideRootElements(e.target, rootState)) break;

        // Drag-out suppression: pointerdown started inside but click landed outside
        if (checkDragOut && pointerDownTarget && isInsideRootElements(pointerDownTarget, rootState)) {
            break;
        }

        const clickedOnOwnBackdrop = rootState.internalBackdropElement
            && (rootState.internalBackdropElement === e.target
                || rootState.internalBackdropElement.contains(e.target));

        if (clickedOnOwnBackdrop) {
            // Press on own backdrop = outside press for this root, continue to parents
            rootState.dotNetRef.invokeMethodAsync('OnOutsidePress').catch(() => { });
            continue;
        }

        // Press is outside this popover — close it
        rootState.dotNetRef.invokeMethodAsync('OnOutsidePress').catch(() => { });
    }
}

/**
 * Sloppy mode: fires immediately on pointerdown.
 * Applied to trap-focus roots (any pointer type) and all roots on touch/pen.
 */
function handleGlobalPointerDown(e) {
    pointerDownTarget = e.target;
    pointerDownTime = performance.now();

    const isTouchOrPen = e.pointerType === 'touch' || e.pointerType === 'pen';
    processOutsidePressForRoots(e,
        (rs) => rs.modal === 'trap-focus' || isTouchOrPen,
        false
    );
}

/**
 * Intentional mode: fires on click (mousedown + mouseup pair).
 * Applied to non-trap-focus roots. Suppresses drag-out (pointerdown inside, click outside).
 */
function handleGlobalClick(e) {
    processOutsidePressForRoots(e,
        (rs) => rs.modal !== 'trap-focus',
        true
    );
    pointerDownTarget = null;
    pointerDownTime = 0;
}

// ============================================================================
// Root Management
// ============================================================================

export function initializeRoot(rootId, dotNetRef, modal) {
    initGlobalListeners();

    state.roots.set(rootId, {
        dotNetRef,
        isOpen: false,
        modal: modal || 'false',
        triggerElement: null,
        triggerElements: new Map(),
        positionerElement: null,
        backdropElement: null,
        popupElement: null,
        internalBackdropElement: null,
        hoverInteraction: null,
        triggerHoverStickCleanup: null,
        focusTrapCleanup: null,
        focusManagerId: null,
        focusOutCleanup: null,
        popupFocusOutCleanup: null,
        releaseScrollLock: null,
        positionerId: null,
        initialFocusMode: null,
        initialFocusElement: null,
        finalFocusMode: null,
        finalFocusElement: null,
        transitionCleanup: null,
        fallbackTimeoutId: null,
        pendingOpen: false,
        openReason: null,
        interactionType: null,
        openedAt: 0,
        openOrderStamp: 0,
        stickIfOpen: false,
        patientClickTimeout: null,
        patientClickObserver: null,
        patientClickArmTimeout: null,
        patientClickFrame: null,
        ignoreOutsidePressUntil: 0,
        compositeKeyCleanup: null,
        viewportElement: null,
        viewportDotNetRef: null,
        pendingViewportTransition: null,
        backdropCutoutCleanup: null,
        triggerElementCleanups: new Map(),
        viewportTransitionCleanup: null
    });
}

export function disposeRoot(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        // Clean up hover interaction
        if (rootState.hoverInteraction) {
            rootState.hoverInteraction.cleanup();
        }
        rootState.triggerHoverStickCleanup?.();
        rootState.triggerHoverStickCleanup = null;

        // Clean up patient click timeout
        clearPatientClickTimeout(rootState);

        // Clean up focus management
        cleanupFocusTrap(rootState);
        cleanupFocusOutListener(rootState);
        cleanupPopupFocusOutListener(rootState);

        // Clean up composite key suppression
        rootState.compositeKeyCleanup?.();

        // Clean up backdrop cutout
        cleanupBackdropCutout(rootState);

        // Clean up transition listener
        cleanupTransitionState(rootState);

        // Clean up viewport
        cleanupViewportTransition(rootState);
        rootState.viewportDotNetRef = null;
        rootState.viewportElement = null;

        // Clean up registered trigger listeners
        cleanupRegisteredTriggerElements(rootState);

        // Release scroll lock
        if (rootState.releaseScrollLock) {
            rootState.releaseScrollLock();
            rootState.releaseScrollLock = null;
        }
    }
    state.roots.delete(rootId);
}

export function updateScrollLock(rootId, modal) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.modal = modal;
    syncScrollLock(rootState);
}

function syncScrollLock(rootState) {
    const shouldLock = shouldLockScroll(rootState);

    if (shouldLock && !rootState.releaseScrollLock) {
        rootState.releaseScrollLock = acquireScrollLock(rootState.positionerElement);
    } else if (!shouldLock && rootState.releaseScrollLock) {
        rootState.releaseScrollLock();
        rootState.releaseScrollLock = null;
    }
}

function shouldLockScroll(rootState) {
    if (!rootState?.isOpen || rootState.modal !== 'true' || rootState.openReason === 'trigger-hover') {
        return false;
    }

    if (rootState.interactionType !== 'touch') {
        return true;
    }

    const positioner = rootState.positionerElement;
    if (!positioner) {
        return false;
    }

    const doc = positioner.ownerDocument || document;
    const viewportWidth = doc.documentElement?.clientWidth ?? 0;
    const positionerWidth = positioner.offsetWidth ?? 0;

    return viewportWidth > 0 &&
        positionerWidth > 0 &&
        positionerWidth >= viewportWidth - VIEWPORT_WIDTH_TOLERANCE_PX;
}

export function hydrateRootOpen(rootId, isOpen, reason, interactionType) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.isOpen = isOpen;
    rootState.pendingOpen = isOpen;
    rootState.openReason = reason || null;
    rootState.interactionType = interactionType || null;

    if (isOpen) {
        rootState.openedAt = performance.now();
        rootState.openOrderStamp = ++state.openOrderCounter;
        if (reason === 'imperative-action') {
            rootState.ignoreOutsidePressUntil = performance.now() + 100;
        }
    } else {
        rootState.openedAt = 0;
        rootState.ignoreOutsidePressUntil = 0;
    }

    if (rootState.hoverInteraction) {
        rootState.hoverInteraction.setOpen(isOpen);
    }

    syncBackdropHidden(rootState);

    if (isOpen) {
        syncScrollLock(rootState);

        if (rootState.internalBackdropElement) {
            setupBackdropCutoutTracking(rootState);
        }
    } else {
        clearPatientClickTimeout(rootState);
        cleanupBackdropCutout(rootState);

        if (rootState.releaseScrollLock) {
            rootState.releaseScrollLock();
            rootState.releaseScrollLock = null;
        }

        cleanupFocusTrap(rootState);
        cleanupFocusOutListener(rootState);
    }
}

export function setRootOpen(rootId, isOpen, reason, interactionType) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.isOpen = isOpen;
    rootState.pendingOpen = isOpen;
    rootState.openReason = reason;
    rootState.interactionType = interactionType || null;

    if (isOpen) {
        rootState.openedAt = performance.now();
        rootState.openOrderStamp = ++state.openOrderCounter;
        if (reason === 'imperative-action') {
            rootState.ignoreOutsidePressUntil = performance.now() + 100;
        }
    } else {
        rootState.openedAt = 0;
        rootState.ignoreOutsidePressUntil = 0;
    }

    // Sync with hover interaction
    if (rootState.hoverInteraction) {
        rootState.hoverInteraction.setOpen(isOpen);
    }

    syncBackdropHidden(rootState);

    if (isOpen) {
        // Start patient click protection when hover-opened
        if (reason === 'trigger-hover') {
            startPatientClickProtection(rootState);
        }

        syncScrollLock(rootState);

        // Set up backdrop cutout tracking if internal backdrop is present
        if (rootState.internalBackdropElement) {
            setupBackdropCutoutTracking(rootState);
        }

        waitForPopupAndStartTransition(rootState, isOpen);
    } else {
        closeDescendantRoots(rootState);

        // Clear patient click protection on close
        clearPatientClickTimeout(rootState);

        // Clean up backdrop cutout
        cleanupBackdropCutout(rootState);

        // Release scroll lock
        if (rootState.releaseScrollLock) {
            rootState.releaseScrollLock();
            rootState.releaseScrollLock = null;
        }

        // Clean up focus management
        cleanupFocusTrap(rootState);
        cleanupFocusOutListener(rootState);
        focusFinalElementWhenReady(rootState);
        focusDefaultFinalElementWhenReady(rootState);

        startTransition(rootState, isOpen);
    }
}

function clearPatientClickTimeout(rootState) {
    if (rootState.patientClickTimeout !== null) {
        clearTimeout(rootState.patientClickTimeout);
        rootState.patientClickTimeout = null;
    }

    if (rootState.patientClickArmTimeout !== null) {
        clearTimeout(rootState.patientClickArmTimeout);
        rootState.patientClickArmTimeout = null;
    }

    if (rootState.patientClickFrame !== null) {
        cancelAnimationFrame(rootState.patientClickFrame);
        rootState.patientClickFrame = null;
    }

    if (rootState.patientClickObserver !== null) {
        rootState.patientClickObserver.disconnect();
        rootState.patientClickObserver = null;
    }

    rootState.stickIfOpen = false;
}

function startPatientClickProtection(rootState) {
    clearPatientClickTimeout(rootState);
    rootState.stickIfOpen = true;

    const startCountdown = () => {
        if (!rootState.stickIfOpen || rootState.patientClickTimeout !== null) {
            return;
        }

        rootState.patientClickTimeout = setTimeout(() => {
            rootState.stickIfOpen = false;
            rootState.patientClickTimeout = null;
        }, PATIENT_CLICK_THRESHOLD);
    };

    const startCountdownOnNextFrame = () => {
        if (rootState.patientClickFrame !== null) {
            return;
        }

        rootState.patientClickFrame = requestAnimationFrame(() => {
            rootState.patientClickFrame = null;
            startCountdown();
        });
    };

    const triggerElement = rootState.triggerElement;
    if (!triggerElement || triggerElement.hasAttribute('data-popup-open')) {
        startCountdownOnNextFrame();
        return;
    }

    rootState.patientClickObserver = new MutationObserver(() => {
        if (!triggerElement.hasAttribute('data-popup-open')) {
            return;
        }

        if (rootState.patientClickObserver !== null) {
            rootState.patientClickObserver.disconnect();
            rootState.patientClickObserver = null;
        }

        if (rootState.patientClickArmTimeout !== null) {
            clearTimeout(rootState.patientClickArmTimeout);
            rootState.patientClickArmTimeout = null;
        }

        startCountdownOnNextFrame();
    });

    rootState.patientClickObserver.observe(triggerElement, {
        attributes: true,
        attributeFilter: ['data-popup-open'],
    });

    rootState.patientClickArmTimeout = setTimeout(() => {
        if (rootState.patientClickObserver !== null) {
            rootState.patientClickObserver.disconnect();
            rootState.patientClickObserver = null;
        }

        rootState.patientClickArmTimeout = null;
        startCountdownOnNextFrame();
    }, PATIENT_CLICK_THRESHOLD * 2);
}

function syncBackdropHidden(rootState) {
    if (!rootState.backdropElement) return;

    rootState.backdropElement.hidden = !rootState.isOpen || rootState.openReason === 'trigger-hover';
}

/**
 * Called by the trigger before toggling. Returns true if the click should be
 * suppressed (the popover was recently hover-opened and should "stick" open).
 * Consuming the flag resets it so the next click toggles normally.
 */
export function consumeStickIfOpen(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return false;

    if (rootState.stickIfOpen) {
        clearPatientClickTimeout(rootState);
        return true;
    }

    return false;
}

// ============================================================================
// Tab Index Management
// ============================================================================

function handleTabIndex(popupElement) {
    if (!popupElement) return;

    if (popupElement.hasAttribute('tabindex') && !popupElement.hasAttribute('data-tabindex')) {
        return;
    }

    const role = popupElement.getAttribute('role') || '';
    if (!role.includes('dialog')) return;

    const tabbableContent = getTabbableElements(popupElement);
    const currentTabIndex = popupElement.getAttribute('tabindex');

    if (tabbableContent.length === 0) {
        if (currentTabIndex !== '0') {
            popupElement.setAttribute('tabindex', '0');
        }
    } else {
        if (currentTabIndex !== '-1') {
            popupElement.setAttribute('tabindex', '-1');
        }
    }
}

function cleanupFocusTrap(rootState) {
    if (rootState.focusTrapCleanup) {
        rootState.focusTrapCleanup();
        rootState.focusTrapCleanup = null;
    }
}

function cleanupFocusOutListener(rootState) {
    if (rootState.focusOutCleanup) {
        rootState.focusOutCleanup();
        rootState.focusOutCleanup = null;
    }
}

function cleanupPopupFocusOutListener(rootState) {
    if (rootState.popupFocusOutCleanup) {
        rootState.popupFocusOutCleanup();
        rootState.popupFocusOutCleanup = null;
    }
    rootState.lastFocusOutTarget = null;
    rootState.lastFocusOutFromPopover = false;
}

function focusInitialElementWhenReady(rootState) {
    const element = rootState.initialFocusElement;
    if (!(rootState.isOpen && element instanceof HTMLElement)) return;

    let attempts = 0;
    const maxAttempts = 5;

    const applyFocus = () => {
        if (!rootState.isOpen || rootState.initialFocusElement !== element) return;

        attempts += 1;
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

function focusFinalElementWhenReady(rootState) {
    const element = rootState.finalFocusElement;
    if (!(!rootState.isOpen && element instanceof HTMLElement)) return;

    let attempts = 0;
    const maxAttempts = 5;

    const applyFocus = () => {
        if (rootState.isOpen || rootState.finalFocusElement !== element) return;

        attempts += 1;
        if (document.contains(element)) {
            element.focus({ preventScroll: true });
            if (document.activeElement === element || attempts >= maxAttempts) return;
        }

        if (attempts < maxAttempts) {
            requestAnimationFrame(applyFocus);
        }
    };

    requestAnimationFrame(applyFocus);
}

function focusDefaultFinalElementWhenReady(rootState) {
    if (rootState.isOpen) return;
    if (rootState.finalFocusMode === 'none' || rootState.finalFocusMode === 'element') return;
    if (rootState.openReason === 'focus-out' || rootState.openReason === 'outside-press' || rootState.openReason === 'trigger-hover') return;

    const element = rootState.triggerElement;
    if (!(element instanceof HTMLElement)) return;

    let attempts = 0;
    const maxAttempts = 5;

    const applyFocus = () => {
        if (rootState.isOpen) return;

        attempts += 1;
        if (document.contains(element)) {
            element.focus({ preventScroll: true });
            if (document.activeElement === element || attempts >= maxAttempts) return;
        }

        if (attempts < maxAttempts) {
            requestAnimationFrame(applyFocus);
        }
    };

    requestAnimationFrame(applyFocus);
}

export function setInitialFocusElement(rootId, mode, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.initialFocusMode = mode || null;
        rootState.initialFocusElement = resolveFocusMode(mode, element);

        focusInitialElementWhenReady(rootState);
    }
}

export function setFinalFocusElement(rootId, mode, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.finalFocusMode = mode || null;
        rootState.finalFocusElement = resolveFocusMode(mode, element);
        focusFinalElementWhenReady(rootState);
    }
}

export function focusElement(element) {
    if (!element) return;
    element.focus();
}

// ============================================================================
// Transition Handling
// ============================================================================

function waitForPopupAndStartTransition(rootState, isOpen) {
    const popupElement = rootState.popupElement;

    if (popupElement) {
        startTransition(rootState, isOpen);
        return;
    }

    let attempts = 0;
    const maxAttempts = 10;

    function checkForPopup() {
        attempts++;
        const element = rootState.popupElement;

        if (element) {
            // Update hover interaction with the new popup element
            if (rootState.hoverInteraction) {
                rootState.hoverInteraction.setFloatingElement(element);
            }
            if (rootState.pendingOpen === isOpen) {
                startTransition(rootState, isOpen);
            }
        } else if (attempts < maxAttempts && rootState.pendingOpen === isOpen) {
            requestAnimationFrame(checkForPopup);
        } else if (rootState.dotNetRef && rootState.pendingOpen === isOpen) {
            rootState.dotNetRef.invokeMethodAsync('OnStartingStyleApplied').catch(() => { });
        }
    }

    requestAnimationFrame(checkForPopup);
}

async function startTransition(rootState, isOpen) {
    const popupElement = rootState.popupElement;

    if (!popupElement) {
        if (rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
        }
        return;
    }

    const hasTransition = checkForTransitionOrAnimation(popupElement);

    if (isOpen) {
        await requestDoubleAnimationFrame();

        if (rootState.pendingOpen !== isOpen) {
            return;
        }

        handleTabIndex(popupElement);

        if (hasTransition) {
            setupTransitionEndListener(rootState, isOpen);
        }
        if (rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnStartingStyleApplied').catch(() => { });
        }
        if (!hasTransition && rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', true).catch(() => { });
        }
    } else {
        if (hasTransition) {
            setupTransitionEndListener(rootState, isOpen);
        } else {
            if (rootState.dotNetRef) {
                rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
            }
        }
    }
}


// ============================================================================
// Element References
// ============================================================================

export function setTriggerElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        enableTriggerChangeTransitions(rootState, element);
        rootState.triggerElement = element;
        updateRootPositioner(rootState);
    }
}

function enableTriggerChangeTransitions(rootState, nextTriggerElement) {
    if (!rootState.viewportElement || !rootState.triggerElement || !nextTriggerElement || rootState.triggerElement === nextTriggerElement) {
        return;
    }

    rootState.positionerElement?.removeAttribute('data-instant');
    rootState.popupElement?.removeAttribute('data-instant');
    rootState.viewportElement?.removeAttribute('data-instant');
}

function cleanupRegisteredTriggerElement(rootState, triggerId) {
    const cleanup = rootState.triggerElementCleanups?.get(triggerId);
    if (!cleanup) {
        return;
    }

    cleanup();
    rootState.triggerElementCleanups.delete(triggerId);
}

function cleanupRegisteredTriggerElements(rootState) {
    if (!rootState.triggerElementCleanups) {
        return;
    }

    for (const cleanup of rootState.triggerElementCleanups.values()) {
        cleanup();
    }

    rootState.triggerElementCleanups.clear();
}

function attachRegisteredTriggerInstantClear(rootState, triggerId, element) {
    cleanupRegisteredTriggerElement(rootState, triggerId);

    if (!element) {
        return;
    }

    rootState.triggerElementCleanups ??= new Map();

    const clear = () => {
        enableTriggerChangeTransitions(rootState, element);
    };
    const handleKeyDown = (event) => {
        if (event.key === 'Enter' || event.key === ' ') {
            clear();
        }
    };

    element.addEventListener('pointerdown', clear);
    element.addEventListener('click', clear);
    element.addEventListener('keydown', handleKeyDown);

    rootState.triggerElementCleanups.set(triggerId, () => {
        element.removeEventListener('pointerdown', clear);
        element.removeEventListener('click', clear);
        element.removeEventListener('keydown', handleKeyDown);
    });
}

export function resolveRenderedTriggerId(rootId, activeTriggerId) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !activeTriggerId || !rootState.triggerElements) {
        return null;
    }

    if (rootState.triggerElements.has(activeTriggerId)) {
        return activeTriggerId;
    }

    for (const [registeredId, element] of rootState.triggerElements.entries()) {
        if (element === rootState.triggerElement ||
            (element instanceof HTMLElement && element.id === activeTriggerId)) {
            return registeredId;
        }
    }

    return null;
}

function updateRootPositioner(rootState) {
    if (!rootState.positionerId) return;

    const options = {
        triggerElement: rootState.triggerElement ?? null
    };
    const arrowElement = resolveRootArrowElement(rootState);

    if (arrowElement) {
        options.arrowElement = arrowElement;
    }

    floatingUpdatePositioner(rootState.positionerId, options).catch(() => { });
}

function resolveRootArrowElement(rootState) {
    if (rootState.arrowElement?.isConnected) {
        return rootState.arrowElement;
    }

    const arrowElement = rootState.positionerElement?.querySelector('[data-blazix-base-ui-popover-arrow]') ?? null;
    if (arrowElement) {
        rootState.arrowElement = arrowElement;
    }

    return arrowElement;
}

export function registerTriggerElement(rootId, triggerId, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !triggerId) return;

    if (!rootState.triggerElements) {
        rootState.triggerElements = new Map();
    }

    if (element) {
        rootState.triggerElements.set(triggerId, element);
        attachRegisteredTriggerInstantClear(rootState, triggerId, element);
    } else {
        rootState.triggerElements.delete(triggerId);
        cleanupRegisteredTriggerElement(rootState, triggerId);
    }
}

export function unregisterTriggerElement(rootId, triggerId) {
    const rootState = state.roots.get(rootId);
    if (!rootState?.triggerElements || !triggerId) return;

    rootState.triggerElements.delete(triggerId);
    cleanupRegisteredTriggerElement(rootState, triggerId);
}

export function setPopupElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.popupElement = element;
        cleanupPopupFocusOutListener(rootState);
        if (element) {
            const handleFocusOut = (event) => {
                rootState.lastFocusOutTarget = event.relatedTarget;
                rootState.lastFocusOutFromPopover = true;
                window.setTimeout(() => {
                    if (rootState.lastFocusOutTarget === event.relatedTarget) {
                        rootState.lastFocusOutTarget = null;
                        rootState.lastFocusOutFromPopover = false;
                    }
                }, 250);
            };
            element.addEventListener('focusout', handleFocusOut, true);
            rootState.popupFocusOutCleanup = () => element.removeEventListener('focusout', handleFocusOut, true);
        }
        // Update hover interaction with the new popup element
        if (rootState.hoverInteraction && element) {
            rootState.hoverInteraction.setFloatingElement(element);
        }
    }
}

export function setBackdropElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.backdropElement = element || null;
    syncBackdropHidden(rootState);
}

export function setPositionerElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.positionerElement = element;

    syncScrollLock(rootState);
}

// ============================================================================
// Positioning (delegated to shared floating module)
// ============================================================================

function buildCollisionAvoidance(collisionAvoidanceSide, collisionAvoidanceAlign, collisionAvoidanceFallback) {
    return {
        side: collisionAvoidanceSide || 'flip',
        align: collisionAvoidanceAlign || 'flip',
        fallbackAxisSide: collisionAvoidanceFallback || 'end'
    };
}

export async function initializePositioner(positionerElement, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, disableAnchorTracking, collisionAvoidanceSide, collisionAvoidanceAlign, collisionAvoidanceFallback, dotNetRef, hasSideOffsetFn, hasAlignOffsetFn, hasViewport, rootId) {
    // Build optional position update callback when dotNetRef is provided
    let onPositionUpdated = null;
    if (dotNetRef) {
        onPositionUpdated = (effectiveSide, effectiveAlign, anchorHidden, arrowUncentered) => {
            dotNetRef.invokeMethodAsync('OnPositionUpdated', effectiveSide, effectiveAlign, anchorHidden, arrowUncentered).catch(() => { });
        };
    }

    const positionerId = await floatingInitializePositioner({
        positionerElement,
        triggerElement,
        side,
        align,
        sideOffset,
        alignOffset,
        collisionPadding,
        collisionBoundary: collisionBoundary || 'clipping-ancestors',
        arrowPadding,
        arrowElement,
        sticky: sticky || false,
        positionMethod: positionMethod || 'fixed',
        disableAnchorTracking: disableAnchorTracking || false,
        collisionAvoidance: buildCollisionAvoidance(collisionAvoidanceSide, collisionAvoidanceAlign, collisionAvoidanceFallback),
        onPositionUpdated,
        dotNetRef: dotNetRef || null,
        hasSideOffsetFn: hasSideOffsetFn || false,
        hasAlignOffsetFn: hasAlignOffsetFn || false,
        hasViewport: hasViewport || false
    });

    if (positionerId) {
        state.positioners.set(positionerId, { positionerId, rootId });
        const rootState = state.roots.get(rootId);
        if (rootState) {
            rootState.positionerId = positionerId;
            rootState.positionerElement = positionerElement;
            rootState.arrowElement = arrowElement || resolveRootArrowElement(rootState);
        }
    }

    return positionerId;
}

export async function updatePosition(positionerId, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, collisionAvoidanceSide, collisionAvoidanceAlign, collisionAvoidanceFallback, hasSideOffsetFn, hasAlignOffsetFn, hasViewport) {
    const positionerState = state.positioners.get(positionerId);
    const rootState = positionerState?.rootId ? state.roots.get(positionerState.rootId) : null;
    const resolvedArrowElement = arrowElement || (rootState ? resolveRootArrowElement(rootState) : null);

    if (rootState && resolvedArrowElement) {
        rootState.arrowElement = resolvedArrowElement;
    }

    await floatingUpdatePositioner(positionerId, {
        triggerElement,
        side,
        align,
        sideOffset,
        alignOffset,
        collisionPadding,
        collisionBoundary: collisionBoundary || 'clipping-ancestors',
        arrowPadding,
        arrowElement: resolvedArrowElement,
        sticky: sticky || false,
        positionMethod: positionMethod || 'fixed',
        collisionAvoidance: buildCollisionAvoidance(collisionAvoidanceSide, collisionAvoidanceAlign, collisionAvoidanceFallback),
        hasSideOffsetFn: hasSideOffsetFn || false,
        hasAlignOffsetFn: hasAlignOffsetFn || false,
        hasViewport: hasViewport || false
    });
}

export function disposePositioner(positionerId) {
    floatingDisposePositioner(positionerId);
    state.positioners.delete(positionerId);
}

// ============================================================================
// Internal Backdrop Clip-Path Cutout
// ============================================================================

function updateInternalBackdropCutout(rootState) {
    const backdrop = rootState.internalBackdropElement;
    const trigger = rootState.triggerElement;
    if (!backdrop) return;
    if (!trigger) {
        backdrop.style.clipPath = '';
        return;
    }
    const rect = trigger.getBoundingClientRect();
    backdrop.style.clipPath = `polygon(
        0% 0%, 100% 0%, 100% 100%, 0% 100%, 0% 0%,
        ${rect.left}px ${rect.top}px,
        ${rect.left}px ${rect.bottom}px,
        ${rect.right}px ${rect.bottom}px,
        ${rect.right}px ${rect.top}px,
        ${rect.left}px ${rect.top}px
    )`;
}

function setupBackdropCutoutTracking(rootState) {
    cleanupBackdropCutout(rootState);

    const onUpdate = () => updateInternalBackdropCutout(rootState);
    window.addEventListener('scroll', onUpdate, true);
    window.addEventListener('resize', onUpdate);
    rootState.backdropCutoutCleanup = () => {
        window.removeEventListener('scroll', onUpdate, true);
        window.removeEventListener('resize', onUpdate);
    };

    // Initial update
    onUpdate();
}

function cleanupBackdropCutout(rootState) {
    if (rootState.backdropCutoutCleanup) {
        rootState.backdropCutoutCleanup();
        rootState.backdropCutoutCleanup = null;
    }
    if (rootState.internalBackdropElement) {
        rootState.internalBackdropElement.style.clipPath = '';
    }
}

export function setInternalBackdrop(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;
    rootState.internalBackdropElement = element || null;
    if (element && rootState.isOpen) {
        setupBackdropCutoutTracking(rootState);
    } else {
        cleanupBackdropCutout(rootState);
    }
}

// ============================================================================
// Composite Key Suppression (Toolbar integration)
// ============================================================================

const COMPOSITE_KEYS = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End']);

function setupCompositeKeySuppression(rootState, insideToolbar) {
    if (!insideToolbar) return;
    const popup = rootState.popupElement;
    if (!popup) return;
    const handler = (e) => {
        if (COMPOSITE_KEYS.has(e.key)) {
            e.stopPropagation();
        }
    };
    popup.addEventListener('keydown', handler);
    rootState.compositeKeyCleanup = () => popup.removeEventListener('keydown', handler);
}

// ============================================================================
// Popup Management
// ============================================================================

function resolveFocusMode(mode, element) {
    switch (mode) {
        case 'none': return false;
        case 'element': return element || null;
        case 'default': return null;
        default: return null;
    }
}

export function initializePopup(rootId, popupElement, dotNetRef, modal, initialMode, initialElement, finalMode, finalElement, insideToolbar) {
    if (!popupElement) return;

    // Store modal and focus elements on root state
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.modal = modal || 'false';
        rootState.initialFocusMode = initialMode || null;
        rootState.initialFocusElement = resolveFocusMode(initialMode, initialElement);
        rootState.finalFocusMode = finalMode || null;
        rootState.finalFocusElement = resolveFocusMode(finalMode, finalElement);

        // Set up composite key suppression only when inside a Toolbar
        setupCompositeKeySuppression(rootState, !!insideToolbar);
    }

    const popupState = {
        popupElement,
        dotNetRef
    };

    state.popups.set(popupElement, popupState);

    const updatePopupDimensions = () => {
        if (rootState?.autoResizeObserver) {
            return;
        }

        const width = popupElement.offsetWidth;
        const height = popupElement.offsetHeight;
        popupElement.style.setProperty('--popup-width', `${width}px`);
        popupElement.style.setProperty('--popup-height', `${height}px`);
    };

    updatePopupDimensions();

    if (typeof ResizeObserver !== 'undefined') {
        const resizeObserver = new ResizeObserver(updatePopupDimensions);
        resizeObserver.observe(popupElement);
        popupState.resizeObserver = resizeObserver;
    }
}

export function disposePopup(rootId, popupElement) {
    if (!popupElement) return;
    const popupState = state.popups.get(popupElement);
    if (popupState?.resizeObserver) {
        popupState.resizeObserver.disconnect();
    }
    state.popups.delete(popupElement);

    // Clean up composite key suppression
    const rootState = rootId ? state.roots.get(rootId) : null;
    if (rootState) {
        cleanupPopupFocusOutListener(rootState);
    }
    if (rootState?.compositeKeyCleanup) {
        rootState.compositeKeyCleanup();
        rootState.compositeKeyCleanup = null;
    }
}

// ============================================================================
// Viewport Auto-Resize
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

function applyAnchoringStyles(el, side, direction) {
    if (!el) return;
    // For top/left sides, anchor popup so it grows in the correct direction
    const isRtl = direction === 'rtl';
    if (side === 'top') {
        el.style.position = 'absolute';
        el.style.bottom = '0';
        el.style.top = 'auto';
    } else if (side === 'bottom') {
        el.style.position = '';
        el.style.bottom = '';
        el.style.top = '';
    }
    if ((side === 'left' && !isRtl) || (side === 'right' && isRtl)) {
        el.style.position = 'absolute';
        el.style.right = '0';
        el.style.left = 'auto';
    } else if ((side === 'right' && !isRtl) || (side === 'left' && isRtl)) {
        el.style.position = '';
        el.style.right = '';
        el.style.left = '';
    }
}

function setupPopupAutoResize(rootState) {
    cleanupPopupAutoResize(rootState);

    const { popupElement, positionerElement } = rootState;
    if (!popupElement || !positionerElement || typeof ResizeObserver === 'undefined') return;

    const side = rootState.currentSide || 'bottom';
    const direction = rootState.direction || 'ltr';
    applyAnchoringStyles(popupElement, side, direction);

    const observer = new ResizeObserver((entries) => {
        const entry = entries[0];
        if (entry) {
            rootState.liveDimensions = {
                width: Math.ceil(entry.borderBoxSize[0]?.inlineSize || entry.contentRect.width),
                height: Math.ceil(entry.borderBoxSize[0]?.blockSize || entry.contentRect.height)
            };
        }
    });
    observer.observe(popupElement);

    // Initial measurement
    setPopupCssSize(popupElement, 'auto');
    setPositionerCssSize(positionerElement, 'max-content');
    const dims = getCssDimensions(popupElement);
    rootState.autoResizeCommitted = dims;
    setPositionerCssSize(positionerElement, dims);

    rootState.autoResizeObserver = observer;
}

function cleanupPopupAutoResize(rootState) {
    if (rootState.autoResizeObserver) {
        rootState.autoResizeObserver.disconnect();
        rootState.autoResizeObserver = null;
    }
    rootState.autoResizeCommitted = null;
    rootState.liveDimensions = null;
}

function remeasurePopupAutoResize(rootState) {
    const { popupElement, positionerElement } = rootState;
    if (!popupElement || !positionerElement) return;

    const previousDimensions = rootState.autoResizeCommitted || rootState.liveDimensions;

    setPopupCssSize(popupElement, 'auto');
    setPositionerCssSize(positionerElement, 'max-content');
    const newDimensions = getCssDimensions(popupElement);
    rootState.autoResizeCommitted = newDimensions;

    if (!previousDimensions) {
        setPositionerCssSize(positionerElement, newDimensions);
        return;
    }

    setPopupCssSize(popupElement, previousDimensions);
    setPositionerCssSize(positionerElement, newDimensions);

    requestAnimationFrame(() => {
        setPopupCssSize(popupElement, newDimensions);
        updateRootPositioner(rootState);
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

export function initializeAutoResize(rootId, side, direction) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;
    rootState.currentSide = side || 'bottom';
    rootState.direction = direction || 'ltr';
    setupPopupAutoResize(rootState);
}

export function disposeAutoResize(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        cleanupPopupAutoResize(rootState);
    }
}

// ============================================================================
// Viewport Content Transitions
// ============================================================================

const DIRECTION_TOLERANCE = 5;

function parseCssTimeList(value) {
    return value.split(',').map(part => {
        const time = part.trim();
        if (time.endsWith('ms')) {
            return Number.parseFloat(time) || 0;
        }

        if (time.endsWith('s')) {
            return (Number.parseFloat(time) || 0) * 1000;
        }

        return Number.parseFloat(time) || 0;
    });
}

function getMaxCssTimeMs(durations, delays) {
    const length = Math.max(durations.length, delays.length);
    let maxTime = 0;

    for (let index = 0; index < length; index += 1) {
        const duration = durations[index % durations.length] || 0;
        const delay = delays[index % delays.length] || 0;
        maxTime = Math.max(maxTime, duration + delay);
    }

    return maxTime;
}

function getViewportAnimationFallbackMs(element) {
    const animations = typeof element.getAnimations === 'function'
        ? element.getAnimations()
        : [];
    let maxDuration = 0;

    for (const animation of animations) {
        const timing = animation.effect?.getTiming?.();
        if (!timing) {
            continue;
        }

        const delay = Number(timing.delay) || 0;
        const duration = Number(timing.duration) || 0;
        const iterations = Number.isFinite(timing.iterations) ? timing.iterations : 1;
        maxDuration = Math.max(maxDuration, delay + duration * iterations);
    }

    const style = getComputedStyle(element);
    const maxTransitionDuration = getMaxCssTimeMs(
        parseCssTimeList(style.transitionDuration),
        parseCssTimeList(style.transitionDelay));
    const maxAnimationDuration = getMaxCssTimeMs(
        parseCssTimeList(style.animationDuration),
        parseCssTimeList(style.animationDelay));

    return Math.max(500, maxDuration + 100, maxTransitionDuration + 100, maxAnimationDuration + 100);
}

function waitForViewportAnimations(elements, onFinished) {
    const animationElements = Array.isArray(elements) ? elements : [elements];
    let disposed = false;
    let fallbackId = null;

    const finish = () => {
        if (disposed) {
            return;
        }

        disposed = true;
        if (fallbackId !== null) {
            clearTimeout(fallbackId);
            fallbackId = null;
        }
        onFinished();
    };

    const check = () => {
        if (disposed) {
            return;
        }

        const animations = animationElements.flatMap(element => {
            if (typeof element?.getAnimations !== 'function') {
                return [];
            }

            return element.getAnimations();
        });

        if (animations.length === 0 && animationElements.every(element => typeof element?.getAnimations !== 'function')) {
            finish();
            return;
        }

        if (animations.length === 0) {
            finish();
            return;
        }

        if (fallbackId === null) {
            const fallbackMs = Math.max(...animationElements.map(element => getViewportAnimationFallbackMs(element)));
            fallbackId = setTimeout(finish, fallbackMs);
        }

        const unfinished = animations.filter(animation => animation.pending || animation.playState !== 'finished');
        if (unfinished.length === 0) {
            finish();
            return;
        }

        Promise.all(unfinished.map(animation => animation.finished)).then(finish).catch(() => {
            if (!disposed) {
                requestAnimationFrame(check);
            }
        });
    };

    requestAnimationFrame(check);

    return () => {
        disposed = true;
        if (fallbackId !== null) {
            clearTimeout(fallbackId);
            fallbackId = null;
        }
    };
}

function cleanupViewportTransition(rootState) {
    if (rootState.viewportTransitionCleanup) {
        rootState.viewportTransitionCleanup();
        rootState.viewportTransitionCleanup = null;
    }

    if (rootState.viewportElement) {
        const clones = rootState.viewportElement.querySelectorAll('[data-previous]');
        clones.forEach(clone => clone.remove());
    }

    rootState.pendingViewportTransition = null;
}

export function initializeViewport(rootId, viewportElement, dotNetRef) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.viewportElement = viewportElement;
        rootState.viewportDotNetRef = dotNetRef;
    }
}

export function disposeViewport(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        cleanupViewportTransition(rootState);

        // Remove any leftover cloned elements inside the viewport
        if (rootState.viewportElement) {
            const clones = rootState.viewportElement.querySelectorAll('[data-previous]');
            clones.forEach(clone => clone.remove());
        }
        rootState.viewportElement = null;
        rootState.viewportDotNetRef = null;
    }
}

export function prepareViewportTriggerChange(rootId, previousTriggerElement, newTriggerElement) {
    const rootState = state.roots.get(rootId);
    if (!rootState?.viewportElement || !rootState.viewportDotNetRef) return;

    const viewport = rootState.viewportElement;
    const currentDiv = viewport.querySelector('[data-current]');
    if (!currentDiv) return;

    cleanupViewportTransition(rootState);

    // Clone the inner data-current div as "previous" content
    const clone = currentDiv.cloneNode(true);
    clone.removeAttribute('data-current');
    clone.setAttribute('data-previous', '');
    clone.setAttribute('inert', '');

    // Set dimensions on the clone for CSS transition use
    const width = currentDiv.offsetWidth;
    const height = currentDiv.offsetHeight;
    clone.style.setProperty('--popup-width', `${width}px`);
    clone.style.setProperty('--popup-height', `${height}px`);
    clone.style.position = 'absolute';

    // Calculate activation direction from trigger positions
    const prevRect = previousTriggerElement.getBoundingClientRect();
    const newRect = newTriggerElement.getBoundingClientRect();

    const prevCenterX = prevRect.left + prevRect.width / 2;
    const prevCenterY = prevRect.top + prevRect.height / 2;
    const newCenterX = newRect.left + newRect.width / 2;
    const newCenterY = newRect.top + newRect.height / 2;

    const dx = newCenterX - prevCenterX;
    const dy = newCenterY - prevCenterY;

    // Space-separated dual axis direction matching React
    const horizontal = Math.abs(dx) < DIRECTION_TOLERANCE ? '' : (dx > 0 ? 'right' : 'left');
    const vertical = Math.abs(dy) < DIRECTION_TOLERANCE ? '' : (dy > 0 ? 'down' : 'up');
    const direction = `${horizontal} ${vertical}`.trim() || 'none';

    // Insert clone inside viewport, before the current div
    viewport.insertBefore(clone, currentDiv);
    rootState.pendingViewportTransition = { clone, direction };
    viewport.setAttribute('data-activation-direction', direction);
    viewport.setAttribute('data-transitioning', '');
    viewport.removeAttribute('data-instant');

    // Notify Blazor of transition start
    rootState.viewportDotNetRef.invokeMethodAsync('OnViewportTransitionStart', direction).catch(() => { });
}

export async function completeViewportTriggerChange(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState?.viewportElement || !rootState.viewportDotNetRef) return;

    const pending = rootState.pendingViewportTransition;
    if (!pending) return;

    const viewport = rootState.viewportElement;
    const currentDiv = viewport.querySelector('[data-current]');
    if (!currentDiv) {
        cleanupViewportTransition(rootState);
        return;
    }

    const { clone } = pending;
    if (!clone.isConnected) {
        viewport.insertBefore(clone, currentDiv);
    }

    // Frame 0: only data-starting-style on current (data-ending-style is staggered to next frame)
    currentDiv.setAttribute('data-starting-style', '');

    // Remeasure auto-resize for the new content dimensions
    remeasurePopupAutoResize(rootState);

    // Wait two rAF frames then apply ending/remove starting style attributes
    await requestDoubleAnimationFrame();
    if (rootState.pendingViewportTransition !== pending) {
        return;
    }

    // Staggered: apply data-ending-style on clone and remove data-starting-style from current
    clone.setAttribute('data-ending-style', '');
    currentDiv.removeAttribute('data-starting-style');
    let ended = false;
    let stopWaitingForAnimations = null;
    let cleanup = null;
    const onEnd = () => {
        if (ended) return;
        ended = true;
        cleanup?.();
        if (rootState.viewportTransitionCleanup === cleanup) {
            rootState.viewportTransitionCleanup = null;
        }
        if (rootState.pendingViewportTransition === pending) {
            rootState.pendingViewportTransition = null;
        }
        viewport.removeAttribute('data-activation-direction');
        viewport.removeAttribute('data-transitioning');
        if (rootState.viewportDotNetRef) {
            rootState.viewportDotNetRef.invokeMethodAsync('OnViewportTransitionEnd').catch(() => { });
        }
    };
    cleanup = () => {
        stopWaitingForAnimations?.();
        stopWaitingForAnimations = null;
        clone.remove();
    };

    stopWaitingForAnimations = waitForViewportAnimations([currentDiv, clone], onEnd);
    rootState.viewportTransitionCleanup = cleanup;
}

export async function onViewportTriggerChange(rootId, previousTriggerElement, newTriggerElement) {
    prepareViewportTriggerChange(rootId, previousTriggerElement, newTriggerElement);
    await completeViewportTriggerChange(rootId);
}
