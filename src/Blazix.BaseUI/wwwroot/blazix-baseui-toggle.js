const STATE_KEY = Symbol.for('Blazix.BaseUI.Toggle.State');
const GROUP_STATE_KEY = Symbol.for('Blazix.BaseUI.ToggleGroup.State');
const GROUP_ITEM_STATE_KEY = Symbol.for('Blazix.BaseUI.ToggleGroupItem.State');

if (!window[GROUP_STATE_KEY]) {
    window[GROUP_STATE_KEY] = new WeakMap();
}
const groupStateMap = window[GROUP_STATE_KEY];

export function initialize(element, disabled) {
    if (!element) {
        return;
    }

    const state = {
        disabled,
        keydownHandler: null,
        keyupHandler: null
    };

    state.keydownHandler = (event) => {
        if (state.disabled) {
            event.preventDefault();
            return;
        }

        const isEnterKey = event.key === 'Enter';
        const isSpaceKey = event.key === ' ';

        if (isSpaceKey || isEnterKey) {
            event.preventDefault();
            if (isEnterKey) {
                element.click();
            }
        }
    };

    state.keyupHandler = (event) => {
        if (state.disabled) {
            return;
        }

        if (event.key === ' ') {
            element.click();
        }
    };

    element.addEventListener('keydown', state.keydownHandler);
    element.addEventListener('keyup', state.keyupHandler);

    element[STATE_KEY] = state;
}

export function updateState(element, disabled) {
    if (!element) {
        return;
    }

    const state = element[STATE_KEY];
    if (state) {
        state.disabled = disabled;
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
        if (state.keyupHandler) {
            element.removeEventListener('keyup', state.keyupHandler);
        }
        delete element[STATE_KEY];
    }
}

export function initializeGroup(element, orientation, loopFocus, direction = 'ltr') {
    if (!element) {
        return;
    }

    const state = {
        element,
        orientation,
        loopFocus,
        direction,
        highlightedElement: null,
        hasUserHighlighted: false,
        items: new Set()
    };

    groupStateMap.set(element, state);
}

export function updateGroup(element, orientation, loopFocus, direction = 'ltr') {
    if (!element) {
        return;
    }

    const state = groupStateMap.get(element);
    if (state) {
        state.orientation = orientation;
        state.loopFocus = loopFocus;
        state.direction = direction;
        updateToggleTabIndexes(element);
    }
}

export function disposeGroup(element) {
    if (!element) {
        return;
    }

    const state = groupStateMap.get(element);
    if (state) {
        for (const item of state.items) {
            if (item.focusHandler) {
                item.element.removeEventListener('focus', item.focusHandler);
            }
        }
    }

    groupStateMap.delete(element);
}

export function registerToggle(groupElement, toggleElement, value) {
    if (!groupElement || !toggleElement) {
        return;
    }

    const state = groupStateMap.get(groupElement);
    if (!state) {
        return;
    }

    for (const item of state.items) {
        if (item.element === toggleElement) {
            item.value = value;
            updateToggleTabIndexes(groupElement);
            return;
        }
    }

    const item = {
        element: toggleElement,
        value,
        focusHandler: null
    };

    item.focusHandler = () => {
        const currentState = groupStateMap.get(groupElement);
        if (!currentState || isToggleDisabled(toggleElement)) {
            return;
        }

        currentState.highlightedElement = toggleElement;
        currentState.hasUserHighlighted = true;
        updateToggleTabIndexes(groupElement);
    };

    toggleElement.addEventListener('focus', item.focusHandler);
    state.items.add(item);
    updateToggleTabIndexes(groupElement);
}

export function unregisterToggle(groupElement, toggleElement) {
    if (!groupElement || !toggleElement) {
        return;
    }

    const state = groupStateMap.get(groupElement);
    if (!state) {
        return;
    }

    for (const item of state.items) {
        if (item.element === toggleElement) {
            if (item.focusHandler) {
                item.element.removeEventListener('focus', item.focusHandler);
            }
            state.items.delete(item);
            if (state.highlightedElement === toggleElement) {
                state.highlightedElement = null;
                state.hasUserHighlighted = false;
            }
            updateToggleTabIndexes(groupElement);
            return;
        }
    }
}

function getOrderedToggles(groupElement) {
    const state = groupStateMap.get(groupElement);
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

function isToggleDisabled(toggleElement) {
    return toggleElement.hasAttribute('data-disabled') ||
        toggleElement.disabled ||
        toggleElement.getAttribute('aria-disabled') === 'true';
}

function updateToggleTabIndexes(groupElement) {
    const state = groupStateMap.get(groupElement);
    if (!state) {
        return;
    }

    const items = getOrderedToggles(groupElement);
    if (items.length === 0) {
        return;
    }

    const firstEnabled = items.find(item => !isToggleDisabled(item.element));
    const highlightedItem = state.hasUserHighlighted
        ? items.find(item => item.element === state.highlightedElement)
        : null;
    const targetItem = highlightedItem && !isToggleDisabled(highlightedItem.element)
        ? highlightedItem
        : firstEnabled;

    state.highlightedElement = targetItem?.element ?? null;

    for (const item of items) {
        const isDisabled = isToggleDisabled(item.element);

        if (isDisabled) {
            item.element.tabIndex = -1;
        } else if (item === targetItem) {
            item.element.tabIndex = 0;
        } else {
            item.element.tabIndex = -1;
        }
    }
}

export function syncGroupTabIndexes(groupElement) {
    updateToggleTabIndexes(groupElement);
}

export function navigateToPrevious(groupElement, currentElement) {
    const state = groupStateMap.get(groupElement);
    if (!state) {
        return false;
    }

    const items = getOrderedToggles(groupElement);
    const currentIndex = items.findIndex(item => item.element === currentElement);
    if (currentIndex < 0) {
        return false;
    }

    for (let i = currentIndex - 1; i >= 0; i--) {
        if (!isToggleDisabled(items[i].element)) {
            setFocusedToggle(groupElement, items[i].element);
            return true;
        }
    }

    if (state.loopFocus) {
        for (let i = items.length - 1; i > currentIndex; i--) {
            if (!isToggleDisabled(items[i].element)) {
                setFocusedToggle(groupElement, items[i].element);
                return true;
            }
        }
    }

    return false;
}

export function navigateToNext(groupElement, currentElement) {
    const state = groupStateMap.get(groupElement);
    if (!state) {
        return false;
    }

    const items = getOrderedToggles(groupElement);
    const currentIndex = items.findIndex(item => item.element === currentElement);
    if (currentIndex < 0) {
        return false;
    }

    for (let i = currentIndex + 1; i < items.length; i++) {
        if (!isToggleDisabled(items[i].element)) {
            setFocusedToggle(groupElement, items[i].element);
            return true;
        }
    }

    if (state.loopFocus) {
        for (let i = 0; i < currentIndex; i++) {
            if (!isToggleDisabled(items[i].element)) {
                setFocusedToggle(groupElement, items[i].element);
                return true;
            }
        }
    }

    return false;
}

export function navigateToFirst(groupElement) {
    const items = getOrderedToggles(groupElement);
    for (const item of items) {
        if (!isToggleDisabled(item.element)) {
            setFocusedToggle(groupElement, item.element);
            return true;
        }
    }
    return false;
}

export function navigateToLast(groupElement) {
    const items = getOrderedToggles(groupElement);
    for (let i = items.length - 1; i >= 0; i--) {
        if (!isToggleDisabled(items[i].element)) {
            setFocusedToggle(groupElement, items[i].element);
            return true;
        }
    }
    return false;
}

function setFocusedToggle(groupElement, toggleElement) {
    const state = groupStateMap.get(groupElement);
    if (state) {
        state.highlightedElement = toggleElement;
        state.hasUserHighlighted = true;
    }

    const items = getOrderedToggles(groupElement);
    for (const item of items) {
        item.element.tabIndex = item.element === toggleElement ? 0 : -1;
    }
    toggleElement.focus({ preventScroll: true });
}

export function isFirstEnabledToggle(groupElement, toggleElement) {
    const items = getOrderedToggles(groupElement);
    const firstEnabled = items.find(item => !isToggleDisabled(item.element));
    return firstEnabled?.element === toggleElement;
}

export function initializeGroupItem(element, orientation) {
    if (!element) {
        return;
    }

    const state = {
        orientation,
        keydownHandler: null
    };

    state.keydownHandler = (event) => {
        const isHorizontal = state.orientation === 'horizontal';
        const isVertical = state.orientation === 'vertical';

        const isArrowLeft = event.key === 'ArrowLeft';
        const isArrowRight = event.key === 'ArrowRight';
        const isArrowUp = event.key === 'ArrowUp';
        const isArrowDown = event.key === 'ArrowDown';
        const isHome = event.key === 'Home';
        const isEnd = event.key === 'End';

        const shouldPrevent =
            isHome ||
            isEnd ||
            (isHorizontal && (isArrowLeft || isArrowRight)) ||
            (isVertical && (isArrowUp || isArrowDown));

        if (shouldPrevent) {
            event.preventDefault();
        }
    };

    element.addEventListener('keydown', state.keydownHandler);

    element[GROUP_ITEM_STATE_KEY] = state;
}

export function updateGroupItemOrientation(element, orientation) {
    if (!element) {
        return;
    }

    const state = element[GROUP_ITEM_STATE_KEY];
    if (state) {
        state.orientation = orientation;
    }
}

export function disposeGroupItem(element) {
    if (!element) {
        return;
    }

    const state = element[GROUP_ITEM_STATE_KEY];
    if (state) {
        if (state.keydownHandler) {
            element.removeEventListener('keydown', state.keydownHandler);
        }
        delete element[GROUP_ITEM_STATE_KEY];
    }
}
