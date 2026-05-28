const STATE_KEY = Symbol.for('BlazorBaseUI.Radio.State');
const GROUP_STATE_KEY = Symbol.for('BlazorBaseUI.RadioGroup.State');

if (!window[GROUP_STATE_KEY]) {
    window[GROUP_STATE_KEY] = new WeakMap();
}
const groupState = window[GROUP_STATE_KEY];

export function initialize(element, inputElement, disabled, readOnly, nativeButton) {
    if (!element) {
        return;
    }

    const state = {
        inputElement,
        disabled,
        readOnly,
        nativeButton,
        keydownHandler: null
    };

    // Set up keyboard handler that prevents default for arrow keys
    state.keydownHandler = (e) => {
        if (state.disabled || state.readOnly) {
            return;
        }

        // Arrow keys should prevent default to stop browser scrolling
        const arrowKeys = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'];
        
        if (arrowKeys.includes(e.key)) {
            e.preventDefault();
        }
        
        // Space key should also prevent default (page scroll)
        if (e.key === ' ' || e.key === 'Enter') {
            e.preventDefault();
        }
    };

    element.addEventListener('keydown', state.keydownHandler);
    element[STATE_KEY] = state;
}

export function updateState(element, inputElement, disabled, readOnly, nativeButton) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        state.disabled = disabled;
        state.readOnly = readOnly;
        state.nativeButton = nativeButton;
        state.inputElement = inputElement;
    }
}

export function focus(element) {
    if (!element) {
        return;
    }

    element.focus({ preventScroll: true });
}

export function dispose(element) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        if (state.keydownHandler) {
            element.removeEventListener('keydown', state.keydownHandler);
        }
        delete element[STATE_KEY];
    }
}

export function initializeGroup(element, dotNetRef) {
    if (!element) {
        return;
    }

    const state = {
        element,
        dotNetRef,
        items: new Set(),
        keydownCaptureHandler: null
    };

    state.keydownCaptureHandler = async (e) => {
        const arrowKeys = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'];

        if (arrowKeys.includes(e.key)) {
            e.preventDefault();

            const currentElement = getRadioFromEventTarget(element, e.target);
            if (!currentElement || isRadioDisabled(currentElement)) {
                return;
            }

            await dotNetRef.invokeMethodAsync('OnArrowKeyPressed');

            if (e.key === 'ArrowUp' || e.key === 'ArrowLeft') {
                await navigateToPrevious(element, currentElement);
            } else {
                await navigateToNext(element, currentElement);
            }
        }

        if (e.key === ' ') {
            e.preventDefault();
        }
    };

    element.addEventListener('keydown', state.keydownCaptureHandler, { capture: true });
    groupState.set(element, state);
}

export function disposeGroup(element) {
    if (!element) {
        return;
    }

    const state = groupState.get(element);
    if (state) {
        if (state.keydownCaptureHandler) {
            element.removeEventListener('keydown', state.keydownCaptureHandler, { capture: true });
        }
        groupState.delete(element);
    }
}

export function registerRadio(groupElement, radioElement, value, isNullValue, serializedValue) {
    if (!groupElement || !radioElement) {
        return;
    }

    const state = groupState.get(groupElement);
    if (!state) {
        return;
    }

    for (const item of state.items) {
        if (item.element === radioElement) {
            item.value = value;
            item.isNullValue = isNullValue;
            item.serializedValue = serializedValue;
            updateTabIndexes(groupElement);
            return;
        }
    }

    state.items.add({ element: radioElement, value, isNullValue, serializedValue });
    updateTabIndexes(groupElement);
}

function updateTabIndexes(groupElement) {
    const items = getOrderedRadios(groupElement);
    if (items.length === 0) {
        return;
    }

    const hasChecked = items.some(item => item.element.getAttribute('aria-checked') === 'true');
    const firstEnabled = items.find(item => !isRadioDisabled(item.element));

    for (const item of items) {
        const isChecked = item.element.getAttribute('aria-checked') === 'true';
        const isDisabled = isRadioDisabled(item.element);

        if (isDisabled) {
            item.element.tabIndex = -1;
        } else if (isChecked) {
            item.element.tabIndex = 0;
        } else if (!hasChecked && item === firstEnabled) {
            item.element.tabIndex = 0;
        } else {
            item.element.tabIndex = -1;
        }
    }
}

export function unregisterRadio(groupElement, radioElement) {
    if (!groupElement || !radioElement) {
        return;
    }

    const state = groupState.get(groupElement);
    if (!state) {
        return;
    }

    for (const item of state.items) {
        if (item.element === radioElement) {
            state.items.delete(item);
            updateTabIndexes(groupElement);
            return;
        }
    }
}

function getOrderedRadios(groupElement) {
    const state = groupState.get(groupElement);
    if (!state) {
        return [];
    }

    const items = Array.from(state.items).filter(item => document.contains(item.element));
    items.sort((a, b) => {
        const position = a.element.compareDocumentPosition(b.element);
        if (position & Node.DOCUMENT_POSITION_FOLLOWING) return -1;
        if (position & Node.DOCUMENT_POSITION_PRECEDING) return 1;
        return 0;
    });
    return items;
}

function isRadioDisabled(radioElement) {
    return radioElement.hasAttribute('data-disabled');
}

function isRadioReadOnly(radioElement) {
    return radioElement.hasAttribute('data-readonly');
}

function getRadioFromEventTarget(groupElement, target) {
    if (!(target instanceof Element)) {
        return null;
    }

    const radioElement = target.closest('[data-radio-item]');
    if (!radioElement || !groupElement.contains(radioElement)) {
        return null;
    }

    return radioElement;
}

export async function navigateToPrevious(groupElement, currentElement) {
    const state = groupState.get(groupElement);
    if (!state) {
        return false;
    }

    const items = getOrderedRadios(groupElement);
    const currentIndex = items.findIndex(item => item.element === currentElement);
    if (currentIndex < 0) {
        return false;
    }

    for (let i = currentIndex - 1; i >= 0; i--) {
        if (!isRadioDisabled(items[i].element)) {
            items[i].element.focus({ preventScroll: true });
            if (!isRadioReadOnly(items[i].element)) {
                await state.dotNetRef.invokeMethodAsync('OnNavigateToRadio', items[i].value, items[i].isNullValue === true, items[i].serializedValue);
            }
            return true;
        }
    }

    for (let i = items.length - 1; i > currentIndex; i--) {
        if (!isRadioDisabled(items[i].element)) {
            items[i].element.focus({ preventScroll: true });
            if (!isRadioReadOnly(items[i].element)) {
                await state.dotNetRef.invokeMethodAsync('OnNavigateToRadio', items[i].value, items[i].isNullValue === true, items[i].serializedValue);
            }
            return true;
        }
    }

    return false;
}

export async function navigateToNext(groupElement, currentElement) {
    const state = groupState.get(groupElement);
    if (!state) {
        return false;
    }

    const items = getOrderedRadios(groupElement);
    const currentIndex = items.findIndex(item => item.element === currentElement);
    if (currentIndex < 0) {
        return false;
    }

    for (let i = currentIndex + 1; i < items.length; i++) {
        if (!isRadioDisabled(items[i].element)) {
            items[i].element.focus({ preventScroll: true });
            if (!isRadioReadOnly(items[i].element)) {
                await state.dotNetRef.invokeMethodAsync('OnNavigateToRadio', items[i].value, items[i].isNullValue === true, items[i].serializedValue);
            }
            return true;
        }
    }

    for (let i = 0; i < currentIndex; i++) {
        if (!isRadioDisabled(items[i].element)) {
            items[i].element.focus({ preventScroll: true });
            if (!isRadioReadOnly(items[i].element)) {
                await state.dotNetRef.invokeMethodAsync('OnNavigateToRadio', items[i].value, items[i].isNullValue === true, items[i].serializedValue);
            }
            return true;
        }
    }

    return false;
}

export function getFirstEnabledRadio(groupElement) {
    const items = getOrderedRadios(groupElement);
    for (const item of items) {
        if (!isRadioDisabled(item.element)) {
            return item.element;
        }
    }
    return null;
}

export function isBlurWithinGroup(groupElement) {
    if (!groupElement) {
        return false;
    }

    const activeElement = document.activeElement;
    if (!activeElement) {
        return false;
    }

    return groupElement.contains(activeElement);
}
