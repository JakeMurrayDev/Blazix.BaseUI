/**
 * Blazix.BaseUI Tooltip Component
 *
 * Tooltip-specific functionality that builds on the shared floating infrastructure.
 */

import {
    createHoverInteraction,
    createEscapeKeyHandler,
    createDismissInteraction,
    createVirtualElement,
    updateVirtualElement,
    disposeVirtualElement,
    waitForPopupAndStartTransition as floatingWaitForPopup,
    startSimpleTransition,
    disposeHoverInteractionOnRoot,
    updateHoverInteractionFloatingOnRoot,
    setHoverInteractionOpenOnRoot,
    initializePositioner as floatingInitializePositioner,
    updatePositioner as floatingUpdatePositioner,
    disposePositioner as floatingDisposePositioner
} from './blazix-baseui-floating.min.js';

const STATE_KEY = Symbol.for('Blazix.BaseUI.Tooltip.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        roots: new Map(),
        positioners: new Map(),
        globalListenersInitialized: false
    };
}
const state = window[STATE_KEY];

const handleGlobalKeyDown = createEscapeKeyHandler(state.roots, 'OnEscapeKey');

function initGlobalListeners() {
    if (state.globalListenersInitialized) return;

    document.addEventListener('keydown', handleGlobalKeyDown);
    state.globalListenersInitialized = true;
}

// ============================================================================
// Hover Interaction Support
// ============================================================================

export async function initializeHoverInteraction(rootId, triggerId, triggerElement, openDelay, closeDelay, disableHoverablePopup) {
    let rootState = state.roots.get(rootId);

    // If root state doesn't exist yet, wait briefly for it to be initialized
    if (!rootState) {
        await new Promise(resolve => setTimeout(resolve, 50));
        rootState = state.roots.get(rootId);
        if (!rootState) return;
    }

    // Store the trigger element if provided
    if (triggerElement) {
        rootState.triggerElements.set(triggerId, triggerElement);
        if (!rootState.triggerElement || rootState.activeTriggerId === triggerId) {
            rootState.triggerElement = triggerElement;
        }
    }

    if (!triggerElement) return;

    // Clean up existing hover interaction
    const existing = rootState.hoverInteractions.get(triggerId);
    if (existing) {
        existing.cleanup();
    }

    const hoverInteraction = createHoverInteraction({
        interactionId: `tooltip-hover-${rootId}-${triggerId}`,
        triggerElement,
        floatingElement: rootState.popupElement,
        openDelay: openDelay || 0,
        closeDelay: closeDelay || 0,
        mouseOnly: true,
        // Use safePolygon when hoverable popup is enabled (disableHoverablePopup=false)
        useSafePolygon: !disableHoverablePopup,
        safePolygonOptions: { blockPointerEvents: false },
        onOpen: (reason) => {
            if (rootState.dotNetRef && (!rootState.isOpen || rootState.activeTriggerId !== triggerId)) {
                rootState.activeTriggerId = triggerId;
                rootState.triggerElement = triggerElement;
                applyTriggerOpenAttributes(rootState);
                rootState.dotNetRef.invokeMethodAsync('OnHoverOpen', triggerId).catch(() => { });
                setTimeout(() => applyTriggerOpenAttributes(rootState), 0);
                setTimeout(() => applyTriggerOpenAttributes(rootState), 50);
            }
        },
        onClose: (reason) => {
            if (rootState.dotNetRef && rootState.isOpen && (!rootState.activeTriggerId || rootState.activeTriggerId === triggerId)) {
                rootState.dotNetRef.invokeMethodAsync('OnHoverClose', triggerId).catch(() => { });
            }
        }
    });

    rootState.hoverInteractions.set(triggerId, hoverInteraction);
}

export function disposeHoverInteraction(rootId, triggerId = null) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    if (triggerId == null) {
        for (const interaction of rootState.hoverInteractions.values()) {
            interaction.cleanup();
        }
        rootState.hoverInteractions.clear();
        disposeHoverInteractionOnRoot(state.roots, rootId);
        return;
    }

    const interaction = rootState.hoverInteractions.get(triggerId);
    if (interaction) {
        interaction.cleanup();
        rootState.hoverInteractions.delete(triggerId);
    }
    rootState.triggerElements.delete(triggerId);
}

export function updateHoverInteractionFloatingElement(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState?.popupElement) {
        return;
    }

    for (const interaction of rootState.hoverInteractions.values()) {
        interaction.setFloatingElement(rootState.popupElement);
    }
    updateHoverInteractionFloatingOnRoot(state.roots, rootId);
}

export function setHoverInteractionOpen(rootId, isOpen) {
    const rootState = state.roots.get(rootId);
    if (!rootState) {
        return;
    }

    if (!isOpen) {
        for (const interaction of rootState.hoverInteractions.values()) {
            interaction.setOpen(false);
        }
    } else {
        rootState.hoverInteractions.get(rootState.activeTriggerId)?.setOpen(true);
    }
    setHoverInteractionOpenOnRoot(state.roots, rootId, isOpen);
}

export function cancelPendingHoverOpen(rootId, triggerId) {
    const rootState = state.roots.get(rootId);
    rootState?.hoverInteractions.get(triggerId)?.cancelPendingOpen?.();
}

export function updateHoverInteractionDelays(rootId, triggerId, openDelay, closeDelay) {
    const rootState = state.roots.get(rootId);
    rootState?.hoverInteractions.get(triggerId)?.setDelays(openDelay || 0, closeDelay || 0);
}

// ============================================================================
// Dismiss Interaction Support
// ============================================================================

function updateDismissInteraction(rootState) {
    if (!rootState.triggerElement || !rootState.popupElement) return;

    // Dispose existing if any
    if (rootState.dismissInteraction) {
        rootState.dismissInteraction.cleanup();
        rootState.dismissInteraction = null;
    }

    // Only create when open
    if (!rootState.isOpen) return;

    rootState.dismissInteraction = createDismissInteraction({
        interactionId: `tooltip-dismiss-${rootState.rootId}`,
        triggerElement: rootState.triggerElement,
        floatingElement: rootState.popupElement,
        escapeKey: false, // Already handled by global escape key handler
        outsidePress: true,
        onDismiss: (reason) => {
            if (reason === 'outside-press' && rootState.dotNetRef) {
                rootState.dotNetRef.invokeMethodAsync('OnOutsidePress').catch(() => { });
            }
        }
    });
}

function applyTriggerOpenAttributes(rootState) {
    for (const [triggerId, triggerElement] of rootState.triggerElements) {
        const currentTriggerElement = document.getElementById(triggerId) || triggerElement;
        if (rootState.isOpen && rootState.activeTriggerId === triggerId) {
            currentTriggerElement.setAttribute('data-popup-open', '');
        } else {
            currentTriggerElement.removeAttribute('data-popup-open');
        }
    }
}

// ============================================================================
// Root Management
// ============================================================================

export function initializeRoot(rootId, dotNetRef) {
    initGlobalListeners();

    state.roots.set(rootId, {
        rootId,
        dotNetRef,
        isOpen: false,
        activeTriggerId: null,
        triggerElement: null,
        triggerElements: new Map(),
        positionerElement: null,
        popupElement: null,
        hoverInteraction: null,
        hoverInteractions: new Map(),
        dismissInteraction: null,
        cursorTrackingCleanup: null,
        virtualAnchor: null,
        positionerId: null
    });
}

export function disposeRoot(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        // Clean up hover interaction
        for (const interaction of rootState.hoverInteractions.values()) {
            interaction.cleanup();
        }
        rootState.hoverInteractions.clear();
        if (rootState.hoverInteraction) {
            rootState.hoverInteraction.cleanup();
        }
        // Clean up dismiss interaction
        if (rootState.dismissInteraction) {
            rootState.dismissInteraction.cleanup();
        }
        // Clean up cursor tracking
        disposeCursorTrackingInternal(rootState);
    }
    state.roots.delete(rootId);
}

export function setRootOpen(rootId, isOpen, activeTriggerId = null) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    if (activeTriggerId) {
        rootState.activeTriggerId = activeTriggerId;
        rootState.triggerElement = rootState.triggerElements.get(activeTriggerId) || rootState.triggerElement;
    }

    rootState.isOpen = isOpen;
    rootState.pendingOpen = isOpen;
    applyTriggerOpenAttributes(rootState);

    // Sync with hover interaction
    if (isOpen) {
        rootState.hoverInteractions.get(rootState.activeTriggerId)?.setOpen(true);
    } else {
        for (const interaction of rootState.hoverInteractions.values()) {
            interaction.setOpen(false);
        }
        rootState.activeTriggerId = null;
        applyTriggerOpenAttributes(rootState);
    }

    // Update dismiss interaction based on open state
    if (isOpen) {
        updateDismissInteraction(rootState);
    } else if (rootState.dismissInteraction) {
        rootState.dismissInteraction.cleanup();
        rootState.dismissInteraction = null;
    }

    if (isOpen) {
        floatingWaitForPopup(rootState, isOpen, startSimpleTransition);
    } else {
        startSimpleTransition(rootState, isOpen);
    }
}

// ============================================================================
// Element References
// ============================================================================

export function setTriggerElement(rootId, triggerId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.activeTriggerId = triggerId;
        rootState.triggerElement = element;
        rootState.triggerElements.set(triggerId, element);
        applyTriggerOpenAttributes(rootState);
        if (rootState.isOpen) {
            updateDismissInteraction(rootState);
        }
    }
}

export function syncTriggerOpenAttributes(rootId, isOpen, activeTriggerId = null) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.isOpen = isOpen;
    rootState.activeTriggerId = activeTriggerId;
    if (activeTriggerId) {
        rootState.triggerElement = rootState.triggerElements.get(activeTriggerId) || rootState.triggerElement;
    }
    applyTriggerOpenAttributes(rootState);
}

export function setPopupElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.popupElement = element;
        // Update hover interaction with the new popup element
        for (const interaction of rootState.hoverInteractions.values()) {
            interaction.setFloatingElement(element);
        }
        if (rootState.hoverInteraction && element) {
            rootState.hoverInteraction.setFloatingElement(element);
        }
        if (rootState.isOpen) {
            updateDismissInteraction(rootState);
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

export function setPositionerId(rootId, positionerId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.positionerId = positionerId;
    }
}

export async function initializePositioner(positionerElement, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, disableAnchorTracking, collisionAvoidanceSide, collisionAvoidanceAlign, collisionAvoidanceFallback, dotNetRef, virtualId, hasSideOffsetFn, hasAlignOffsetFn, hasViewport) {
    // Build optional position update callback when dotNetRef is provided
    let onPositionUpdated = null;
    if (dotNetRef) {
        onPositionUpdated = (effectiveSide, effectiveAlign, anchorHidden, arrowUncentered) => {
            dotNetRef.invokeMethodAsync('OnPositionUpdated', effectiveSide, effectiveAlign, anchorHidden, arrowUncentered).catch(() => { });
        };
    }

    const positionerId = await floatingInitializePositioner({
        positionerElement,
        triggerElement: virtualId ? null : triggerElement,
        virtualId,
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
    const options = {
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
    };
    // Only include triggerElement when provided, so virtual anchor is not overwritten
    if (triggerElement) {
        options.triggerElement = triggerElement;
    }
    await floatingUpdatePositioner(positionerId, options);
}

export function disposePositioner(positionerId) {
    floatingDisposePositioner(positionerId);
    state.positioners.delete(positionerId);
}

// ============================================================================
// Cursor Tracking
// ============================================================================

function disposeCursorTrackingInternal(rootState) {
    if (rootState.clientPointInteraction) {
        rootState.clientPointInteraction.dispose();
        rootState.clientPointInteraction = null;
    }
    if (rootState.virtualAnchor) {
        disposeVirtualElement(rootState.virtualAnchor.virtualId);
        rootState.virtualAnchor = null;
    }
    rootState.cursorTrackingCleanup?.();
    rootState.cursorTrackingCleanup = null;
    rootState.virtualId = null;
}

export function initializeCursorTracking(rootId, axis) {
    const rootState = state.roots.get(rootId);
    if (!rootState || !rootState.triggerElement) return null;

    // Clean up any existing cursor tracking
    disposeCursorTrackingInternal(rootState);

    // Create a virtual element at the trigger's center position
    const triggerRect = rootState.triggerElement.getBoundingClientRect();
    const centerX = triggerRect.x + triggerRect.width / 2;
    const centerY = triggerRect.y + triggerRect.height / 2;

    const virtualAnchor = createVirtualElement(centerX, centerY);
    rootState.virtualAnchor = virtualAnchor;

    // Set up mousemove listener on the trigger element to update virtual element
    function onMouseMove(e) {
        const newRect = rootState.triggerElement.getBoundingClientRect();

        const newX = axis === 'y' ? newRect.x + newRect.width / 2 : e.clientX;
        const newY = axis === 'x' ? newRect.y + newRect.height / 2 : e.clientY;

        updateVirtualElement(virtualAnchor.virtualId, newX, newY);

        // Trigger position re-computation using stored positioner ID
        if (rootState.positionerId) {
            floatingUpdatePositioner(rootState.positionerId, {});
        }
    }

    rootState.triggerElement.addEventListener('mousemove', onMouseMove);

    rootState.cursorTrackingCleanup = () => {
        rootState.triggerElement?.removeEventListener('mousemove', onMouseMove);
    };

    return virtualAnchor.virtualId;
}

export function disposeCursorTracking(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;
    disposeCursorTrackingInternal(rootState);
}
