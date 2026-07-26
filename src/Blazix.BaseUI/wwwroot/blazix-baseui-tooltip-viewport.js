const STATE_KEY = Symbol.for('Blazix.BaseUI.TooltipViewport.State');
const MaximumTransitionWait = 2000;

if (!window[STATE_KEY]) {
    window[STATE_KEY] = { viewports: new Map() };
}

const state = window[STATE_KEY];

function getViewportState(viewportId) {
    return state.viewports.get(viewportId);
}

function cloneChildren(element) {
    const wrapper = element.ownerDocument.createElement('div');
    for (const child of Array.from(element.childNodes)) {
        wrapper.appendChild(child.cloneNode(true));
    }
    return wrapper;
}

function calculateRelativePosition(fromElement, toElement) {
    const fromRect = fromElement.getBoundingClientRect();
    const toRect = toElement.getBoundingClientRect();
    return {
        horizontal: (toRect.left + toRect.width / 2) - (fromRect.left + fromRect.width / 2),
        vertical: (toRect.top + toRect.height / 2) - (fromRect.top + fromRect.height / 2)
    };
}

function getActivationDirection(offset) {
    const horizontal = offset.horizontal > 5 ? 'right' : offset.horizontal < -5 ? 'left' : '';
    const vertical = offset.vertical > 5 ? 'down' : offset.vertical < -5 ? 'up' : '';
    return `${horizontal} ${vertical}`.trim();
}

function getDimensions(element) {
    const rect = element.getBoundingClientRect();
    return {
        width: Math.ceil(rect.width),
        height: Math.ceil(rect.height)
    };
}

function setPopupSize(element, dimensions) {
    const width = dimensions === 'auto' ? 'auto' : `${dimensions.width}px`;
    const height = dimensions === 'auto' ? 'auto' : `${dimensions.height}px`;
    element.style.setProperty('--popup-width', width);
    element.style.setProperty('--popup-height', height);
}

function setPositionerSize(element, dimensions) {
    const width = dimensions === 'max-content' ? 'max-content' : `${dimensions.width}px`;
    const height = dimensions === 'max-content' ? 'max-content' : `${dimensions.height}px`;
    element.style.setProperty('--positioner-width', width);
    element.style.setProperty('--positioner-height', height);
}

function overrideStyle(element, property, value) {
    const originalValue = element.style.getPropertyValue(property);
    element.style.setProperty(property, value);
    return () => {
        if (originalValue) {
            element.style.setProperty(property, originalValue);
        } else {
            element.style.removeProperty(property);
        }
    };
}

function measureCurrentContent(viewportState) {
    const popupElement = viewportState.popupElement;
    const positionerElement = viewportState.positionerElement;
    const currentContainer = viewportState.currentContainer;
    const previousContainer = viewportState.previousContainer;

    if (!popupElement || !positionerElement || !currentContainer) {
        return popupElement ? getDimensions(popupElement) : null;
    }

    const restoreCurrentAnimation = overrideStyle(currentContainer, 'animation', 'none');
    const restoreCurrentTransition = overrideStyle(currentContainer, 'transition', 'none');
    const restorePreviousDisplay = previousContainer
        ? overrideStyle(previousContainer, 'display', 'none')
        : () => { };
    const restorePopupPosition = overrideStyle(popupElement, 'position', 'static');
    const restorePopupTransform = overrideStyle(popupElement, 'transform', 'none');
    const restorePopupScale = overrideStyle(popupElement, 'scale', '1');
    const restoreAvailableWidth = overrideStyle(positionerElement, '--available-width', 'max-content');
    const restoreAvailableHeight = overrideStyle(positionerElement, '--available-height', 'max-content');

    setPopupSize(popupElement, 'auto');
    setPositionerSize(positionerElement, 'max-content');
    const dimensions = getDimensions(popupElement);

    restoreAvailableHeight();
    restoreAvailableWidth();
    restorePopupScale();
    restorePopupTransform();
    restorePopupPosition();
    restorePreviousDisplay();
    restoreCurrentTransition();
    restoreCurrentAnimation();

    return dimensions;
}

function cleanupPreviousContainer(viewportState) {
    const previousContainer = viewportState.previousContainer;
    if (!previousContainer) return;

    previousContainer.replaceChildren();
    previousContainer.removeAttribute('data-previous');
    previousContainer.removeAttribute('data-ending-style');
    previousContainer.removeAttribute('inert');
    previousContainer.style.removeProperty('position');
    previousContainer.style.removeProperty('--popup-width');
    previousContainer.style.removeProperty('--popup-height');
}

function cleanupTransitionAttributes(viewportState) {
    viewportState.viewportElement?.removeAttribute('data-activation-direction');
    viewportState.viewportElement?.removeAttribute('data-transitioning');
    viewportState.currentContainer?.removeAttribute('data-starting-style');
    cleanupPreviousContainer(viewportState);
}

function getRunningAnimations(element) {
    if (!element || typeof element.getAnimations !== 'function') return [];

    try {
        return element.getAnimations({ subtree: true })
            .filter(animation => animation.playState !== 'finished' && animation.playState !== 'idle');
    } catch {
        return element.getAnimations()
            .filter(animation => animation.playState !== 'finished' && animation.playState !== 'idle');
    }
}

function waitForAnimationsFinished(elements, signal) {
    const animations = elements.flatMap(getRunningAnimations);
    if (animations.length === 0 || signal.aborted) {
        return Promise.resolve(!signal.aborted);
    }

    return new Promise(resolve => {
        let settled = false;
        const complete = value => {
            if (settled) return;
            settled = true;
            clearTimeout(timeoutId);
            signal.removeEventListener('abort', handleAbort);
            resolve(value);
        };
        const handleAbort = () => complete(false);
        const timeoutId = setTimeout(() => complete(!signal.aborted), MaximumTransitionWait);

        signal.addEventListener('abort', handleAbort, { once: true });
        Promise.allSettled(animations.map(animation => animation.finished))
            .then(() => complete(!signal.aborted));
    });
}

function applyAnchoringStyles(viewportState) {
    viewportState.anchoringCleanup?.();
    viewportState.anchoringCleanup = null;

    const popupElement = viewportState.popupElement;
    const side = viewportState.side;
    if (!popupElement || (side !== 'top' && side !== 'left')) return;

    const restorers = [overrideStyle(popupElement, 'position', 'absolute')];
    if (side === 'top') {
        restorers.push(overrideStyle(popupElement, 'bottom', '0'));
        restorers.push(overrideStyle(popupElement, 'left', '0'));
    } else {
        restorers.push(overrideStyle(popupElement, 'top', '0'));
        restorers.push(overrideStyle(popupElement, 'right', '0'));
    }

    viewportState.anchoringCleanup = () => {
        for (const restore of restorers) restore();
    };
}

function startPreparedTransition(viewportState) {
    const currentContainer = viewportState.currentContainer;
    const previousContainer = viewportState.previousContainer;
    const previousSnapshot = viewportState.pendingSnapshot;
    if (!currentContainer || !previousContainer || !previousSnapshot) return;

    clearTimeout(viewportState.armTimeoutId);
    viewportState.armTimeoutId = null;
    viewportState.transitionArmed = false;
    viewportState.pendingSnapshot = null;
    viewportState.latestSnapshot = cloneChildren(currentContainer);

    previousContainer.replaceChildren(...Array.from(previousSnapshot.childNodes));
    previousContainer.setAttribute('data-previous', '');
    previousContainer.setAttribute('inert', '');
    previousContainer.style.setProperty('position', 'absolute');

    const previousDimensions = viewportState.pendingDimensions
        ?? viewportState.committedDimensions
        ?? (viewportState.popupElement ? getDimensions(viewportState.popupElement) : null);
    if (previousDimensions) {
        previousContainer.style.setProperty('--popup-width', `${previousDimensions.width}px`);
        previousContainer.style.setProperty('--popup-height', `${previousDimensions.height}px`);
    }

    const nextDimensions = measureCurrentContent(viewportState) ?? previousDimensions;
    if (nextDimensions) {
        viewportState.committedDimensions = nextDimensions;
    }

    if (viewportState.popupElement && previousDimensions) {
        setPopupSize(viewportState.popupElement, previousDimensions);
    }
    if (viewportState.positionerElement && nextDimensions) {
        setPositionerSize(viewportState.positionerElement, nextDimensions);
    }

    const direction = viewportState.pendingDirection ?? '';
    viewportState.pendingDirection = null;
    viewportState.pendingDimensions = null;
    viewportState.viewportElement.setAttribute('data-transitioning', '');
    if (direction) {
        viewportState.viewportElement.setAttribute('data-activation-direction', direction);
    } else {
        viewportState.viewportElement.removeAttribute('data-activation-direction');
    }
    currentContainer.setAttribute('data-starting-style', '');

    Promise.resolve().then(() => {
        if (!viewportState.abortController?.signal.aborted) {
            viewportState.dotNetRef.invokeMethodAsync('OnTransitionStarted', direction).catch(() => { });
        }
    });

    const signal = viewportState.abortController.signal;
    requestAnimationFrame(() => {
        if (signal.aborted) return;

        currentContainer.removeAttribute('data-starting-style');
        previousContainer.setAttribute('data-ending-style', '');
        if (viewportState.popupElement && nextDimensions) {
            setPopupSize(viewportState.popupElement, nextDimensions);
        }

        requestAnimationFrame(async () => {
            const completed = await waitForAnimationsFinished([
                currentContainer,
                previousContainer,
                viewportState.popupElement,
                viewportState.positionerElement
            ], signal);
            if (!completed || signal.aborted) return;

            cleanupPreviousContainer(viewportState);
            if (viewportState.popupElement) {
                setPopupSize(viewportState.popupElement, 'auto');
            }
            if (viewportState.positionerElement && viewportState.popupElement) {
                const dimensions = getDimensions(viewportState.popupElement);
                viewportState.committedDimensions = dimensions;
                setPositionerSize(viewportState.positionerElement, dimensions);
            }

            viewportState.viewportElement.removeAttribute('data-activation-direction');
            viewportState.viewportElement.removeAttribute('data-transitioning');
            viewportState.dotNetRef.invokeMethodAsync('OnTransitionEnded').catch(() => { });
            viewportState.abortController = null;
        });
    });
}

function handleCurrentContentChanged(viewportState) {
    viewportState.currentContainer = viewportState.viewportElement.querySelector('[data-current]');
    if (!viewportState.currentContainer) return;

    if (viewportState.transitionArmed) {
        startPreparedTransition(viewportState);
    } else {
        viewportState.latestSnapshot = cloneChildren(viewportState.currentContainer);
    }
}

export function initializeViewport(viewportId, viewportElement, dotNetRef) {
    const currentContainer = viewportElement.querySelector('[data-current]');
    const previousContainer = viewportElement.querySelector('[data-previous-cache]');
    const viewportState = {
        viewportElement,
        currentContainer,
        previousContainer,
        dotNetRef,
        abortController: null,
        contentObserver: null,
        resizeObserver: null,
        popupElement: null,
        positionerElement: null,
        anchoringCleanup: null,
        armTimeoutId: null,
        side: 'top',
        transitionArmed: false,
        pendingSnapshot: null,
        pendingDirection: null,
        pendingDimensions: null,
        latestSnapshot: currentContainer ? cloneChildren(currentContainer) : null,
        committedDimensions: null
    };

    if (currentContainer && typeof MutationObserver === 'function') {
        viewportState.contentObserver = new MutationObserver(() => handleCurrentContentChanged(viewportState));
        viewportState.contentObserver.observe(currentContainer, {
            childList: true,
            characterData: true,
            subtree: true
        });
    }

    state.viewports.set(viewportId, viewportState);
}

export function prepareTransition(viewportId, previousTriggerElement, nextTriggerElement) {
    const viewportState = getViewportState(viewportId);
    if (!viewportState?.currentContainer) return;

    viewportState.abortController?.abort();
    clearTimeout(viewportState.armTimeoutId);
    cleanupTransitionAttributes(viewportState);
    viewportState.abortController = new AbortController();
    viewportState.pendingSnapshot = viewportState.latestSnapshot
        ?? cloneChildren(viewportState.currentContainer);
    viewportState.pendingDirection = getActivationDirection(
        calculateRelativePosition(previousTriggerElement, nextTriggerElement));
    viewportState.pendingDimensions = viewportState.committedDimensions
        ?? (viewportState.popupElement ? getDimensions(viewportState.popupElement) : null);
    viewportState.transitionArmed = true;
    viewportState.armTimeoutId = setTimeout(() => {
        if (!viewportState.transitionArmed) return;

        viewportState.transitionArmed = false;
        viewportState.pendingSnapshot = null;
        viewportState.pendingDirection = null;
        viewportState.pendingDimensions = null;
        viewportState.abortController?.abort();
        viewportState.abortController = null;
        viewportState.armTimeoutId = null;
    }, MaximumTransitionWait);
}

export function initializeAutoResize(viewportId, popupElement, positionerElement, side) {
    const viewportState = getViewportState(viewportId);
    if (!viewportState) return;

    viewportState.popupElement = popupElement;
    viewportState.positionerElement = positionerElement;
    viewportState.side = side;
    applyAnchoringStyles(viewportState);

    const initialDimensions = getDimensions(popupElement);
    viewportState.committedDimensions = initialDimensions;
    setPositionerSize(positionerElement, initialDimensions);

    if (typeof ResizeObserver === 'function') {
        viewportState.resizeObserver = new ResizeObserver(entries => {
            if (viewportState.transitionArmed || viewportState.abortController) return;

            const entry = entries[0];
            if (!entry || !viewportState.positionerElement) return;

            const borderBox = Array.isArray(entry.borderBoxSize)
                ? entry.borderBoxSize[0]
                : entry.borderBoxSize;
            const dimensions = {
                width: Math.ceil(borderBox?.inlineSize ?? entry.contentRect.width),
                height: Math.ceil(borderBox?.blockSize ?? entry.contentRect.height)
            };
            viewportState.committedDimensions = dimensions;
            setPositionerSize(viewportState.positionerElement, dimensions);
        });
        viewportState.resizeObserver.observe(popupElement);
    }
}

export function updateAutoResizeSide(viewportId, side) {
    const viewportState = getViewportState(viewportId);
    if (!viewportState) return;

    viewportState.side = side;
    applyAnchoringStyles(viewportState);
}

export function disposeViewport(viewportId) {
    const viewportState = getViewportState(viewportId);
    if (!viewportState) return;

    viewportState.abortController?.abort();
    clearTimeout(viewportState.armTimeoutId);
    viewportState.contentObserver?.disconnect();
    viewportState.resizeObserver?.disconnect();
    viewportState.anchoringCleanup?.();
    cleanupTransitionAttributes(viewportState);
    state.viewports.delete(viewportId);
}

export function notifyContentChanged(viewportId) {
    const viewportState = getViewportState(viewportId);
    if (!viewportState || viewportState.transitionArmed || viewportState.abortController) return;

    handleCurrentContentChanged(viewportState);
    if (viewportState.popupElement && viewportState.positionerElement) {
        const dimensions = getDimensions(viewportState.popupElement);
        viewportState.committedDimensions = dimensions;
        setPositionerSize(viewportState.positionerElement, dimensions);
    }
}
