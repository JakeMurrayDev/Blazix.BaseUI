/**
 * BlazorBaseUI Popover Component
 *
 * Popover-specific functionality that builds on the shared floating infrastructure.
 */

import { acquireScrollLock } from './blazor-baseui-scroll-lock.js';
import { requestDoubleAnimationFrame } from './blazor-baseui-animations.js';

const PATIENT_CLICK_THRESHOLD = 500;

import {
    TABBABLE_SELECTOR,
    getTabbableElements,
    getPreviousTabbable,
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
} from './blazor-baseui-floating.js';

const STATE_KEY = Symbol.for('BlazorBaseUI.Popover.State');

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

    // Store the trigger element if provided
    if (triggerElement) {
        rootState.triggerElement = triggerElement;
    }

    if (!rootState.triggerElement) return;

    // Clean up existing hover interaction
    if (rootState.hoverInteraction) {
        rootState.hoverInteraction.cleanup();
    }

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
}

export function disposeHoverInteraction(rootId) {
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

/**
 * Redirects focus into the popover content.
 * Finds the FloatingFocusManager's before-content guard inside the positioner
 * and focuses it, which in turn redirects to the first tabbable in the popup.
 * Used by the trigger's post-focus guard on Tab forward.
 */
export function focusPopoverContent(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState?.positionerElement) return;

    const beforeContentGuard = rootState.positionerElement.querySelector(
        '[data-blazor-base-ui-focus-guard-type="inside"]'
    );
    if (beforeContentGuard) {
        beforeContentGuard.focus();
        return;
    }

    const tabbable = getTabbableElements(rootState.positionerElement);
    if (tabbable.length > 0) tabbable[0].focus();
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
    const { triggerElement, popupElement, positionerElement } = rootState;
    return (positionerElement && positionerElement.contains(target))
        || (popupElement && popupElement.contains(target))
        || (triggerElement && triggerElement.contains(target));
}

/**
 * Shared outside-press processing loop.
 * @param {Event} e - The pointer or click event.
 * @param {function} rootFilter - Predicate selecting which roots to process.
 * @param {boolean} checkDragOut - When true, suppress if pointerdown started inside.
 */
function processOutsidePressForRoots(e, rootFilter, checkDragOut) {
    const openRoots = [];
    for (const [id, rootState] of state.roots) {
        if (rootState.isOpen && rootState.dotNetRef && rootFilter(rootState)) {
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
        positionerElement: null,
        popupElement: null,
        internalBackdropElement: null,
        hoverInteraction: null,
        focusTrapCleanup: null,
        focusManagerId: null,
        focusOutCleanup: null,
        releaseScrollLock: null,
        initialFocusElement: null,
        finalFocusElement: null,
        transitionCleanup: null,
        fallbackTimeoutId: null,
        pendingOpen: false,
        openReason: null,
        interactionType: null,
        openOrderStamp: 0,
        stickIfOpen: false,
        patientClickTimeout: null,
        compositeKeyCleanup: null,
        viewportElement: null,
        viewportDotNetRef: null,
        backdropCutoutCleanup: null
    });
}

export function disposeRoot(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        // Clean up hover interaction
        if (rootState.hoverInteraction) {
            rootState.hoverInteraction.cleanup();
        }

        // Clean up patient click timeout
        clearPatientClickTimeout(rootState);

        // Clean up focus management
        cleanupFocusTrap(rootState);
        cleanupFocusOutListener(rootState);

        // Clean up composite key suppression
        rootState.compositeKeyCleanup?.();

        // Clean up backdrop cutout
        cleanupBackdropCutout(rootState);

        // Clean up transition listener
        cleanupTransitionState(rootState);

        // Clean up viewport
        rootState.viewportDotNetRef = null;
        rootState.viewportElement = null;

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

    if (modal === 'true' && !rootState.releaseScrollLock) {
        rootState.releaseScrollLock = acquireScrollLock(rootState.positionerElement);
    } else if (modal !== 'true' && rootState.releaseScrollLock) {
        rootState.releaseScrollLock();
        rootState.releaseScrollLock = null;
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
        rootState.openOrderStamp = ++state.openOrderCounter;
    }

    // Sync with hover interaction
    if (rootState.hoverInteraction) {
        rootState.hoverInteraction.setOpen(isOpen);
    }

    if (isOpen) {
        // Start patient click protection when hover-opened
        if (reason === 'trigger-hover') {
            clearPatientClickTimeout(rootState);
            rootState.stickIfOpen = true;
            rootState.patientClickTimeout = setTimeout(() => {
                rootState.stickIfOpen = false;
                rootState.patientClickTimeout = null;
            }, PATIENT_CLICK_THRESHOLD);
        }

        // Acquire scroll lock for modal popovers (skip for hover-opened and touch)
        if (rootState.modal === 'true' && reason !== 'trigger-hover' && rootState.interactionType !== 'touch') {
            rootState.releaseScrollLock = acquireScrollLock(rootState.positionerElement);
        }

        // Set up backdrop cutout tracking if internal backdrop is present
        if (rootState.internalBackdropElement) {
            setupBackdropCutoutTracking(rootState);
        }

        waitForPopupAndStartTransition(rootState, isOpen);
    } else {
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

        startTransition(rootState, isOpen);
    }
}

function clearPatientClickTimeout(rootState) {
    if (rootState.patientClickTimeout !== null) {
        clearTimeout(rootState.patientClickTimeout);
        rootState.patientClickTimeout = null;
    }
    rootState.stickIfOpen = false;
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

export function setInitialFocusElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.initialFocusElement = element;

        // If popup is already open, focus the element now
        if (rootState.isOpen && element) {
            element.focus();
        }
    }
}

export function setFinalFocusElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.finalFocusElement = element;
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
        rootState.triggerElement = element;
    }
}

export function setPopupElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.popupElement = element;
        // Update hover interaction with the new popup element
        if (rootState.hoverInteraction && element) {
            rootState.hoverInteraction.setFloatingElement(element);
        }
    }
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

export async function initializePositioner(positionerElement, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, disableAnchorTracking, collisionAvoidanceSide, collisionAvoidanceAlign, collisionAvoidanceFallback, dotNetRef, hasSideOffsetFn, hasAlignOffsetFn, hasViewport) {
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
        state.positioners.set(positionerId, { positionerId });
    }

    return positionerId;
}

export async function updatePosition(positionerId, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, collisionAvoidanceSide, collisionAvoidanceAlign, collisionAvoidanceFallback, hasSideOffsetFn, hasAlignOffsetFn, hasViewport) {
    await floatingUpdatePositioner(positionerId, {
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
        rootState.initialFocusElement = resolveFocusMode(initialMode, initialElement);
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
        // Remove any leftover cloned elements inside the viewport
        if (rootState.viewportElement) {
            const clones = rootState.viewportElement.querySelectorAll('[data-previous]');
            clones.forEach(clone => clone.remove());
        }
        rootState.viewportElement = null;
        rootState.viewportDotNetRef = null;
    }
}

export async function onViewportTriggerChange(rootId, previousTriggerElement, newTriggerElement) {
    const rootState = state.roots.get(rootId);
    if (!rootState?.viewportElement || !rootState.viewportDotNetRef) return;

    const viewport = rootState.viewportElement;
    const currentDiv = viewport.querySelector('[data-current]');
    if (!currentDiv) return;

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

    // Frame 0: only data-starting-style on current (data-ending-style is staggered to next frame)
    currentDiv.setAttribute('data-starting-style', '');

    // Insert clone inside viewport, before the current div
    viewport.insertBefore(clone, currentDiv);

    // Notify Blazor of transition start
    rootState.viewportDotNetRef.invokeMethodAsync('OnViewportTransitionStart', direction).catch(() => { });

    // Remeasure auto-resize for the new content dimensions
    remeasurePopupAutoResize(rootState);

    // Wait two rAF frames then apply ending/remove starting style attributes
    await requestDoubleAnimationFrame();

    // Staggered: apply data-ending-style on clone and remove data-starting-style from current
    clone.setAttribute('data-ending-style', '');
    currentDiv.removeAttribute('data-starting-style');
    let ended = false;
    const onEnd = (event) => {
        if (event && event.target !== clone) return;
        if (ended) return;
        ended = true;
        clone.removeEventListener('transitionend', onEnd);
        clone.removeEventListener('animationend', onEnd);
        clearTimeout(fallbackId);
        clone.remove();
        if (rootState.viewportDotNetRef) {
            rootState.viewportDotNetRef.invokeMethodAsync('OnViewportTransitionEnd').catch(() => { });
        }
    };

    clone.addEventListener('transitionend', onEnd);
    clone.addEventListener('animationend', onEnd);

    // Fallback timeout in case no transition/animation fires
    const fallbackId = setTimeout(onEnd, 500);
}
