import { acquireScrollLock } from './blazor-baseui-scroll-lock.js';
import { requestDoubleAnimationFrame } from './blazor-baseui-animations.js';
import {
    createFloatingFocusManager,
    disposeFloatingFocusManager,
    checkForTransitionOrAnimation,
    setupTransitionEndListener,
    cleanupTransitionState
} from './blazor-baseui-floating.js';

const STATE_KEY = Symbol.for('BlazorBaseUI.Dialog.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        roots: new Map(),
        dialogStack: [],
        globalListenersInitialized: false
    };
}
const state = window[STATE_KEY];

function initGlobalListeners() {
    if (state.globalListenersInitialized) return;

    document.addEventListener('keydown', handleGlobalKeyDown);
    state.globalListenersInitialized = true;
}

function handleGlobalKeyDown(e) {
    if (e.key !== 'Escape') return;
    if (state.dialogStack.length === 0) return;

    // Close the topmost open dialog
    const topmostRootId = state.dialogStack[state.dialogStack.length - 1];
    const rootState = state.roots.get(topmostRootId);

    if (rootState && rootState.isOpen && rootState.dotNetRef) {
        e.preventDefault();
        e.stopPropagation();
        rootState.dotNetRef.invokeMethodAsync('OnEscapeKey').catch(() => { });
    }
}

export function initializeRoot(rootId, dotNetRef, modal) {
    initGlobalListeners();

    state.roots.set(rootId, {
        rootId,
        dotNetRef,
        isOpen: false,
        modal: modal || 'true',
        triggerElement: null,
        popupElement: null,
        backdropElement: null,
        focusManagerId: null,
        transitionCleanup: null,
        fallbackTimeoutId: null,
        pendingOpen: false,
        releaseScrollLock: null,
        outsideClickCleanup: null,
        backdropClickCleanup: null
    });
}

export function disposeRoot(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        cleanupFocusManager(rootState);
        cleanupTransitionState(rootState);
        cleanupOutsideClick(rootState);
        cleanupBackdropClick(rootState);
        rootState.compositeKeyCleanup?.();
        removeFromDialogStack(rootId);

        // Release scroll lock if this dialog had it
        if (rootState.releaseScrollLock) {
            rootState.releaseScrollLock();
            rootState.releaseScrollLock = null;
        }
    }
    state.roots.delete(rootId);
}

export function setRootOpen(rootId, isOpen, interactionType) {
    const rootState = state.roots.get(rootId);
    if (!rootState) {
        return;
    }

    // Prevent duplicate calls with the same state
    if (rootState.isOpen === isOpen) {
        return;
    }

    rootState.isOpen = isOpen;
    rootState.pendingOpen = isOpen;
    rootState.interactionType = interactionType || null;

    if (isOpen) {
        addToDialogStack(rootId);

        // Lock scroll for modal dialogs (modal === 'true' only, not 'trap-focus')
        if (rootState.modal === 'true') {
            rootState.releaseScrollLock = acquireScrollLock(rootState.popupElement);
        }

        waitForPopupAndStartTransition(rootState, isOpen);
    } else {
        removeFromDialogStack(rootId);

        // Release scroll lock if this dialog acquired it
        if (rootState.releaseScrollLock) {
            rootState.releaseScrollLock();
            rootState.releaseScrollLock = null;
        }

        cleanupOutsideClick(rootState);
        cleanupBackdropClick(rootState);
        cleanupFocusManager(rootState);
        startTransition(rootState, isOpen);
    }
}

function addToDialogStack(rootId) {
    if (!state.dialogStack.includes(rootId)) {
        state.dialogStack.push(rootId);
    }
}

function removeFromDialogStack(rootId) {
    const index = state.dialogStack.indexOf(rootId);
    if (index !== -1) {
        state.dialogStack.splice(index, 1);
    }
}

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

        // Set up outside click listener for non-modal dialogs
        if (rootState.modal === 'false') {
            setupOutsideClickListener(rootState);
        }

        // Set up backdrop click listener for modal dialogs (source: useDialogRoot outsidePress guard)
        if (rootState.modal === 'true' || rootState.modal === 'trap-focus') {
            setupBackdropClickListener(rootState);
        }

        // Focus custom element or default behavior
        focusPopup(rootState, popupElement);

        if (hasTransition) {
            setupTransitionEndListener(rootState, isOpen);
        }

        if (rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnStartingStyleApplied').catch(() => { });
        }
    } else {
        if (hasTransition) {
            setupTransitionEndListener(rootState, isOpen);
        } else {
            if (rootState.dotNetRef) {
                rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
            }
        }

        // Return focus handled by FloatingFocusManager dispose in setRootOpen close path
    }
}

function focusPopup(rootState, popupElement) {
    if (!popupElement) return;

    const mode = rootState.initialFocusMode;

    // Resolve initial focus target for the FocusManager
    let initialFocus = true;

    if (mode === 'none') {
        // Don't move focus, but still initialize FocusManager for trapping/return-focus
        initialFocus = false;
    } else if (mode === 'element' && rootState.initialFocusElement) {
        initialFocus = rootState.initialFocusElement;
    }

    // Resolve return focus target
    const finalMode = rootState.finalFocusMode;
    let returnFocusTarget = true;
    if (finalMode === 'none') {
        returnFocusTarget = false;
    } else if (finalMode === 'element' && rootState.finalFocusElement) {
        returnFocusTarget = rootState.finalFocusElement;
    }

    // iOS Safari hitslop detection
    const isIOS = /iPad|iPhone|iPod/.test(navigator.userAgent) ||
                  (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
    const effectiveInteractionType = rootState.interactionType ||
        (isIOS ? 'touch' : null);

    // Clean up previous focus manager
    cleanupFocusManager(rootState);

    // Delegate to shared FloatingFocusManager
    const isModal = rootState.modal === 'true' || rootState.modal === 'trap-focus';
    const insideElements = [];
    if (rootState.backdropElement) {
        insideElements.push(rootState.backdropElement);
    }
    rootState.focusManagerId = createFloatingFocusManager({
        floatingElement: popupElement,
        triggerElement: rootState.triggerElement,
        modal: isModal,
        initialFocus,
        returnFocus: returnFocusTarget,
        interactionType: effectiveInteractionType || '',
        closeOnFocusOut: rootState.modal === 'false',
        insideElements,
        onClose: rootState.modal === 'false' ? () => {
            if (rootState.isOpen && rootState.dotNetRef) {
                rootState.dotNetRef.invokeMethodAsync('OnFocusOut').catch(() => {});
            }
        } : null
    });
}

function cleanupFocusManager(rootState) {
    if (rootState.focusManagerId) {
        disposeFloatingFocusManager(rootState.focusManagerId, true);
        rootState.focusManagerId = null;
    }
}

function isTopmostDialog(rootId) {
    return state.dialogStack.length === 0 ||
           state.dialogStack[state.dialogStack.length - 1] === rootId;
}

function setupOutsideClickListener(rootState) {
    // Only skip for true modal, not for false or trap-focus
    if (rootState.modal === 'true') return;

    cleanupOutsideClick(rootState);

    const handleOutsideClick = (e) => {
        // Primary button only (left-click)
        if (e.button !== 0) return;

        // Only the topmost dialog responds to outside press
        if (!isTopmostDialog(rootState.rootId)) return;

        const popupElement = rootState.popupElement;
        const triggerElement = rootState.triggerElement;

        if (!popupElement) return;
        if (!rootState.isOpen) return;

        const clickedInsidePopup = popupElement.contains(e.target);
        const clickedOnTrigger = triggerElement && triggerElement.contains(e.target);

        if (!clickedInsidePopup && !clickedOnTrigger && rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnOutsidePress').catch(() => { });
        }
    };

    // Use a small delay to avoid catching the click that opened the dialog
    const timeoutId = setTimeout(() => {
        document.addEventListener('pointerdown', handleOutsideClick);
    }, 0);

    rootState.outsideClickCleanup = () => {
        clearTimeout(timeoutId);
        document.removeEventListener('pointerdown', handleOutsideClick);
    };
}

function cleanupOutsideClick(rootState) {
    if (rootState.outsideClickCleanup) {
        rootState.outsideClickCleanup();
        rootState.outsideClickCleanup = null;
    }
}

// Source: useDialogRoot.ts outsidePress guard — for modal dialogs, clicking the backdrop
// element itself (not the popup) dismisses the dialog. Uses capture-phase 'click' event
// ('intentional' mode in source) so that pointerdown alone does not dismiss.
function setupBackdropClickListener(rootState) {
    cleanupBackdropClick(rootState);

    const handleBackdropClick = (e) => {
        if (e.button !== 0) return;
        if (!rootState.isOpen) return;
        if (!isTopmostDialog(rootState.rootId)) return;

        const target = e.composedPath ? e.composedPath()[0] : e.target;
        const backdropElement = rootState.backdropElement;

        if (!backdropElement) return;

        if (target === backdropElement && rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnOutsidePress').catch(() => { });
        }
    };

    // Use setTimeout(0) to avoid catching the click that opened the dialog
    const timeoutId = setTimeout(() => {
        document.addEventListener('click', handleBackdropClick, true);
    }, 0);

    rootState.backdropClickCleanup = () => {
        clearTimeout(timeoutId);
        document.removeEventListener('click', handleBackdropClick, true);
    };
}

function cleanupBackdropClick(rootState) {
    if (rootState.backdropClickCleanup) {
        rootState.backdropClickCleanup();
        rootState.backdropClickCleanup = null;
    }
}


export function setTriggerElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.triggerElement = element;
    }
}

export function setBackdropElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.backdropElement = element;
    }
}

export function setPopupElement(rootId, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.popupElement = element;
    }
}

const COMPOSITE_KEYS = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End']);

function setupCompositeKeySuppression(rootState) {
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

export function initializePopup(rootId, popupElement, modal, initialFocusMode, initialFocusElement, finalFocusMode, finalFocusElement) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.popupElement = popupElement;
        rootState.modal = modal || 'true';
        rootState.initialFocusMode = initialFocusMode || null;
        rootState.initialFocusElement = initialFocusElement;
        rootState.finalFocusMode = finalFocusMode || null;
        rootState.finalFocusElement = finalFocusElement;
        setupCompositeKeySuppression(rootState);
    }
}

export function setInitialFocusElement(rootId, mode, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.initialFocusMode = mode || null;
        rootState.initialFocusElement = element;

        // If popup is already open and mode is 'element', focus now
        if (rootState.isOpen && mode === 'element' && element) {
            element.focus();
        }
    }
}

export function setFinalFocusElement(rootId, mode, element) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.finalFocusMode = mode || null;
        rootState.finalFocusElement = element;
    }
}

export function disposePopup(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        cleanupFocusManager(rootState);
        rootState.compositeKeyCleanup?.();
        rootState.compositeKeyCleanup = null;
        rootState.popupElement = null;
    }
}
