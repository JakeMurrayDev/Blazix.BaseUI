import { acquireScrollLock } from './blazix-baseui-scroll-lock.min.js';

const MENU_STATE_KEY = Symbol.for('Blazix.BaseUI.Menu.State');
const STATE_KEY = Symbol.for('Blazix.BaseUI.MenuBar.State');
if (!window[STATE_KEY]) {
    window[STATE_KEY] = new WeakMap();
}
const state = window[STATE_KEY];

function getMenuState() {
    return window[MENU_STATE_KEY];
}

function getItems(element) {
    const menuBarState = state.get(element);
    if (!menuBarState) return [];
    const items = Array.from(menuBarState.items).filter(item => document.contains(item));
    items.sort((a, b) => {
        const position = a.compareDocumentPosition(b);
        if (position & Node.DOCUMENT_POSITION_FOLLOWING) return -1;
        if (position & Node.DOCUMENT_POSITION_PRECEDING) return 1;
        return 0;
    });
    return items;
}

function getFocusableItems(element) {
    return getItems(element).filter(item => {
        if (item.hasAttribute('data-disabled') && !item.hasAttribute('data-focusable')) {
            return false;
        }
        return true;
    });
}

function focusItem(item) {
    if (item) {
        item.tabIndex = 0;
        item.focus();
        item.scrollIntoView({ block: 'nearest', inline: 'nearest' });
    }
}

function isMenuRootInMenubar(rootState, element) {
    return rootState.menubarElement === element ||
        (rootState.triggerElement instanceof HTMLElement && element.contains(rootState.triggerElement));
}

function getDirection(element, menuBarState) {
    const menuState = getMenuState();
    if (menuState?.roots) {
        for (const rootState of menuState.roots.values()) {
            if (isMenuRootInMenubar(rootState, element) && rootState.direction) {
                return rootState.direction;
            }
        }
    }

    return menuBarState.direction || 'ltr';
}

function getLatestOpenMenuRoot(element) {
    const menuState = getMenuState();
    if (!menuState?.roots) return null;

    let latestRoot = null;
    for (const rootState of menuState.roots.values()) {
        if (!isMenuRootInMenubar(rootState, element) || !rootState.isOpen || !rootState.positionerElement) {
            continue;
        }

        if (!latestRoot || (rootState.openSequence ?? 0) > (latestRoot.openSequence ?? 0)) {
            latestRoot = rootState;
        }
    }

    return latestRoot;
}

function isViewportWidthPositioner(positionerElement) {
    const doc = positionerElement.ownerDocument || document;
    const win = doc.defaultView || window;
    const rect = positionerElement.getBoundingClientRect();
    return rect.width >= win.innerWidth - 20;
}

function getScrollLockElement(element, shouldEvaluate, openMethod) {
    if (!shouldEvaluate) return null;

    const latestRoot = getLatestOpenMenuRoot(element);
    if (!latestRoot?.positionerElement) return null;

    if (openMethod === 'touch' && !isViewportWidthPositioner(latestRoot.positionerElement)) {
        return null;
    }

    return latestRoot.positionerElement;
}

function shouldDeferScrollLock(element, shouldEvaluate, openMethod) {
    if (!shouldEvaluate) return false;

    const latestRoot = getLatestOpenMenuRoot(element);
    if (!latestRoot?.positionerElement) return true;

    if (openMethod === 'touch') {
        const rect = latestRoot.positionerElement.getBoundingClientRect();
        return rect.width === 0;
    }

    return false;
}

function handleKeyDown(event) {
    const element = event.currentTarget;
    const menuBarState = state.get(element);
    if (!menuBarState) return;

    const { orientation, loopFocus } = menuBarState;
    const isHorizontal = orientation === 'horizontal';
    const items = getFocusableItems(element);

    if (items.length === 0) return;

    const currentIndex = items.indexOf(document.activeElement);
    if (currentIndex === -1) return;

    let nextIndex = -1;
    let handled = false;
    let activateCurrent = false;

    const isRtl = isHorizontal && getDirection(element, menuBarState) === 'rtl';
    const prevKey = isHorizontal ? (isRtl ? 'ArrowRight' : 'ArrowLeft') : 'ArrowUp';
    const nextKey = isHorizontal ? (isRtl ? 'ArrowLeft' : 'ArrowRight') : 'ArrowDown';

    if (event.key === nextKey) {
        if (currentIndex < items.length - 1) {
            nextIndex = currentIndex + 1;
        } else if (loopFocus) {
            nextIndex = 0;
        }
        handled = true;
    } else if (event.key === prevKey) {
        if (currentIndex > 0) {
            nextIndex = currentIndex - 1;
        } else if (loopFocus) {
            nextIndex = items.length - 1;
        }
        handled = true;
    } else if (event.key === 'Enter' || event.key === ' ' || event.key === 'Spacebar' || event.code === 'Space') {
        activateCurrent = true;
        handled = true;
    }

    if (handled) {
        event.preventDefault();
        event.stopPropagation();

        if (activateCurrent) {
            items[currentIndex].click();
        } else if (nextIndex !== -1 && nextIndex !== currentIndex) {
            items[currentIndex].tabIndex = -1;
            focusItem(items[nextIndex]);
        }
    }
}

export function initMenuBar(element, orientation, loopFocus, direction) {
    if (!element) return;

    const menuBarState = {
        orientation,
        loopFocus,
        direction: direction || 'ltr',
        items: new Set(),
        releaseScrollLock: null,
        scrollLockRetryId: null
    };

    state.set(element, menuBarState);
    element.addEventListener('keydown', handleKeyDown, true);
}

export function updateMenuBar(element, orientation, loopFocus, direction) {
    if (!element) return;

    const menuBarState = state.get(element);
    if (menuBarState) {
        menuBarState.orientation = orientation;
        menuBarState.loopFocus = loopFocus;
        menuBarState.direction = direction || 'ltr';
    }
}

export function disposeMenuBar(element) {
    if (!element) return;

    element.removeEventListener('keydown', handleKeyDown, true);

    const menuBarState = state.get(element);
    if (menuBarState?.releaseScrollLock) {
        menuBarState.releaseScrollLock();
        menuBarState.releaseScrollLock = null;
    }
    if (menuBarState?.scrollLockRetryId) {
        clearTimeout(menuBarState.scrollLockRetryId);
        menuBarState.scrollLockRetryId = null;
    }

    state.delete(element);
}

export function updateScrollLock(element, shouldEvaluate, openMethod) {
    if (!element) return;

    const menuBarState = state.get(element);
    if (!menuBarState) return;

    if (menuBarState.scrollLockRetryId) {
        clearTimeout(menuBarState.scrollLockRetryId);
        menuBarState.scrollLockRetryId = null;
    }

    const scrollLockElement = getScrollLockElement(element, shouldEvaluate, openMethod);
    const shouldLock = !!scrollLockElement;

    if (shouldLock) {
        if (!menuBarState.releaseScrollLock) {
            menuBarState.releaseScrollLock = acquireScrollLock(scrollLockElement);
        }
    } else {
        if (menuBarState.releaseScrollLock) {
            menuBarState.releaseScrollLock();
            menuBarState.releaseScrollLock = null;
        }

        if (shouldDeferScrollLock(element, shouldEvaluate, openMethod)) {
            menuBarState.scrollLockRetryId = setTimeout(() => {
                menuBarState.scrollLockRetryId = null;
                updateScrollLock(element, shouldEvaluate, openMethod);
            }, 16);
        }
    }
}

export function registerItem(menuBarElement, itemElement) {
    if (!menuBarElement || !itemElement) return;

    const menuBarState = state.get(menuBarElement);
    if (!menuBarState) return;

    const items = menuBarState.items;
    items.add(itemElement);

    const sortedItems = getItems(menuBarElement);
    const firstItem = sortedItems[0];

    for (const item of sortedItems) {
        item.tabIndex = item === firstItem ? 0 : -1;
    }
}

export function unregisterItem(menuBarElement, itemElement) {
    if (!menuBarElement || !itemElement) return;

    const menuBarState = state.get(menuBarElement);
    if (!menuBarState) return;

    const items = menuBarState.items;
    const hadFocus = document.activeElement === itemElement;
    const sortedBefore = getItems(menuBarElement);
    const wasFirst = sortedBefore[0] === itemElement;

    items.delete(itemElement);

    if (items.size > 0 && (hadFocus || wasFirst)) {
        const sortedAfter = getItems(menuBarElement);
        const firstItem = sortedAfter[0];
        if (firstItem) {
            firstItem.tabIndex = 0;
            if (hadFocus) {
                firstItem.focus();
            }
        }
    }
}
