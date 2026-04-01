/**
 * Composite widget navigation module.
 * Provides roving tabindex, arrow key navigation, Home/End support, and hover highlighting.
 * Based on Base UI useCompositeRoot + useCompositeItem.
 */

const STATE_KEY = Symbol.for('BlazorBaseUI.Composite.State');
if (!window[STATE_KEY]) {
    window[STATE_KEY] = new WeakMap();
}
const state = window[STATE_KEY];

const DEFAULTS = {
    orientation: 'vertical',
    loop: false,
    enableHomeAndEndKeys: true,
    direction: 'ltr',
    cols: 1,
    itemSelector: '[data-blazor-base-ui-composite-item]',
    disabledSelector: '[data-disabled]',
    highlightOnHover: false
};

function getItems(element, options) {
    const allItems = Array.from(element.querySelectorAll(options.itemSelector));
    return allItems.filter(item => !item.matches(options.disabledSelector));
}

function getAllItems(element, options) {
    return Array.from(element.querySelectorAll(options.itemSelector));
}

function updateTabIndices(element, focusedItem, options) {
    const items = getAllItems(element, options);
    for (const item of items) {
        item.setAttribute('tabindex', item === focusedItem ? '0' : '-1');
    }
}

function focusItem(element, item, options) {
    updateTabIndices(element, item, options);
    item.focus();
    item.scrollIntoView({ block: 'nearest' });
}

function isNextKey(key, options) {
    const { orientation, direction } = options;
    const isRtl = direction === 'rtl';

    if (orientation === 'vertical' || orientation === 'both') {
        if (key === 'ArrowDown') return true;
    }
    if (orientation === 'horizontal' || orientation === 'both') {
        if (key === 'ArrowRight' && !isRtl) return true;
        if (key === 'ArrowLeft' && isRtl) return true;
    }
    return false;
}

function isPrevKey(key, options) {
    const { orientation, direction } = options;
    const isRtl = direction === 'rtl';

    if (orientation === 'vertical' || orientation === 'both') {
        if (key === 'ArrowUp') return true;
    }
    if (orientation === 'horizontal' || orientation === 'both') {
        if (key === 'ArrowLeft' && !isRtl) return true;
        if (key === 'ArrowRight' && isRtl) return true;
    }
    return false;
}

function isGridDownKey(key, options) {
    return options.cols > 1 && key === 'ArrowDown';
}

function isGridUpKey(key, options) {
    return options.cols > 1 && key === 'ArrowUp';
}

function handleKeyDown(event, element, options) {
    const items = getItems(element, options);
    if (items.length === 0) return;

    const currentIndex = items.indexOf(document.activeElement);
    let nextIndex = -1;

    if (isGridDownKey(event.key, options)) {
        const target = currentIndex + options.cols;
        if (target < items.length) {
            nextIndex = target;
        } else if (options.loop) {
            nextIndex = currentIndex % options.cols;
        }
    } else if (isGridUpKey(event.key, options)) {
        const target = currentIndex - options.cols;
        if (target >= 0) {
            nextIndex = target;
        } else if (options.loop) {
            const col = currentIndex % options.cols;
            const lastRowStart = items.length - (items.length % options.cols || options.cols);
            nextIndex = Math.min(lastRowStart + col, items.length - 1);
        }
    } else if (isNextKey(event.key, options)) {
        if (currentIndex < items.length - 1) {
            nextIndex = currentIndex + 1;
        } else if (options.loop) {
            nextIndex = 0;
        }
    } else if (isPrevKey(event.key, options)) {
        if (currentIndex > 0) {
            nextIndex = currentIndex - 1;
        } else if (options.loop) {
            nextIndex = items.length - 1;
        }
    } else if (event.key === 'Home' && options.enableHomeAndEndKeys) {
        nextIndex = 0;
    } else if (event.key === 'End' && options.enableHomeAndEndKeys) {
        nextIndex = items.length - 1;
    }

    if (nextIndex >= 0 && nextIndex !== currentIndex) {
        event.preventDefault();
        event.stopPropagation();
        focusItem(element, items[nextIndex], options);
    }
}

function handlePointerMove(event, element, options) {
    const item = event.target.closest(options.itemSelector);
    if (!item || item.matches(options.disabledSelector)) return;
    if (document.activeElement === item) return;
    focusItem(element, item, options);
}

export function initialize(element, userOptions) {
    dispose(element);

    const options = { ...DEFAULTS, ...userOptions };

    const onKeyDown = (e) => handleKeyDown(e, element, options);
    const onPointerMove = options.highlightOnHover
        ? (e) => handlePointerMove(e, element, options)
        : null;

    element.addEventListener('keydown', onKeyDown);
    if (onPointerMove) {
        element.addEventListener('pointermove', onPointerMove);
    }

    const items = getItems(element, options);
    const allItems = getAllItems(element, options);
    for (const item of allItems) {
        item.setAttribute('tabindex', item === items[0] ? '0' : '-1');
    }

    state.set(element, { options, onKeyDown, onPointerMove });
}

export function dispose(element) {
    if (!element) return;
    const entry = state.get(element);
    if (!entry) return;

    element.removeEventListener('keydown', entry.onKeyDown);
    if (entry.onPointerMove) {
        element.removeEventListener('pointermove', entry.onPointerMove);
    }

    state.delete(element);
}

export function updateOptions(element, newOptions) {
    const entry = state.get(element);
    if (!entry) return;
    initialize(element, { ...entry.options, ...newOptions });
}
