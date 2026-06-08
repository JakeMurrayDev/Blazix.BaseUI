import {
    detectAnimationType,
    measureDimensions,
    waitForAnimationsToFinish,
    requestAnimationFrameAsync,
    setCssVariables,
    setDataAttribute
} from './blazix-baseui-animations.min.js';

const STATE_KEY = Symbol.for('Blazix.BaseUI.Collapsible.State');
if (!window[STATE_KEY]) {
    window[STATE_KEY] = new WeakMap();
}
const panelStates = window[STATE_KEY];

/**
 * Helper to generate variable names based on a prefix
 */
function getVarNames(prefix) {
    const p = prefix || 'collapsible-panel';
    return {
        height: `--${p}-height`,
        width: `--${p}-width`
    };
}

/**
 * @typedef {Object} PanelState
 * @property {AbortController | null} abortController
 * @property {DotNetObjectReference} dotNetRef
 * @property {string} prefix
 * @property {Function | null} beforeMatchHandler
 * @property {'opening' | 'closing' | 'idle'} animationDirection
 * @property {boolean} isBeforeMatch
 * @property {boolean} cancelInitialTransition
 * @property {boolean} cancelInitialAnimation
 * @property {boolean} keepMounted
 * @property {'height' | 'width' | null} transitionDimension
 * @property {string | null} latestAnimationName
 * @property {Function | null} pendingTemporaryStyleRestore
 */

function getOrCreateState(panel, dotNetRef, prefix) {
    let state = panelStates.get(panel);

    if (!state) {
        state = {
            abortController: null,
            dotNetRef,
            prefix: prefix || 'collapsible-panel',
            beforeMatchHandler: null,
            animationDirection: 'idle',
            isBeforeMatch: false,
            cancelInitialTransition: false,
            cancelInitialAnimation: false,
            keepMounted: false,
            transitionDimension: null,
            latestAnimationName: null,
            pendingTemporaryStyleRestore: null
        };
        panelStates.set(panel, state);
    } else {
        state.dotNetRef = dotNetRef;
        if (prefix) state.prefix = prefix;
    }

    return state;
}

function restorePendingTemporaryStyle(state) {
    if (state.pendingTemporaryStyleRestore) {
        state.pendingTemporaryStyleRestore();
        state.pendingTemporaryStyleRestore = null;
    }
}

function setPendingTemporaryStyleRestore(state, restore) {
    restorePendingTemporaryStyle(state);
    state.pendingTemporaryStyleRestore = () => {
        state.pendingTemporaryStyleRestore = null;
        restore();
    };
}

function setTemporaryStyle(element, property, value) {
    const previousValue = element.style.getPropertyValue(property);
    const previousPriority = element.style.getPropertyPriority(property);

    element.style.setProperty(property, value);

    return () => {
        if (previousValue === '') {
            element.style.removeProperty(property);
            return;
        }

        element.style.setProperty(property, previousValue, previousPriority);
    };
}

/**
 * Aborts any ongoing animation and immediately finalizes the CSS state.
 * This follows Base UI's approach where aborted animations are treated as finished
 * for CSS purposes - the abort only cancels the callback, not the visual state.
 */
function abortAndFinalize(state, panel, vars) {
    if (!state.abortController) {
        return;
    }

    state.abortController.abort();
    state.abortController = null;

    // Clear transition attributes
    setDataAttribute(panel, 'starting-style', false);
    setDataAttribute(panel, 'ending-style', false);

    // Immediately finalize CSS state based on what animation was running
    if (state.animationDirection === 'opening') {
        // Opening was interrupted - set to current measured dimensions
        const dims = measureDimensions(panel);
        setCssVariables(panel, makeDimVars(vars, state.transitionDimension,
            dims.height > 0 ? `${dims.height}px` : 'auto',
            dims.width > 0 ? `${dims.width}px` : 'auto'
        ));
    } else if (state.animationDirection === 'closing') {
        // Closing was interrupted - measure current dimensions
        const dims = measureDimensions(panel);
        setCssVariables(panel, makeDimVars(vars, state.transitionDimension,
            `${dims.height}px`,
            `${dims.width}px`
        ));
    }

    state.animationDirection = 'idle';
}

/**
 * Builds a CSS variable object respecting the detected transition dimension.
 * When transitionDimension is known, only that dimension gets the specified value;
 * the other dimension is set to fallbackOther (default 'auto') to avoid breaking layout.
 * @param {Object} vars - The var names { height, width }
 * @param {'height' | 'width' | null} dimension - The detected transition dimension
 * @param {string} heightVal - The height value
 * @param {string} widthVal - The width value
 * @param {string} [fallbackOther] - Value for the non-transitioning dimension (default: keep as given)
 * @returns {Object} CSS variable map
 */
function makeDimVars(vars, dimension, heightVal, widthVal, fallbackOther) {
    if (!dimension) {
        // Unknown dimension: set both (original behavior)
        return { [vars.height]: heightVal, [vars.width]: widthVal };
    }
    if (dimension === 'height') {
        return {
            [vars.height]: heightVal,
            [vars.width]: fallbackOther !== undefined ? fallbackOther : widthVal
        };
    }
    // dimension === 'width'
    return {
        [vars.height]: fallbackOther !== undefined ? fallbackOther : heightVal,
        [vars.width]: widthVal
    };
}

export function initialize(panel, dotNetRef, initialOpen, prefix, hiddenUntilFound, keepMounted) {
    if (!panel) return;

    const state = getOrCreateState(panel, dotNetRef, prefix);
    const vars = getVarNames(state.prefix);

    state.keepMounted = !!keepMounted;
    // Track whether initial open transition/animation should be suppressed
    // (panels that start open don't need an opening transition or animation)
    state.cancelInitialTransition = !!initialOpen;
    state.cancelInitialAnimation = !!initialOpen;

    // Detect which dimension is being transitioned (height vs width).
    // Setting both to 0px will break layout, so we need to know which one to collapse.
    // Matches React's transitionDimensionRef logic.
    if (state.transitionDimension === null) {
        const panelStyles = getComputedStyle(panel);
        if (
            panel.getAttribute('data-orientation') === 'horizontal' ||
            panelStyles.transitionProperty.indexOf('width') > -1
        ) {
            state.transitionDimension = 'width';
        } else {
            state.transitionDimension = 'height';
        }
    }

    if (initialOpen) {
        // Use auto so the panel can resize naturally when nested content changes
        setCssVariables(panel, {
            [vars.height]: 'auto',
            [vars.width]: 'auto'
        });
    } else {
        setCssVariables(panel, {
            [vars.height]: 'auto',
            [vars.width]: 'auto'
        });

        // When hiddenUntilFound is used and the panel is closed, persist
        // data-starting-style to prevent CSS transitions from firing when the
        // hidden attribute changes to "until-found" (different display properties).
        // Matches React's useCollapsiblePanel behavior.
        if (hiddenUntilFound) {
            const animationType = detectAnimationType(panel);
            if (animationType !== 'css-animation') {
                setDataAttribute(panel, 'starting-style', true);
            }
        }
    }

    // Clear cancelInitialAnimation after one frame (matches React's useOnMount behavior)
    if (state.cancelInitialAnimation) {
        requestAnimationFrame(() => {
            state.cancelInitialAnimation = false;
        });
    }

    // Add beforematch event listener for hidden="until-found" support
    if (!state.beforeMatchHandler) {
        state.beforeMatchHandler = () => {
            state.isBeforeMatch = true;
            invokeBeforeMatch(state.dotNetRef);
        };
        panel.addEventListener('beforematch', state.beforeMatchHandler);
    }
}

export async function open(panel, skipAnimation) {
    const state = panelStates.get(panel);
    if (!state) return;

    const vars = getVarNames(state.prefix);
    const dim = state.transitionDimension;

    // Abort any ongoing animation and finalize its CSS state immediately
    abortAndFinalize(state, panel, vars);

    // Handle beforematch-triggered open: suppress animation so content appears instantly
    // when revealed by browser find-in-page (matches React's isBeforeMatchRef behavior)
    if (state.isBeforeMatch) {
        state.isBeforeMatch = false;
        setDataAttribute(panel, 'ending-style', false);
        setDataAttribute(panel, 'starting-style', false);

        const animationType = detectAnimationType(panel);
        const dims = measureDimensions(panel);
        setCssVariables(panel, makeDimVars(vars, dim, `${dims.height}px`, `${dims.width}px`));

        if (animationType === 'css-transition') {
            const restoreTransitionDuration = setTemporaryStyle(panel, 'transition-duration', '0s');
            setPendingTemporaryStyleRestore(state, restoreTransitionDuration);
        } else if (animationType === 'css-animation') {
            const restoreAnimationName = setTemporaryStyle(panel, 'animation-name', 'none');
            const restoreAnimationDuration = setTemporaryStyle(panel, 'animation-duration', '0s');

            restoreAnimationName();
            setPendingTemporaryStyleRestore(state, restoreAnimationDuration);
        }

        setCssVariables(panel, { [vars.height]: 'auto', [vars.width]: 'auto' });
        state.animationDirection = 'idle';
        invokeAnimationEnded(state.dotNetRef, 'open', true);
        return;
    }

    // Clear any exit transition attributes from previous animations. Preserve
    // data-starting-style from the Blazor render so newly mounted panels start
    // from the authored starting style before the first paint.
    setDataAttribute(panel, 'ending-style', false);

    // Override layout properties that can distort scrollHeight/scrollWidth measurements
    // (matches React's useCollapsiblePanel open measurement behavior)
    const layoutProps = ['justify-content', 'align-items', 'align-content', 'justify-items'];
    const savedLayout = {};
    for (const prop of layoutProps) {
        savedLayout[prop] = panel.style.getPropertyValue(prop);
        panel.style.setProperty(prop, 'initial', 'important');
    }

    // SYNCHRONOUSLY measure and set dimensions FIRST (Base UI approach)
    // This ensures CSS is always in a valid state
    const dims = measureDimensions(panel);
    setCssVariables(panel, makeDimVars(vars, dim, `${dims.height}px`, `${dims.width}px`));

    // Restore layout properties on the next frame (after measurement is consumed)
    requestAnimationFrame(() => {
        for (const [prop, value] of Object.entries(savedLayout)) {
            if (value === '') {
                panel.style.removeProperty(prop);
            } else {
                panel.style.setProperty(prop, value);
            }
        }
    });

    const animationType = detectAnimationType(panel);

    if (skipAnimation || animationType === 'none') {
        setCssVariables(panel, { [vars.height]: 'auto', [vars.width]: 'auto' });
        state.animationDirection = 'idle';
        state.cancelInitialTransition = false;
        state.cancelInitialAnimation = false;
        invokeAnimationEnded(state.dotNetRef, 'open', true);
        return;
    }

    // --- CSS Animation path ---
    // Matches React's useIsoLayoutEffect for css-animation type (useCollapsiblePanel.ts:275-320)
    if (animationType === 'css-animation') {
        // Save and pause animation-name so we can measure without the animation running
        state.latestAnimationName = panel.style.animationName || state.latestAnimationName;
        panel.style.setProperty('animation-name', 'none');

        // Re-measure with animation paused (matches React's setDimensions call)
        const animDims = measureDimensions(panel);
        setCssVariables(panel, makeDimVars(vars, dim, `${animDims.height}px`, `${animDims.width}px`));

        // Restore animation-name unless this is the initial open (suppressed)
        // or a beforematch open. Matches React's shouldCancelInitialOpenAnimationRef guard.
        if (!state.cancelInitialAnimation && !state.isBeforeMatch) {
            panel.style.removeProperty('animation-name');
        }

        state.cancelInitialAnimation = false;

        // Set up abort controller for animation waiting
        const abortController = new AbortController();
        state.abortController = abortController;
        state.animationDirection = 'opening';

        invokeTransitionStatusChanged(state.dotNetRef, 'starting', animDims.height, animDims.width);

        // Wait for CSS animations to complete
        const completed = await waitForAnimationsToFinish(panel, abortController.signal);
        if (!completed) {
            return;
        }

        if (state.abortController === abortController) {
            state.abortController = null;
        }
        state.animationDirection = 'idle';

        setCssVariables(panel, { [vars.height]: 'auto', [vars.width]: 'auto' });
        invokeAnimationEnded(state.dotNetRef, 'open', true);
        return;
    }

    // --- CSS Transition path ---

    // When keepMounted=false and the panel is opened for the first time,
    // set data-starting-style early to ensure CSS transition starting properties
    // are applied before the transition triggers.
    // Matches React's shouldCancelInitialOpenTransitionRef behavior.
    if (!state.cancelInitialTransition && !state.keepMounted) {
        setDataAttribute(panel, 'starting-style', true);
    }

    // Set up abort controller for animation waiting only
    const abortController = new AbortController();
    state.abortController = abortController;
    state.animationDirection = 'opening';
    const signal = abortController.signal;

    // Set starting-style for CSS transition starting point
    setDataAttribute(panel, 'starting-style', true);
    panel.getBoundingClientRect();
    invokeTransitionStatusChanged(state.dotNetRef, 'starting', dims.height, dims.width);
    state.cancelInitialTransition = false;

    // Wait one frame for starting-style to be applied
    const frameOk = await requestAnimationFrameAsync(signal);
    if (!frameOk) {
        // Aborted - CSS state was already finalized by abortAndFinalize
        return;
    }

    // Remove starting-style to trigger the transition
    setDataAttribute(panel, 'starting-style', false);
    panel.getBoundingClientRect();
    invokeTransitionStatusChanged(state.dotNetRef, 'idle', dims.height, dims.width);

    // Wait for animations to complete
    const completed = await waitForAnimationsToFinish(panel, signal);
    if (!completed) {
        // Aborted - CSS state was already finalized by abortAndFinalize
        return;
    }

    // Animation completed successfully
    if (state.abortController === abortController) {
        state.abortController = null;
    }
    state.animationDirection = 'idle';

    // Set to auto for responsive sizing
    setCssVariables(panel, { [vars.height]: 'auto', [vars.width]: 'auto' });

    invokeAnimationEnded(state.dotNetRef, 'open', true);
}

export async function close(panel) {
    const state = panelStates.get(panel);
    if (!state) return;

    const vars = getVarNames(state.prefix);
    const dim = state.transitionDimension;

    restorePendingTemporaryStyle(state);

    // Abort any ongoing animation and finalize its CSS state immediately
    abortAndFinalize(state, panel, vars);

    // Clear starting-style from any previous animation
    setDataAttribute(panel, 'starting-style', false);

    // SYNCHRONOUSLY measure and set dimensions FIRST (Base UI approach)
    // This captures the current open height as the starting point for close animation
    const dims = measureDimensions(panel);
    if (dims.height === 0 && dims.width === 0) {
        // Already closed, nothing to animate
        state.animationDirection = 'idle';
        invokeAnimationEnded(state.dotNetRef, 'close', true);
        return;
    }

    setCssVariables(panel, makeDimVars(vars, dim, `${dims.height}px`, `${dims.width}px`));

    const animationType = detectAnimationType(panel);

    if (animationType === 'none') {
        setCssVariables(panel, { [vars.height]: 'auto', [vars.width]: 'auto' });
        state.animationDirection = 'idle';
        invokeAnimationEnded(state.dotNetRef, 'close', true);
        return;
    }

    // --- CSS Animation path for close ---
    // Matches React's useIsoLayoutEffect css-animation close (useCollapsiblePanel.ts:295-309)
    if (animationType === 'css-animation') {
        // Save and pause animation-name so we can measure without the animation running
        state.latestAnimationName = panel.style.animationName || state.latestAnimationName;
        panel.style.setProperty('animation-name', 'none');

        // Re-measure with animation paused
        const animDims = measureDimensions(panel);
        setCssVariables(panel, makeDimVars(vars, dim, `${animDims.height}px`, `${animDims.width}px`));

        // Restore animation-name to trigger the close animation
        panel.style.removeProperty('animation-name');

        // Set up abort controller for animation waiting
        const abortController = new AbortController();
        state.abortController = abortController;
        state.animationDirection = 'closing';

        invokeTransitionStatusChanged(state.dotNetRef, 'ending', animDims.height, animDims.width);

        // Wait for CSS animations to complete
        const completed = await waitForAnimationsToFinish(panel, abortController.signal);
        if (!completed) {
            return;
        }

        if (state.abortController === abortController) {
            state.abortController = null;
        }
        state.animationDirection = 'idle';

        // React completes close by updating mounted/dimensions state in one render.
        // Leave ending styles and measured dimensions in place until Blazor applies
        // the close-complete render, otherwise the panel can re-expand for a frame.
        invokeAnimationEnded(state.dotNetRef, 'close', true);
        return;
    }

    // --- CSS Transition path for close ---

    // Set up abort controller for animation waiting only
    const abortController = new AbortController();
    state.abortController = abortController;
    state.animationDirection = 'closing';
    const signal = abortController.signal;

    // Wait one frame for dimensions to be applied
    const frameOk = await requestAnimationFrameAsync(signal);
    if (!frameOk) {
        // Aborted - CSS state was already finalized by abortAndFinalize
        return;
    }

    // Set ending-style to trigger the close transition
    setDataAttribute(panel, 'ending-style', true);
    invokeTransitionStatusChanged(state.dotNetRef, 'ending', dims.height, dims.width);

    // Wait for animations to complete
    const completed = await waitForAnimationsToFinish(panel, signal);

    if (!completed) {
        // Aborted - CSS state was already finalized by abortAndFinalize
        return;
    }

    // Animation completed successfully
    if (state.abortController === abortController) {
        state.abortController = null;
    }
    state.animationDirection = 'idle';

    // React completes close by updating mounted/dimensions state in one render.
    // Leave ending styles and measured dimensions in place until Blazor applies
    // the close-complete render, otherwise the panel can re-expand for a frame.
    invokeAnimationEnded(state.dotNetRef, 'close', true);
}

export function dispose(panel) {
    const state = panelStates.get(panel);
    if (!state) return;

    if (state.abortController) {
        state.abortController.abort();
    }

    if (state.beforeMatchHandler) {
        panel.removeEventListener('beforematch', state.beforeMatchHandler);
    }

    restorePendingTemporaryStyle(state);
    panelStates.delete(panel);
}

function invokeTransitionStatusChanged(dotNetRef, status, height, width) {
    try {
        dotNetRef.invokeMethodAsync('OnTransitionStatusChanged', status, height ?? null, width ?? null);
    } catch (e) { }
}

function invokeAnimationEnded(dotNetRef, animationType, completed) {
    try {
        dotNetRef.invokeMethodAsync('OnAnimationEnded', animationType, completed);
    } catch (e) { }
}

function invokeBeforeMatch(dotNetRef) {
    try {
        dotNetRef.invokeMethodAsync('OnBeforeMatch');
    } catch (e) { }
}
