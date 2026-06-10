const STATE_KEY = Symbol.for('Blazix.BaseUI.Switch.State');

function getEventModifiers(event) {
    return {
        bubbles: true,
        cancelable: true,
        shiftKey: event?.shiftKey ?? false,
        ctrlKey: event?.ctrlKey ?? false,
        altKey: event?.altKey ?? false,
        metaKey: event?.metaKey ?? false
    };
}

function dispatchInputClick(state, event) {
    const inputElement = state.inputElement;

    if (!inputElement) {
        return;
    }

    const view = inputElement.ownerDocument.defaultView;
    const ClickEvent = view.PointerEvent ?? view.MouseEvent;
    inputElement.dispatchEvent(new ClickEvent('click', getEventModifiers(event)));
}

function dispatchRootClick(state, element, event) {
    const view = element.ownerDocument.defaultView;
    const ClickEvent = view.PointerEvent ?? view.MouseEvent;

    state.suppressRootActivation = true;
    try {
        element.dispatchEvent(new ClickEvent('click', getEventModifiers(event)));
    } finally {
        state.suppressRootActivation = false;
    }
}

function dispatchKeyboardActivation(state, element, event) {
    dispatchRootClick(state, element, event);
    dispatchInputClick(state, event);
}

function findAssociatedLabel(labelSource) {
    if (!labelSource) {
        return null;
    }

    const parent = labelSource.parentElement;
    if (parent?.tagName === 'LABEL') {
        return parent;
    }

    const controlId = labelSource.id;
    if (controlId) {
        const nextSibling = labelSource.nextElementSibling;
        if (nextSibling?.tagName === 'LABEL' && nextSibling.htmlFor === controlId) {
            return nextSibling;
        }
    }

    return labelSource.labels?.[0] ?? null;
}

function ensureLabelId(label, state, element) {
    if (label.id) {
        return label.id;
    }

    const baseId = state.inputElement?.id || element.id || `base-ui-switch-${Math.random().toString(36).slice(2)}`;
    label.id = `${baseId}-label`;
    return label.id;
}

function syncFallbackAriaLabelledBy(element, state) {
    if (!state.enableLabelFallback) {
        if (state.fallbackAriaLabelledBy &&
            element.getAttribute('aria-labelledby') === state.fallbackAriaLabelledBy) {
            element.removeAttribute('aria-labelledby');
        }

        state.fallbackAriaLabelledBy = null;
        return;
    }

    const label = findAssociatedLabel(state.inputElement);
    if (!label) {
        if (state.fallbackAriaLabelledBy &&
            element.getAttribute('aria-labelledby') === state.fallbackAriaLabelledBy) {
            element.removeAttribute('aria-labelledby');
        }

        state.fallbackAriaLabelledBy = null;
        return;
    }

    const labelId = ensureLabelId(label, state, element);
    state.fallbackAriaLabelledBy = labelId;
    element.setAttribute('aria-labelledby', labelId);
}

function updateInputFocusHandler(element, state, inputElement) {
    if (state.inputElement === inputElement) {
        return;
    }

    if (state.inputElement && state.inputFocusHandler) {
        state.inputElement.removeEventListener('focus', state.inputFocusHandler);
    }

    state.inputElement = inputElement;

    if (state.inputElement && state.inputFocusHandler) {
        state.inputElement.addEventListener('focus', state.inputFocusHandler);
    }
}

export function initialize(element, inputElement, disabled, readOnly, nativeButton, enableLabelFallback) {
    if (!element) {
        return;
    }

    dispose(element);

    const state = {
        inputElement,
        disabled,
        readOnly,
        nativeButton,
        enableLabelFallback,
        fallbackAriaLabelledBy: null,
        keydownHandler: null,
        keyupHandler: null,
        clickHandler: null,
        inputFocusHandler: null,
        suppressRootActivation: false
    };

    state.inputFocusHandler = () => {
        element.focus();
    };

    if (inputElement) {
        inputElement.addEventListener('focus', state.inputFocusHandler);
    }

    if (nativeButton) {
        state.clickHandler = (event) => {
            if (state.disabled) {
                event.preventDefault();
                event.stopPropagation();
                return;
            }

            if (state.readOnly) {
                event.preventDefault();
                return;
            }

            event.preventDefault();
            dispatchInputClick(state, event);
        };

        element.addEventListener('click', state.clickHandler);
        element[STATE_KEY] = state;
        syncFallbackAriaLabelledBy(element, state);
        return;
    }

    state.keydownHandler = (event) => {
        if (state.disabled) {
            event.preventDefault();
            return;
        }

        if (state.readOnly) {
            if (event.key === ' ') {
                event.preventDefault();
            }

            if (event.key === 'Enter') {
                event.preventDefault();
                dispatchRootClick(state, element, event);
            }

            return;
        }

        if (event.key === ' ') {
            event.preventDefault();
        }

        if (event.key === 'Enter') {
            event.preventDefault();
            dispatchKeyboardActivation(state, element, event);
        }
    };

    state.keyupHandler = (event) => {
        if (state.disabled) {
            return;
        }

        if (state.readOnly) {
            if (event.key === ' ') {
                event.preventDefault();
                dispatchRootClick(state, element, event);
            }

            return;
        }

        if (event.key === ' ') {
            event.preventDefault();
            dispatchKeyboardActivation(state, element, event);
        }
    };

    state.clickHandler = (event) => {
        if (state.suppressRootActivation) {
            return;
        }

        if (state.disabled) {
            event.preventDefault();
            event.stopPropagation();
            return;
        }

        if (state.readOnly) {
            event.preventDefault();
            return;
        }

        event.preventDefault();
        dispatchInputClick(state, event);
    };

    element.addEventListener('keydown', state.keydownHandler);
    element.addEventListener('keyup', state.keyupHandler);
    element.addEventListener('click', state.clickHandler);

    element[STATE_KEY] = state;
    syncFallbackAriaLabelledBy(element, state);
}

export function updateState(element, inputElement, disabled, readOnly, nativeButton, enableLabelFallback) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        updateInputFocusHandler(element, state, inputElement);
        state.disabled = disabled;
        state.readOnly = readOnly;
        state.nativeButton = nativeButton;
        state.enableLabelFallback = enableLabelFallback;
        syncFallbackAriaLabelledBy(element, state);
    }
}

export function focus(element) {
    if (!element) {
        return;
    }

    element.focus();
}

export function setInputChecked(inputElement, checked) {
    if (!inputElement) {
        return;
    }

    inputElement.checked = checked;
}

export function dispose(element) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        if (state.inputElement && state.inputFocusHandler) {
            state.inputElement.removeEventListener('focus', state.inputFocusHandler);
        }
        if (state.keydownHandler) {
            element.removeEventListener('keydown', state.keydownHandler);
        }
        if (state.keyupHandler) {
            element.removeEventListener('keyup', state.keyupHandler);
        }
        if (state.clickHandler) {
            element.removeEventListener('click', state.clickHandler);
        }
        delete element[STATE_KEY];
    }
}
