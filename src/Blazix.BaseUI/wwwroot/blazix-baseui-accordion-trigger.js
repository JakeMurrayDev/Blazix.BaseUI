const STATE_KEY = Symbol.for('Blazix.BaseUI.AccordionTrigger.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = new WeakMap();
}

const state = window[STATE_KEY];

function getState(element) {
    return state.get(element);
}

function setState(element, newState) {
    state.set(element, newState);
}

function isElementDisabled(element) {
    if (!element) return true;
    if (element.disabled) return true;
    if (element.getAttribute('aria-disabled') === 'true') return true;
    return false;
}

function handleKeyDown(event) {
    const element = event.currentTarget;
    const s = getState(element);
    if (!s) return;

    if (isElementDisabled(element)) return;

    if (event.key === ' ') {
        event.preventDefault();
        return;
    }

    if (!s.isNativeButton && event.key === 'Enter') {
        event.preventDefault();
        element.click();
        return;
    }
}

function handleKeyUp(event) {
    const element = event.currentTarget;
    const s = getState(element);
    if (!s) return;

    if (isElementDisabled(element)) return;

    if (event.key === ' ') {
        event.preventDefault();
        element.click();
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

export function initialize(element, isNativeButton) {
    if (!element) return;

    setState(element, { isNativeButton });
    element.addEventListener('keydown', handleKeyDown);
    element.addEventListener('keyup', handleKeyUp);
    element.addEventListener('pointerdown', handlePointerDown);
    element.addEventListener('mousedown', handleMouseDown);
}

export function updateConfig(element, isNativeButton) {
    if (!element) return;
    const s = getState(element);
    if (!s) return;
    s.isNativeButton = isNativeButton;
}

export function dispose(element) {
    if (!element) return;

    element.removeEventListener('keydown', handleKeyDown);
    element.removeEventListener('keyup', handleKeyUp);
    element.removeEventListener('pointerdown', handlePointerDown);
    element.removeEventListener('mousedown', handleMouseDown);
    state.delete(element);
}
