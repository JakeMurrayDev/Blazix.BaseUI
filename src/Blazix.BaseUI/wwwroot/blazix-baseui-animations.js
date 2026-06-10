const STATE_KEY = Symbol.for('Blazix.BaseUI.Animations.State');
if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        initialized: true
    };
}

/**
 * Detects the animation type used by an element.
 * @param {HTMLElement} element
 * @returns {'css-transition' | 'css-animation' | 'none'}
 */
export function detectAnimationType(element) {
    if (!element) return 'none';

    const styles = getComputedStyle(element);
    const hasAnimation = styles.animationName
        .split(',')
        .map((name) => name.trim())
        .some((name) => name !== '' && name !== 'none') && hasNonZeroDuration(styles.animationDuration);
    const hasTransition = hasNonZeroDuration(styles.transitionDuration);

    if (hasAnimation && !hasTransition) {
        return 'css-animation';
    }
    if (hasTransition && !hasAnimation) {
        return 'css-transition';
    }
    if (hasAnimation && hasTransition) {
        console.warn('[Blazix.BaseUI] Both CSS transitions and animations detected. Only one should be used.');
        return 'css-transition';
    }
    return 'none';
}

function hasNonZeroDuration(value) {
    return value
        .split(',')
        .map((part) => part.trim())
        .some((part) => part !== '' && Number.parseFloat(part) > 0);
}

/**
 * Detects which dimension is being transitioned (height or width).
 * @param {HTMLElement} element
 * @returns {'height' | 'width' | null}
 */
export function detectTransitionDimension(element) {
    if (!element) return null;

    const styles = getComputedStyle(element);
    const transitionProperty = styles.transitionProperty;

    if (transitionProperty.includes('height') || transitionProperty === 'all') {
        return 'height';
    }
    if (transitionProperty.includes('width')) {
        return 'width';
    }
    return null;
}

/**
 * Measures the scroll dimensions of an element.
 * @param {HTMLElement} element
 * @returns {{ height: number, width: number }}
 */
export function measureDimensions(element) {
    if (!element) return { height: 0, width: 0 };
    return {
        height: element.scrollHeight,
        width: element.scrollWidth
    };
}

/**
 * Waits for all animations on an element to finish.
 * @param {HTMLElement} element
 * @param {AbortSignal | null} signal
 * @returns {Promise<boolean>} - Returns true if completed, false if aborted
 */
export async function waitForAnimationsToFinish(element, signal = null) {
    if (!element || typeof element.getAnimations !== 'function') {
        return true;
    }

    const frameOk = await requestAnimationFrameAsync(signal);
    if (!frameOk) {
        return false;
    }

    const animations = element.getAnimations();
    if (animations.length === 0) {
        return true;
    }

    try {
        await Promise.all(animations.map(anim => anim.finished));
        return !(signal?.aborted);
    } catch {
        if (signal?.aborted) {
            return false;
        }

        const currentAnimations = element.getAnimations();
        if (currentAnimations.some(anim => anim.pending || anim.playState !== 'finished')) {
            return waitForAnimationsToFinish(element, signal);
        }

        return true;
    }
}

/**
 * Requests an animation frame and returns a promise.
 * @param {AbortSignal | null} signal
 * @returns {Promise<boolean>}
 */
export function requestAnimationFrameAsync(signal = null) {
    return new Promise((resolve) => {
        const frameId = requestAnimationFrame(() => {
            resolve(!(signal?.aborted));
        });

        if (signal) {
            signal.addEventListener('abort', () => {
                cancelAnimationFrame(frameId);
                resolve(false);
            }, { once: true });
        }
    });
}

/**
 * Requests two animation frames (for style application).
 * @param {AbortSignal | null} signal
 * @returns {Promise<boolean>}
 */
export async function requestDoubleAnimationFrame(signal = null) {
    const first = await requestAnimationFrameAsync(signal);
    if (!first) return false;
    return requestAnimationFrameAsync(signal);
}

function invokeDotNet(dotNetRef, methodName, ...args) {
    if (!dotNetRef) return;

    dotNetRef.invokeMethodAsync(methodName, ...args).catch(() => { });
}

/**
 * Keeps data-starting-style in place for one rendered frame, then notifies .NET
 * to clear the starting transition status.
 * @param {HTMLElement} element
 * @param {object} dotNetRef
 */
export function applyStartingStyle(element, dotNetRef) {
    if (!element) {
        invokeDotNet(dotNetRef, 'OnTransitionStarted');
        return;
    }

    requestAnimationFrame(() => {
        invokeDotNet(dotNetRef, 'OnTransitionStarted');
    });
}

/**
 * Waits for an element's exit animations/transitions to finish before notifying
 * .NET that it can unmount the element.
 * @param {HTMLElement} element
 * @param {object} dotNetRef
 */
export function waitForExitTransition(element, dotNetRef) {
    if (!element) {
        invokeDotNet(dotNetRef, 'OnTransitionEnded');
        return;
    }

    requestAnimationFrame(() => {
        waitForElementAnimations(element, dotNetRef);
    });
}

function waitForElementAnimations(element, dotNetRef) {
    if (typeof element.getAnimations !== 'function' || globalThis.BASE_UI_ANIMATIONS_DISABLED) {
        invokeDotNet(dotNetRef, 'OnTransitionEnded');
        return;
    }

    const animations = element.getAnimations();

    Promise.all(animations.map(animation => animation.finished))
        .then(() => {
            invokeDotNet(dotNetRef, 'OnTransitionEnded');
        })
        .catch(() => {
            const currentAnimations = element.getAnimations();
            const hasRunningAnimations = currentAnimations.some(animation =>
                animation.pending || animation.playState !== 'finished');

            if (hasRunningAnimations) {
                waitForElementAnimations(element, dotNetRef);
            } else {
                invokeDotNet(dotNetRef, 'OnTransitionEnded');
            }
        });
}

/**
 * Sets CSS custom properties on an element.
 * @param {HTMLElement} element
 * @param {Record<string, string | null>} properties
 */
export function setCssVariables(element, properties) {
    if (!element) return;
    for (const [key, value] of Object.entries(properties)) {
        if (value === null) {
            element.style.removeProperty(key);
        } else {
            element.style.setProperty(key, value);
        }
    }
}

/**
 * Sets or removes a data attribute on an element.
 * @param {HTMLElement} element
 * @param {string} attribute - The attribute name without 'data-' prefix
 * @param {boolean} present
 */
export function setDataAttribute(element, attribute, present) {
    if (!element) return;
    const fullAttr = attribute.startsWith('data-') ? attribute : `data-${attribute}`;
    if (present) {
        element.setAttribute(fullAttr, '');
    } else {
        element.removeAttribute(fullAttr);
    }
}
