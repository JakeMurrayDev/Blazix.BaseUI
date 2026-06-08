const STATE_KEY = Symbol.for('Blazix.BaseUI.Checkbox.State');

/**
 * Applies optimistic visual state to the checkbox element immediately,
 * without waiting for the Blazor Server round-trip. This eliminates
 * perceived lag during rapid clicking in Interactive Server mode.
 */
function applyOptimisticState(element, checked, indeterminate) {
    element.setAttribute('aria-checked', indeterminate ? 'mixed' : checked ? 'true' : 'false');
    checked && !indeterminate ? element.setAttribute('data-checked', '') : element.removeAttribute('data-checked');
    !checked && !indeterminate ? element.setAttribute('data-unchecked', '') : element.removeAttribute('data-unchecked');
    indeterminate ? element.setAttribute('data-indeterminate', '') : element.removeAttribute('data-indeterminate');
}

function getDefaultFormSubmitter(form) {
    if (!form) {
        return null;
    }

    for (const candidate of form.elements) {
        const tagName = candidate.tagName;

        if ((tagName === 'BUTTON' || tagName === 'INPUT') && candidate.type === 'submit') {
            return candidate;
        }
    }

    return null;
}

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

function dispatchInputClick(state) {
    const inputElement = state.inputElement;

    if (!inputElement) {
        return;
    }

    const view = inputElement.ownerDocument.defaultView;
    const ClickEvent = view.PointerEvent ?? view.MouseEvent;
    inputElement.dispatchEvent(new ClickEvent('click', state.lastInteractionModifiers ?? getEventModifiers()));
}

/**
 * Dispatches the toggle to Blazor by clicking the hidden input element.
 * When debouncing is active, only the final resolved state is dispatched.
 */
function dispatchToggle(state) {
    if (!state.inputElement) {
        return;
    }

    const currentInputChecked = state.inputElement.checked;
    const desiredChecked = state.optimisticChecked;

    // Only click the input if the desired state differs from its current state.
    // Each click toggles the input, so we only need one click (or none).
    if (currentInputChecked !== desiredChecked) {
        dispatchInputClick(state);
    }
}

function handleEnterKey(event, state) {
    if (event.defaultPrevented) {
        return;
    }

    const formToSubmit = state.inputElement?.form ?? null;
    const view = event.currentTarget?.ownerDocument?.defaultView ?? window;
    const originalPreventDefault = event.preventDefault;
    let preventDefaultCalledAfterPropagation = false;

    event.preventDefault = () => {
        preventDefaultCalledAfterPropagation = true;
        originalPreventDefault.call(event);
    };

    originalPreventDefault.call(event);

    view.queueMicrotask(() => {
        event.preventDefault = originalPreventDefault;

        if (!preventDefaultCalledAfterPropagation) {
            getDefaultFormSubmitter(formToSubmit)?.click();
        }
    });
}

export function initialize(element, inputElement, disabled, readOnly, indeterminate, checked, nativeButton, allowsOptimisticState) {
    if (!element) {
        return;
    }

    const state = {
        inputElement,
        disabled,
        readOnly,
        indeterminate,
        nativeButton,
        allowsOptimisticState: allowsOptimisticState !== false,
        optimisticChecked: checked,
        lastInteractionModifiers: null,
        debounceTimer: null,
        keydownHandler: null,
        keyupHandler: null,
        clickHandler: null,
        inputFocusHandler: null
    };

    state.inputFocusHandler = () => {
        element.focus();
    };

    if (inputElement) {
        inputElement.indeterminate = indeterminate;
        inputElement.addEventListener('focus', state.inputFocusHandler);
    }

    state.keydownHandler = (event) => {
        if (state.disabled) {
            event.preventDefault();
            return;
        }

        if (event.key === 'Enter') {
            handleEnterKey(event, state);
            return;
        }

        if (state.readOnly) {
            if (event.key === ' ') {
                event.preventDefault();
            }
            return;
        }

        if (!state.nativeButton && event.key === ' ') {
            event.preventDefault();
        }
    };

    state.keyupHandler = (event) => {
        if (state.disabled || state.readOnly) {
            return;
        }

        if (!state.nativeButton && event.key === ' ') {
            event.preventDefault();
            toggleCheckbox(element, state, event);
        }
    };

    state.clickHandler = (event) => {
        if (state.disabled || state.readOnly) {
            event.preventDefault();
            return;
        }

        event.preventDefault();
        toggleCheckbox(element, state, event);
    };

    element.addEventListener('keydown', state.keydownHandler);
    element.addEventListener('keyup', state.keyupHandler);
    element.addEventListener('click', state.clickHandler);

    element[STATE_KEY] = state;
}

/**
 * Core toggle logic combining optimistic UI and debouncing.
 *
 * 1. Immediately flips the visual state (data attributes + aria) for instant feedback.
 * 2. Debounces the actual Blazor dispatch so rapid clicks coalesce into a single
 *    server round-trip with the final resolved state.
 */
function toggleCheckbox(element, state, event) {
    state.lastInteractionModifiers = getEventModifiers(event);

    if (!state.allowsOptimisticState || state.indeterminate) {
        dispatchInputClick(state);
        return;
    }

    // Flip the optimistic state
    const nextChecked = !state.optimisticChecked;
    state.optimisticChecked = nextChecked;

    // Apply visual feedback immediately (no server round-trip needed)
    applyOptimisticState(element, nextChecked, false);

    // Debounce the Blazor dispatch: coalesce rapid clicks into one server call.
    // 60ms is enough to catch double/triple clicks while feeling instant.
    if (state.debounceTimer !== null) {
        clearTimeout(state.debounceTimer);
    }

    state.debounceTimer = setTimeout(() => {
        state.debounceTimer = null;
        dispatchToggle(state);
    }, 60);
}

export function updateState(element, inputElement, disabled, readOnly, indeterminate, checked, nativeButton, allowsOptimisticState) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        if (state.inputElement !== inputElement) {
            if (state.inputElement && state.inputFocusHandler) {
                state.inputElement.removeEventListener('focus', state.inputFocusHandler);
            }

            state.inputElement = inputElement;

            if (state.inputElement && state.inputFocusHandler) {
                state.inputElement.addEventListener('focus', state.inputFocusHandler);
            }
        }

        const nextAllowsOptimisticState = allowsOptimisticState !== false;
        if (!nextAllowsOptimisticState && state.debounceTimer !== null) {
            clearTimeout(state.debounceTimer);
            state.debounceTimer = null;
        }

        state.disabled = disabled;
        state.readOnly = readOnly;
        state.indeterminate = indeterminate;
        state.nativeButton = nativeButton;
        state.allowsOptimisticState = nextAllowsOptimisticState;

        if (state.inputElement) {
            state.inputElement.indeterminate = indeterminate;
        }

        // Reconcile optimistic state with server truth when no debounce is pending.
        // This ensures controlled checkboxes and group parents stay in sync.
        if (state.debounceTimer === null) {
            state.optimisticChecked = checked;
        }
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

export function resetState(element, inputElement, checked, indeterminate) {
    if (inputElement) {
        inputElement.checked = checked;
        inputElement.indeterminate = indeterminate;
    }

    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        if (state.debounceTimer !== null) {
            clearTimeout(state.debounceTimer);
            state.debounceTimer = null;
        }

        state.optimisticChecked = checked;
        state.indeterminate = indeterminate;
    }

    applyOptimisticState(element, checked, indeterminate);
}

export function dispose(element) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        if (state.debounceTimer !== null) {
            clearTimeout(state.debounceTimer);
        }
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
