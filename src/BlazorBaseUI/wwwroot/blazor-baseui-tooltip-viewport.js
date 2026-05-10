const STATE_KEY = Symbol.for('BlazorBaseUI.TooltipViewport.State');
if (!window[STATE_KEY]) {
    window[STATE_KEY] = { viewports: new Map() };
}
const state = window[STATE_KEY];

function getViewportState(viewportId) {
    return state.viewports.get(viewportId);
}

/**
 * Calculate relative position between two element centers.
 * @param {Element} fromEl
 * @param {Element} toEl
 * @returns {{ horizontal: number, vertical: number }}
 */
function calculateRelativePosition(fromEl, toEl) {
    const fromRect = fromEl.getBoundingClientRect();
    const toRect = toEl.getBoundingClientRect();
    return {
        horizontal: (toRect.left + toRect.width / 2) - (fromRect.left + fromRect.width / 2),
        vertical: (toRect.top + toRect.height / 2) - (fromRect.top + fromRect.height / 2)
    };
}

/**
 * Convert an offset to a human-readable activation direction string.
 * @param {{ horizontal: number, vertical: number } | null} offset
 * @returns {string | undefined}
 */
function getActivationDirection(offset) {
    if (!offset) return undefined;
    const h = offset.horizontal > 5 ? 'right' : offset.horizontal < -5 ? 'left' : '';
    const v = offset.vertical > 5 ? 'down' : offset.vertical < -5 ? 'up' : '';
    return `${h} ${v}`.trim() || undefined;
}

/**
 * Wait for all CSS animations/transitions on an element to finish.
 * @param {Element} element
 * @param {AbortSignal} signal
 * @returns {Promise<void>}
 */
function waitForAnimationsFinished(element, signal) {
    return new Promise((resolve) => {
        if (!element || typeof element.getAnimations !== 'function') {
            resolve();
            return;
        }

        function exec() {
            const animations = element.getAnimations();
            if (animations.length === 0) {
                resolve();
                return;
            }
            Promise.all(animations.map(a => a.finished))
                .then(() => {
                    if (!signal?.aborted) resolve();
                })
                .catch(() => {
                    // Animations may be aborted when properties change mid-animation.
                    // Re-check for new animations.
                    const current = element.getAnimations();
                    if (!signal?.aborted && current.length > 0 &&
                        current.some(a => a.pending || a.playState !== 'finished')) {
                        exec();
                    } else if (!signal?.aborted) {
                        resolve();
                    }
                });
        }

        // Wait for data-starting-style to be removed before checking animations
        if (element.hasAttribute('data-starting-style')) {
            const observer = new MutationObserver(() => {
                if (!element.hasAttribute('data-starting-style')) {
                    observer.disconnect();
                    requestAnimationFrame(() => exec());
                }
            });
            observer.observe(element, { attributes: true, attributeFilter: ['data-starting-style'] });
            signal?.addEventListener('abort', () => observer.disconnect(), { once: true });
        } else {
            requestAnimationFrame(() => exec());
        }
    });
}

/**
 * Initialize the viewport.
 * @param {string} viewportId
 * @param {Element} viewportElement
 * @param {object} dotNetRef
 */
export function initializeViewport(viewportId, viewportElement, dotNetRef) {
    const vs = {
        viewportElement,
        dotNetRef,
        abortController: null,
        resizeObserver: null,
        popupElement: null,
        positionerElement: null,
        side: 'top'
    };
    state.viewports.set(viewportId, vs);
}

/**
 * Begin a transition between triggers.
 * Clones current content, inserts as data-previous, then waits for animations.
 * @param {string} viewportId
 * @param {Element} previousTriggerElement
 * @param {Element} newTriggerElement
 */
export function beginTransition(viewportId, previousTriggerElement, newTriggerElement) {
    const vs = getViewportState(viewportId);
    if (!vs || !vs.viewportElement) return;

    // Abort any in-progress transition
    if (vs.abortController) {
        vs.abortController.abort();
    }
    vs.abortController = new AbortController();
    const signal = vs.abortController.signal;

    const currentContainer = vs.viewportElement.querySelector('[data-current]');
    if (!currentContainer) return;

    // Remove any existing previous container
    const existingPrevious = vs.viewportElement.querySelector('[data-previous]');
    if (existingPrevious) {
        existingPrevious.remove();
    }

    // Freeze popup dimensions during transition
    if (vs.popupElement) {
        const rect = vs.popupElement.getBoundingClientRect();
        vs.popupElement.style.setProperty('--popup-width', `${Math.ceil(rect.width)}px`);
        vs.popupElement.style.setProperty('--popup-height', `${Math.ceil(rect.height)}px`);
    }

    // Clone current content
    const previousContainer = document.createElement('div');
    previousContainer.setAttribute('data-previous', '');
    previousContainer.setAttribute('inert', '');
    previousContainer.style.position = 'absolute';

    for (const child of Array.from(currentContainer.childNodes)) {
        previousContainer.appendChild(child.cloneNode(true));
    }

    // Set frozen dimensions on previous container
    if (vs.popupElement) {
        const rect = vs.popupElement.getBoundingClientRect();
        previousContainer.style.setProperty('--popup-width', `${Math.ceil(rect.width)}px`);
        previousContainer.style.setProperty('--popup-height', `${Math.ceil(rect.height)}px`);
    }

    // Insert before current
    vs.viewportElement.insertBefore(previousContainer, currentContainer);

    // Calculate activation direction
    const offset = calculateRelativePosition(previousTriggerElement, newTriggerElement);
    const direction = getActivationDirection(offset);

    // Notify C# that transition started
    try {
        vs.dotNetRef.invokeMethodAsync('OnTransitionStarted', direction || '');
    } catch (e) { /* circuit disconnect */ }

    // Set transition style attributes
    currentContainer.setAttribute('data-starting-style', '');
    previousContainer.setAttribute('data-ending-style', '');

    // Next frame: remove starting-style, wait for animations, cleanup
    requestAnimationFrame(() => {
        if (signal.aborted) return;

        currentContainer.removeAttribute('data-starting-style');

        waitForAnimationsFinished(currentContainer, signal).then(() => {
            if (signal.aborted) return;

            // Remove previous container
            previousContainer.remove();

            // Unfreeze popup dimensions
            if (vs.popupElement) {
                vs.popupElement.style.setProperty('--popup-width', 'auto');
                vs.popupElement.style.setProperty('--popup-height', 'auto');
            }

            // Notify C#
            try {
                vs.dotNetRef.invokeMethodAsync('OnTransitionEnded');
            } catch (e) { /* circuit disconnect */ }

            vs.abortController = null;
        });
    });
}

/**
 * Initialize auto-resize behavior on the popup.
 * @param {string} viewportId
 * @param {Element} popupElement
 * @param {Element} positionerElement
 * @param {string} side
 */
export function initializeAutoResize(viewportId, popupElement, positionerElement, side) {
    const vs = getViewportState(viewportId);
    if (!vs) return;

    vs.popupElement = popupElement;
    vs.positionerElement = positionerElement;
    vs.side = side;
    vs.anchoringCleanup = null;

    // Apply anchoring styles for correct growth direction on origin sides
    const isOriginSide = side === 'top' || side === 'left';
    if (isOriginSide && popupElement) {
        const anchorProp = side === 'top' ? 'bottom' : 'right';
        popupElement.style.setProperty('position', 'absolute');
        popupElement.style.setProperty(anchorProp, '0');
        vs.anchoringCleanup = () => {
            popupElement.style.removeProperty('position');
            popupElement.style.removeProperty(anchorProp);
        };
    }

    if (typeof ResizeObserver === 'function' && popupElement) {
        vs.resizeObserver = new ResizeObserver((entries) => {
            const entry = entries[0];
            if (entry && vs.positionerElement) {
                const width = Math.ceil(entry.borderBoxSize[0].inlineSize);
                const height = Math.ceil(entry.borderBoxSize[0].blockSize);
                vs.positionerElement.style.setProperty('--positioner-width', `${width}px`);
                vs.positionerElement.style.setProperty('--positioner-height', `${height}px`);
            }
        });
        vs.resizeObserver.observe(popupElement);
    }
}

/**
 * Update the side for anchoring style computation.
 * @param {string} viewportId
 * @param {string} side
 */
export function updateAutoResizeSide(viewportId, side) {
    const vs = getViewportState(viewportId);
    if (vs) {
        vs.side = side;
    }
}

/**
 * Clean up all viewport state.
 * @param {string} viewportId
 */
export function disposeViewport(viewportId) {
    const vs = getViewportState(viewportId);
    if (!vs) return;

    if (vs.abortController) {
        vs.abortController.abort();
    }

    if (vs.resizeObserver) {
        vs.resizeObserver.disconnect();
    }

    if (vs.anchoringCleanup) {
        vs.anchoringCleanup();
    }

    // Remove any lingering previous container
    if (vs.viewportElement) {
        const prev = vs.viewportElement.querySelector('[data-previous]');
        if (prev) prev.remove();
    }

    state.viewports.delete(viewportId);
}

/**
 * Re-measure and update popup/positioner dimensions after content changes.
 * @param {string} viewportId
 */
export function notifyContentChanged(viewportId) {
    const vs = getViewportState(viewportId);
    if (!vs || !vs.popupElement || !vs.positionerElement) return;

    const rect = vs.popupElement.getBoundingClientRect();
    const width = Math.ceil(rect.width);
    const height = Math.ceil(rect.height);
    vs.positionerElement.style.setProperty('--positioner-width', `${width}px`);
    vs.positionerElement.style.setProperty('--positioner-height', `${height}px`);
}
