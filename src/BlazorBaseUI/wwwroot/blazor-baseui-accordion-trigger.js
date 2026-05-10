const STATE_KEY = Symbol.for('BlazorBaseUI.AccordionTrigger.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = new WeakMap();
}

const state = window[STATE_KEY];

const ARROW_UP = 'ArrowUp';
const ARROW_DOWN = 'ArrowDown';
const ARROW_LEFT = 'ArrowLeft';
const ARROW_RIGHT = 'ArrowRight';
const HOME = 'Home';
const END = 'End';

const SUPPORTED_KEYS = new Set([ARROW_UP, ARROW_DOWN, ARROW_LEFT, ARROW_RIGHT, HOME, END]);

function getState(element) {
    return state.get(element);
}

function setState(element, newState) {
    state.set(element, newState);
}

function stopEvent(event) {
    event.preventDefault();
    event.stopPropagation();
}

function isElementDisabled(element) {
    if (!element) return true;
    if (element.disabled) return true;
    if (element.getAttribute('aria-disabled') === 'true') return true;
    return false;
}

function getActiveTriggers(triggerElement) {
    const accordionRoot = triggerElement.closest('[data-blazor-base-ui-accordion-root]');
    if (!accordionRoot) return [];

    const items = accordionRoot.querySelectorAll('[data-index]');
    const activeTriggers = [];

    for (const item of items) {
        if (isElementDisabled(item)) continue;
        if (item.closest('[data-blazor-base-ui-accordion-root]') !== accordionRoot) continue;

        const trigger = item.querySelector('[type="button"], [role="button"]');
        if (trigger && !isElementDisabled(trigger)) {
            if (!activeTriggers.includes(trigger)) {
                activeTriggers.push(trigger);
            }
        }
    }

    return activeTriggers;
}

function handleKeyDown(event) {
    const element = event.currentTarget;
    const s = getState(element);
    if (!s) return;

    if (isElementDisabled(element)) return;

    if (event.key === ' ') {
        event.preventDefault();
        element.click();
        return;
    }

    if (!s.isNativeButton && event.key === 'Enter') {
        event.preventDefault();
        element.click();
        return;
    }

    if (!SUPPORTED_KEYS.has(event.key)) {
        return;
    }

    stopEvent(event);

    const { isHorizontal, isRtl, loopFocus } = s;
    const triggers = getActiveTriggers(element);
    const numTriggers = triggers.length;

    if (numTriggers === 0) return;

    const lastIndex = numTriggers - 1;
    const thisIndex = triggers.indexOf(element);

    if (thisIndex === -1) return;

    let nextIndex = -1;

    function toNext() {
        if (loopFocus) {
            nextIndex = thisIndex + 1 > lastIndex ? 0 : thisIndex + 1;
        } else {
            nextIndex = Math.min(thisIndex + 1, lastIndex);
        }
    }

    function toPrev() {
        if (loopFocus) {
            nextIndex = thisIndex === 0 ? lastIndex : thisIndex - 1;
        } else {
            nextIndex = thisIndex - 1;
        }
    }

    switch (event.key) {
        case ARROW_DOWN:
            if (!isHorizontal) {
                toNext();
            }
            break;
        case ARROW_UP:
            if (!isHorizontal) {
                toPrev();
            }
            break;
        case ARROW_RIGHT:
            if (isHorizontal) {
                if (isRtl) {
                    toPrev();
                } else {
                    toNext();
                }
            }
            break;
        case ARROW_LEFT:
            if (isHorizontal) {
                if (isRtl) {
                    toNext();
                } else {
                    toPrev();
                }
            }
            break;
        case HOME:
            nextIndex = 0;
            break;
        case END:
            nextIndex = lastIndex;
            break;
        default:
            break;
    }

    if (nextIndex > -1) {
        triggers[nextIndex].focus();
    }
}

function handleKeyUp(event) {
    if (event.key === ' ') {
        event.preventDefault();
    }
}

function handlePointerDown(event) {
    if (isElementDisabled(event.currentTarget)) {
        event.preventDefault();
    }
}

function handleMouseDown(event) {
    if (isElementDisabled(event.currentTarget)) {
        event.preventDefault();
    }
}

export function initialize(element, isHorizontal, isRtl, loopFocus, isNativeButton) {
    if (!element) return;

    setState(element, { isHorizontal, isRtl, loopFocus, isNativeButton });
    element.addEventListener('keydown', handleKeyDown);
    element.addEventListener('keyup', handleKeyUp);
    element.addEventListener('pointerdown', handlePointerDown);
    element.addEventListener('mousedown', handleMouseDown);
}

export function updateConfig(element, isHorizontal, isRtl, loopFocus) {
    if (!element) return;
    const s = getState(element);
    if (!s) return;
    s.isHorizontal = isHorizontal;
    s.isRtl = isRtl;
    s.loopFocus = loopFocus;
}

export function dispose(element) {
    if (!element) return;

    element.removeEventListener('keydown', handleKeyDown);
    element.removeEventListener('keyup', handleKeyUp);
    element.removeEventListener('pointerdown', handlePointerDown);
    element.removeEventListener('mousedown', handleMouseDown);
    state.delete(element);
}
