/**
 * Blazix.BaseUI PreviewCard Component
 *
 * PreviewCard-specific functionality that builds on the shared floating infrastructure.
 */

import {
    createHoverInteraction,
    createEscapeKeyHandler,
    createDismissInteraction,
    waitForPopupAndStartTransition as floatingWaitForPopup,
    startSimpleTransition,
    disposeHoverInteractionOnRoot,
    updateHoverInteractionFloatingOnRoot,
    setHoverInteractionOpenOnRoot,
    initializePositioner as floatingInitializePositioner,
    updatePositioner as floatingUpdatePositioner,
    disposePositioner as floatingDisposePositioner
} from './blazix-baseui-floating.min.js';

import {
    initialize as initializePopupViewport,
    onTriggerChange as popupViewportTriggerChange,
    contentChanged as popupViewportContentChanged,
    dispose as disposePopupViewport
} from './blazix-baseui-popup-viewport.min.js';

const STATE_KEY = Symbol.for('Blazix.BaseUI.PreviewCard.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        roots: new Map(),
        positioners: new Map(),
        popups: new WeakMap(),
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

export async function initializeHoverInteraction(rootId, triggerId, triggerElement, openDelay, closeDelay) {
    let rootState = state.roots.get(rootId);

    if (!rootState) {
        await new Promise(resolve => setTimeout(resolve, 50));
        rootState = state.roots.get(rootId);
        if (!rootState) return;
    }

    if (triggerElement) {
        rootState.triggerElements.set(triggerId, triggerElement);
        if (!rootState.triggerElement || rootState.activeTriggerId === triggerId) {
            rootState.triggerElement = triggerElement;
        }
    }

    if (!triggerElement) return;

    const existing = rootState.hoverInteractions.get(triggerId);
    if (existing) {
        existing.cleanup();
    }

    const hoverInteraction = createHoverInteraction({
        interactionId: `preview-card-hover-${rootId}-${triggerId}`,
        triggerElement,
        floatingElement: rootState.popupElement,
        openDelay: openDelay || 0,
        closeDelay: closeDelay || 0,
        mouseOnly: true,
        useSafePolygon: true,
        safePolygonOptions: { blockPointerEvents: false },
        onOpen: () => {
            if (rootState.dotNetRef && (!rootState.isOpen || rootState.activeTriggerId !== triggerId)) {
                rootState.activeTriggerId = triggerId;
                rootState.triggerElement = triggerElement;
                applyTriggerOpenAttributes(rootState);
                rootState.dotNetRef.invokeMethodAsync('OnHoverOpen', triggerId).catch(() => { });
                setTimeout(() => applyTriggerOpenAttributes(rootState), 0);
                setTimeout(() => applyTriggerOpenAttributes(rootState), 50);
            }
        },
        onClose: () => {
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

export function updateHoverInteractionDelays(rootId, triggerId, openDelay, closeDelay) {
    const rootState = state.roots.get(rootId);
    rootState?.hoverInteractions.get(triggerId)?.setDelays(openDelay || 0, closeDelay || 0);
}

// ============================================================================
// Dismiss Interaction Support
// ============================================================================

function updateDismissInteraction(rootState) {
    if (!rootState.triggerElement || !rootState.popupElement) return;

    if (rootState.dismissInteraction) {
        rootState.dismissInteraction.cleanup();
        rootState.dismissInteraction = null;
    }

    if (!rootState.isOpen) return;

    rootState.dismissInteraction = createDismissInteraction({
        interactionId: `preview-card-dismiss-${rootState.rootId}`,
        triggerElement: rootState.triggerElement,
        floatingElement: rootState.popupElement,
        escapeKey: false,
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
        viewportElement: null,
        viewportDotNetRef: null,
        viewportSide: 'bottom',
        viewportDirection: 'ltr'
    });
}

export function disposeRoot(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        for (const interaction of rootState.hoverInteractions.values()) {
            interaction.cleanup();
        }
        rootState.hoverInteractions.clear();
        if (rootState.hoverInteraction) {
            rootState.hoverInteraction.cleanup();
        }
        if (rootState.dismissInteraction) {
            rootState.dismissInteraction.cleanup();
        }
        if (rootState.viewportElement) {
            disposePopupViewport(rootState.viewportElement);
        }
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

    if (isOpen) {
        rootState.hoverInteractions.get(rootState.activeTriggerId)?.setOpen(true);
    } else {
        for (const interaction of rootState.hoverInteractions.values()) {
            interaction.setOpen(false);
        }
        rootState.activeTriggerId = null;
        applyTriggerOpenAttributes(rootState);
    }

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

export function setPositionerElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.positionerElement = element;
        setupViewport(rootState);
    }
}

export function setPopupElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.popupElement = element;
        for (const interaction of rootState.hoverInteractions.values()) {
            interaction.setFloatingElement(element);
        }
        if (rootState.hoverInteraction && element) {
            rootState.hoverInteraction.setFloatingElement(element);
        }
        if (rootState.isOpen) {
            updateDismissInteraction(rootState);
        }
        setupViewport(rootState);
    }
}

// ============================================================================
// Positioning
// ============================================================================

function buildCollisionAvoidance(collisionAvoidanceSide, collisionAvoidanceAlign, collisionAvoidanceFallback) {
    return {
        side: collisionAvoidanceSide || 'flip',
        align: collisionAvoidanceAlign || 'flip',
        fallbackAxisSide: collisionAvoidanceFallback || 'end'
    };
}

export async function initializePositioner(positionerElement, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, disableAnchorTracking, collisionAvoidanceSide, collisionAvoidanceAlign, collisionAvoidanceFallback, dotNetRef, hasViewport) {
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
        hasViewport: hasViewport || false
    });

    if (positionerId) {
        state.positioners.set(positionerId, { positionerId });
    }

    return positionerId;
}

export async function updatePosition(positionerId, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, disableAnchorTracking, collisionAvoidanceSide, collisionAvoidanceAlign, collisionAvoidanceFallback, hasViewport) {
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
        disableAnchorTracking: disableAnchorTracking || false,
        collisionAvoidance: buildCollisionAvoidance(collisionAvoidanceSide, collisionAvoidanceAlign, collisionAvoidanceFallback),
        hasViewport: hasViewport || false
    });
}

export function disposePositioner(positionerId) {
    floatingDisposePositioner(positionerId);
    state.positioners.delete(positionerId);
}

// ============================================================================
// Popup Management
// ============================================================================

export function initializePopup(popupElement) {
    if (!popupElement) return;

    state.popups.set(popupElement, { popupElement });
}

export function disposePopup(popupElement) {
    if (!popupElement) return;
    state.popups.delete(popupElement);
}

// ============================================================================
// Viewport Management
// ============================================================================

function setupViewport(rootState) {
    if (!rootState?.viewportElement) {
        return;
    }

    initializePopupViewport(rootState.viewportElement, {
        dotNetRef: rootState.viewportDotNetRef,
        popupElement: rootState.popupElement,
        positionerElement: rootState.positionerElement,
        side: rootState.viewportSide || 'bottom',
        direction: rootState.viewportDirection || 'ltr',
        cssVars: { popupWidth: '--popup-width', popupHeight: '--popup-height' }
    });
}

export function initializeViewport(rootId, viewportElement, dotNetRef) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.viewportElement = viewportElement;
    rootState.viewportDotNetRef = dotNetRef;
    setupViewport(rootState);
}

export function initializeViewportAutoResize(rootId, side, direction) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.viewportSide = side || 'bottom';
    rootState.viewportDirection = direction || 'ltr';
    setupViewport(rootState);
}

export function disposeViewportAutoResize(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState?.viewportElement) {
        disposePopupViewport(rootState.viewportElement);
    }
}

export function disposeViewport(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        if (rootState.viewportElement) {
            disposePopupViewport(rootState.viewportElement);
        }
        rootState.viewportElement = null;
        rootState.viewportDotNetRef = null;
    }
}

export function onViewportTriggerChange(rootId, previousTriggerElement, newTriggerElement) {
    const rootState = state.roots.get(rootId);
    if (!rootState?.viewportElement) return;

    popupViewportTriggerChange(rootState.viewportElement, previousTriggerElement, newTriggerElement);
}

export function notifyViewportContentChanged(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState?.viewportElement) return;

    popupViewportContentChanged(rootState.viewportElement);
}
