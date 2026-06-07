const STATE_KEY = Symbol.for('BlazorBaseUI.Toolbar.State');
if (!window[STATE_KEY]) {
    window[STATE_KEY] = new WeakMap();
}
const state = window[STATE_KEY];

const ARROW_UP = 'ArrowUp';
const ARROW_DOWN = 'ArrowDown';
const ARROW_LEFT = 'ArrowLeft';
const ARROW_RIGHT = 'ArrowRight';
const TAB = 'Tab';

function getItems(element) {
    const toolbarState = state.get(element);
    if (!toolbarState) {
        return [];
    }

    const items = Array.from(toolbarState.items).filter(item => item.isConnected && element.contains(item));
    items.sort((a, b) => {
        const position = a.compareDocumentPosition(b);
        if (position & Node.DOCUMENT_POSITION_FOLLOWING) {
            return -1;
        }
        if (position & Node.DOCUMENT_POSITION_PRECEDING) {
            return 1;
        }
        return 0;
    });
    return items;
}

function isItemDisabled(item) {
    return item.hasAttribute('data-disabled') ||
        item.getAttribute('aria-disabled') === 'true' ||
        item.disabled === true;
}

function isItemFocusable(item) {
    if (item.hasAttribute('disabled')) {
        return false;
    }

    if (item.hasAttribute('data-disabled') && !item.hasAttribute('data-focusable')) {
        return false;
    }

    if (item.getAttribute('aria-disabled') === 'true' && !item.hasAttribute('data-focusable')) {
        return false;
    }

    return true;
}

function getFocusableItems(element) {
    return getItems(element).filter(isItemFocusable);
}

function getItemFromTarget(toolbarElement, target) {
    if (!(target instanceof Element)) {
        return null;
    }

    return getItems(toolbarElement).find(item => item === target || item.contains(target)) ?? null;
}

function updateTabIndexes(element, activeItem = null) {
    const items = getItems(element);
    const focusableItems = items.filter(isItemFocusable);
    const targetItem = activeItem && focusableItems.includes(activeItem)
        ? activeItem
        : focusableItems[0] ?? null;

    for (const item of items) {
        item.tabIndex = item === targetItem ? 0 : -1;
    }

    return targetItem;
}

function focusItem(element, item) {
    if (!item) {
        return;
    }

    updateTabIndexes(element, item);
    item.scrollIntoView({ block: 'nearest', inline: 'nearest' });
    item.focus();
}

function getDirection(element) {
    const dirAttribute = element.getAttribute('dir') || element.closest('[dir]')?.getAttribute('dir');
    if (dirAttribute) {
        return dirAttribute.toLowerCase() === 'rtl' ? 'rtl' : 'ltr';
    }

    return getComputedStyle(element).direction === 'rtl' ? 'rtl' : 'ltr';
}

function getNavigationKeys(element, orientation) {
    const isRtl = getDirection(element) === 'rtl';
    const horizontalForwardKey = isRtl ? ARROW_LEFT : ARROW_RIGHT;
    const horizontalBackwardKey = isRtl ? ARROW_RIGHT : ARROW_LEFT;

    return {
        forwardKey: orientation === 'vertical' ? ARROW_DOWN : horizontalForwardKey,
        backwardKey: orientation === 'vertical' ? ARROW_UP : horizontalBackwardKey
    };
}

function isModifierKeySet(event) {
    return event.shiftKey || event.ctrlKey || event.altKey || event.metaKey;
}

function isNativeInput(target) {
    if (!(target instanceof HTMLElement)) {
        return false;
    }

    if (target.tagName === 'TEXTAREA') {
        return true;
    }

    if (target.tagName !== 'INPUT') {
        return false;
    }

    try {
        return target.selectionStart !== null;
    } catch {
        return false;
    }
}

function shouldUseNativeInputNavigation(event, target, forwardKey, backwardKey) {
    if (!isNativeInput(target) || isItemDisabled(target)) {
        return false;
    }

    const selectionStart = target.selectionStart;
    const selectionEnd = target.selectionEnd;
    const textContent = target.value ?? '';

    if (selectionStart == null || event.shiftKey || selectionStart !== selectionEnd) {
        return true;
    }

    if (event.key !== backwardKey && selectionStart < textContent.length) {
        return true;
    }

    if (event.key !== forwardKey && selectionStart > 0) {
        return true;
    }

    return false;
}

function shouldStopDisabledInputKeyDown(event, target) {
    return isNativeInput(target) &&
        isItemDisabled(target) &&
        event.key !== TAB &&
        event.key !== ARROW_LEFT &&
        event.key !== ARROW_RIGHT;
}

function stopDisabledEvent(event) {
    event.preventDefault();
    event.stopPropagation();
}

function handleFocusIn(event) {
    const element = event.currentTarget;
    const item = getItemFromTarget(element, event.target);

    if (item && isItemFocusable(item)) {
        updateTabIndexes(element, item);
    }

    if (isNativeInput(event.target)) {
        try {
            event.target.setSelectionRange(0, event.target.value?.length ?? 0);
        } catch {
            // Ignore input types that expose selectionStart but reject selection updates.
        }
    }
}

function handleDisabledPointerEvent(event) {
    const element = event.currentTarget;
    const item = getItemFromTarget(element, event.target);

    if (!item || !isItemDisabled(item)) {
        return;
    }

    stopDisabledEvent(event);
}

function handleDisabledKeyUp(event) {
    const element = event.currentTarget;
    const item = getItemFromTarget(element, event.target);

    if (!item || !isItemDisabled(item) || event.key === TAB) {
        return;
    }

    stopDisabledEvent(event);
}

function handleKeyDown(event) {
    const element = event.currentTarget;
    const toolbarState = state.get(element);
    if (!toolbarState) {
        return;
    }

    const { orientation, loopFocus } = toolbarState;
    const { forwardKey, backwardKey } = getNavigationKeys(element, orientation);
    const item = getItemFromTarget(element, event.target);
    const disabledTarget = item && isItemDisabled(item);
    const isNavigationKey = event.key === forwardKey || event.key === backwardKey;

    if (shouldStopDisabledInputKeyDown(event, event.target)) {
        stopDisabledEvent(event);
        return;
    }

    if (disabledTarget && event.key !== TAB && !isNavigationKey) {
        stopDisabledEvent(event);
        return;
    }

    if (!isNavigationKey || isModifierKeySet(event)) {
        return;
    }

    if (shouldUseNativeInputNavigation(event, event.target, forwardKey, backwardKey)) {
        return;
    }

    const items = getFocusableItems(element);
    if (items.length === 0) {
        if (disabledTarget) {
            stopDisabledEvent(event);
        }
        return;
    }

    const activeItem = item && items.includes(item) ? item : document.activeElement;
    const currentIndex = items.indexOf(activeItem);
    if (currentIndex === -1) {
        if (disabledTarget) {
            stopDisabledEvent(event);
        }
        return;
    }

    let nextIndex = -1;

    if (event.key === forwardKey) {
        if (currentIndex < items.length - 1) {
            nextIndex = currentIndex + 1;
        } else if (loopFocus) {
            nextIndex = 0;
        }
    } else if (event.key === backwardKey) {
        if (currentIndex > 0) {
            nextIndex = currentIndex - 1;
        } else if (loopFocus) {
            nextIndex = items.length - 1;
        }
    }

    if (nextIndex === -1 || nextIndex === currentIndex) {
        if (disabledTarget) {
            stopDisabledEvent(event);
        }
        return;
    }

    event.preventDefault();
    event.stopPropagation();

    const nextItem = items[nextIndex];
    updateTabIndexes(element, nextItem);
    queueMicrotask(() => focusItem(element, nextItem));
}

export function initToolbar(element, orientation, loopFocus) {
    if (!element) {
        return;
    }

    const toolbarState = {
        orientation,
        loopFocus,
        items: new Set()
    };

    state.set(element, toolbarState);
    element.addEventListener('focusin', handleFocusIn);
    element.addEventListener('keydown', handleKeyDown);
    element.addEventListener('keyup', handleDisabledKeyUp);
    element.addEventListener('click', handleDisabledPointerEvent);
    element.addEventListener('mousedown', handleDisabledPointerEvent);
    element.addEventListener('pointerdown', handleDisabledPointerEvent);
}

export function updateToolbar(element, orientation, loopFocus) {
    if (!element) {
        return;
    }

    const toolbarState = state.get(element);
    if (toolbarState) {
        toolbarState.orientation = orientation;
        toolbarState.loopFocus = loopFocus;
        updateTabIndexes(element, document.activeElement);
    }
}

export function disposeToolbar(element) {
    if (!element) {
        return;
    }

    element.removeEventListener('focusin', handleFocusIn);
    element.removeEventListener('keydown', handleKeyDown);
    element.removeEventListener('keyup', handleDisabledKeyUp);
    element.removeEventListener('click', handleDisabledPointerEvent);
    element.removeEventListener('mousedown', handleDisabledPointerEvent);
    element.removeEventListener('pointerdown', handleDisabledPointerEvent);
    state.delete(element);
}

export function registerItem(toolbarElement, itemElement) {
    if (!toolbarElement || !itemElement) {
        return;
    }

    const toolbarState = state.get(toolbarElement);
    if (!toolbarState) {
        return;
    }

    toolbarState.items.add(itemElement);
    updateTabIndexes(toolbarElement, document.activeElement);
}

export function unregisterItem(toolbarElement, itemElement) {
    if (!toolbarElement || !itemElement) {
        return;
    }

    const toolbarState = state.get(toolbarElement);
    if (!toolbarState) {
        return;
    }

    const hadFocus = document.activeElement === itemElement;
    toolbarState.items.delete(itemElement);
    const nextItem = updateTabIndexes(toolbarElement, document.activeElement);

    if (hadFocus && nextItem) {
        nextItem.focus();
    }
}
