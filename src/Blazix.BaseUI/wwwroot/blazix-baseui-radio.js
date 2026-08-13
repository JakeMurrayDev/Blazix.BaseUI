const STATE_KEY = Symbol.for('Blazix.BaseUI.Radio.State');
const GROUP_STATE_KEY = Symbol.for('Blazix.BaseUI.RadioGroup.State');

if (!window[GROUP_STATE_KEY]) {
    window[GROUP_STATE_KEY] = new WeakMap();
}
const groupState = window[GROUP_STATE_KEY];

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

    const baseId = state.inputElement?.id || element.id || `base-ui-radio-${Math.random().toString(36).slice(2)}`;
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

export function initialize(element, inputElement, disabled, readOnly, nativeButton, enableLabelFallback) {
    if (!element) {
        return;
    }

    const state = {
        inputElement,
        disabled,
        readOnly,
        nativeButton,
        enableLabelFallback,
        fallbackAriaLabelledBy: null,
        keydownHandler: null
    };

    // Set up keyboard handler that prevents default for arrow keys
    state.keydownHandler = (e) => {
        // Upstream `useButton` bails out before any `preventDefault()` when disabled,
        // so keys the browser owns (Tab, shortcuts, scrolling) keep working.
        // Read-only is not a `useButton` concern: the root's `onKeyDown` still prevents
        // Enter and `useButton` still prevents Space on a read-only radio.
        if (state.disabled) {
            return;
        }

        // `useButton` only activates when the key originated on the element itself and
        // treats an already-prevented default as a cancelled activation.
        if (e.target !== element || e.defaultPrevented) {
            return;
        }

        // Arrow keys should prevent default to stop browser scrolling
        const arrowKeys = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'];

        if (arrowKeys.includes(e.key)) {
            // The group only navigates with Shift allowed (`MODIFIER_KEYS = [SHIFT]`),
            // so other modifier combinations must reach the browser/OS shortcut.
            if (!e.ctrlKey && !e.altKey && !e.metaKey) {
                e.preventDefault();
            }
        }

        // Space key should also prevent default (page scroll)
        if (e.key === ' ' || e.key === 'Enter') {
            e.preventDefault();
        }
    };

    element.addEventListener('keydown', state.keydownHandler);
    element[STATE_KEY] = state;
    syncFallbackAriaLabelledBy(element, state);
}

export function updateState(element, inputElement, disabled, readOnly, nativeButton, enableLabelFallback) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        state.disabled = disabled;
        state.readOnly = readOnly;
        state.nativeButton = nativeButton;
        state.inputElement = inputElement;
        state.enableLabelFallback = enableLabelFallback;
        syncFallbackAriaLabelledBy(element, state);
    }
}

export function setInputChecked(inputElement, checked) {
    if (!inputElement) {
        return;
    }

    inputElement.checked = checked;
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

export function initializeGroup(element, dotNetRef, direction) {
    if (!element) {
        return;
    }

    const state = {
        element,
        dotNetRef,
        direction,
        items: new Set(),
        keydownCaptureHandler: null
    };

    state.keydownCaptureHandler = async (e) => {
        const arrowKeys = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'];

        if (!arrowKeys.includes(e.key)) {
            return;
        }

        // The composite root only navigates when no modifier outside `MODIFIER_KEYS = [SHIFT]`
        // is held, so Ctrl/Alt/Meta + Arrow keeps reaching the browser or OS shortcut.
        if (e.ctrlKey || e.altKey || e.metaKey) {
            return;
        }

        // Resolve the originating radio before preventing anything: focusable content
        // rendered inside the group (text inputs, buttons) keeps native arrow behavior,
        // matching the composite root which bails out for native inputs with a caret.
        const currentElement = getRadioFromEventTarget(element, e.target);
        if (!currentElement || isRadioDisabled(currentElement)) {
            return;
        }

        e.preventDefault();

        await dotNetRef.invokeMethodAsync('OnArrowKeyPressed');

        const backwardKey = state.direction === 'rtl' ? 'ArrowRight' : 'ArrowLeft';

        if (e.key === 'ArrowUp' || e.key === backwardKey) {
            await navigateToPrevious(element, currentElement);
        } else {
            await navigateToNext(element, currentElement);
        }
    };

    element.addEventListener('keydown', state.keydownCaptureHandler, { capture: true });
    groupState.set(element, state);
}

export function updateGroupDirection(element, direction) {
    if (!element) {
        return;
    }

    const state = groupState.get(element);
    if (state) {
        state.direction = direction;
    }
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
