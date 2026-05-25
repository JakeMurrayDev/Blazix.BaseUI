const STATE_KEY = Symbol.for('BlazorBaseUI.Tabs.State');
const LIST_STATE_KEY = Symbol.for('BlazorBaseUI.TabsList.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        resizeObservers: new WeakMap(),
        dotNetRefs: new WeakMap(),
        listHandlers: new WeakMap(),
        tabPreActivationHandlers: new WeakMap(),
        panelHandoffObservers: new WeakMap(),
        panelVisibilityObservers: new WeakMap()
    };
}
const state = window[STATE_KEY];
state.tabPreActivationHandlers ??= new WeakMap();
state.panelHandoffObservers ??= new WeakMap();
state.panelVisibilityObservers ??= new WeakMap();

if (!window[LIST_STATE_KEY]) {
    window[LIST_STATE_KEY] = new WeakMap();
}
const listStateMap = window[LIST_STATE_KEY];
const pendingTabsByList = new WeakMap();

const NAVIGATION_KEYS = ['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End'];

export function initializeList(element, orientation, loopFocus, activateOnFocus, direction, dotNetRef) {
    if (!element) return;

    const listState = {
        element,
        orientation,
        loopFocus,
        activateOnFocus,
        direction,
        dotNetRef,
        tabs: new Set()
    };

    const handler = (e) => {
        if (!NAVIGATION_KEYS.includes(e.key)) return;
        if (e.shiftKey || e.ctrlKey || e.altKey || e.metaKey) return;

        const isHorizontal = listState.orientation === 'horizontal';
        const isVertical = listState.orientation === 'vertical';

        const shouldPrevent =
            (isHorizontal && (e.key === 'ArrowLeft' || e.key === 'ArrowRight')) ||
            (isVertical && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) ||
            e.key === 'Home' ||
            e.key === 'End';

        if (shouldPrevent) {
            e.preventDefault();
        }
    };

    element.addEventListener('keydown', handler);
    state.listHandlers.set(element, handler);
    listStateMap.set(element, listState);
    hydratePendingTabs(element, listState);
    observePanelVisibility(element);
}

export function updateList(element, orientation, loopFocus, activateOnFocus, direction) {
    if (!element) return;

    const listState = listStateMap.get(element);
    if (listState) {
        listState.orientation = orientation;
        listState.loopFocus = loopFocus;
        listState.activateOnFocus = activateOnFocus;
        listState.direction = direction;
    }
}

export function disposeList(element) {
    if (!element) return;

    disposePanelHandoff(element);

    const listState = listStateMap.get(element);
    if (listState) {
        for (const item of listState.tabs) {
            detachPreActivationHandler(item.element);
        }
    }

    const handler = state.listHandlers.get(element);
    if (handler) {
        element.removeEventListener('keydown', handler);
        state.listHandlers.delete(element);
    }

    disposePanelVisibilityObserver(element);
    listStateMap.delete(element);
    pendingTabsByList.delete(element);
    unobserveResize(element);
}

export function registerTab(listElement, tabElement, value) {
    if (!listElement || !tabElement) return;

    tabElement._tabsValue = value;

    const listState = listStateMap.get(listElement);
    if (!listState) {
        let pendingTabs = pendingTabsByList.get(listElement);
        if (!pendingTabs) {
            pendingTabs = new Set();
            pendingTabsByList.set(listElement, pendingTabs);
        }

        upsertTab(pendingTabs, tabElement, value);
        return;
    }

    upsertTab(listState.tabs, tabElement, value);
    attachPreActivationHandler(listElement, tabElement);
    observeTabForIndicators(listElement, tabElement);
    updateTabIndexes(listElement);
    notifyIndicatorRefs(listElement);
}

export function unregisterTab(listElement, tabElement) {
    if (!listElement || !tabElement) return;

    delete tabElement._tabsValue;

    const listState = listStateMap.get(listElement);
    if (!listState) {
        const pendingTabs = pendingTabsByList.get(listElement);
        if (pendingTabs) {
            deleteTab(pendingTabs, tabElement);
            if (pendingTabs.size === 0) {
                pendingTabsByList.delete(listElement);
            }
        }

        return;
    }

    if (deleteTab(listState.tabs, tabElement)) {
        detachPreActivationHandler(tabElement);
        unobserveTabForIndicators(listElement, tabElement);
        updateTabIndexes(listElement);
        notifyIndicatorRefs(listElement);
    }
}

function getOrderedTabs(listElement) {
    const listState = listStateMap.get(listElement);
    if (!listState) return [];

    const items = Array.from(listState.tabs).filter(item => document.contains(item.element));
    items.sort((a, b) => {
        const position = a.element.compareDocumentPosition(b.element);
        if (position & Node.DOCUMENT_POSITION_FOLLOWING) return -1;
        if (position & Node.DOCUMENT_POSITION_PRECEDING) return 1;
        return 0;
    });
    return items;
}

function hydratePendingTabs(listElement, listState) {
    const pendingTabs = pendingTabsByList.get(listElement);
    if (!pendingTabs) return;

    for (const item of pendingTabs) {
        upsertTab(listState.tabs, item.element, item.value);
        attachPreActivationHandler(listElement, item.element);
        observeTabForIndicators(listElement, item.element);
    }

    pendingTabsByList.delete(listElement);
    updateTabIndexes(listElement);
    notifyIndicatorRefs(listElement);
}

function upsertTab(tabs, tabElement, value) {
    for (const item of tabs) {
        if (item.element === tabElement) {
            item.value = value;
            return;
        }
    }

    tabs.add({ element: tabElement, value });
}

function deleteTab(tabs, tabElement) {
    for (const item of tabs) {
        if (item.element === tabElement) {
            tabs.delete(item);
            return true;
        }
    }

    return false;
}

function attachPreActivationHandler(listElement, tabElement) {
    detachPreActivationHandler(tabElement);

    const clickHandler = (event) => {
        if (event.button !== 0 || isTabDisabled(tabElement)) return;
        armCurrentPanelHandoffIfNoMotion(listElement, tabElement);
    };

    const focusHandler = () => {
        const listState = listStateMap.get(listElement);
        if (!listState?.activateOnFocus || isTabDisabled(tabElement)) return;
        armCurrentPanelHandoffIfNoMotion(listElement, tabElement);
    };

    tabElement.addEventListener('click', clickHandler, true);
    tabElement.addEventListener('focus', focusHandler, true);
    state.tabPreActivationHandlers.set(tabElement, { clickHandler, focusHandler });
}

function detachPreActivationHandler(tabElement) {
    const handlers = state.tabPreActivationHandlers.get(tabElement);
    if (!handlers) return;

    tabElement.removeEventListener('click', handlers.clickHandler, true);
    tabElement.removeEventListener('focus', handlers.focusHandler, true);
    state.tabPreActivationHandlers.delete(tabElement);
}

function armCurrentPanelHandoffIfNoMotion(listElement, nextTabElement) {
    const activeTab = listElement.querySelector('[role="tab"][aria-selected="true"]');
    if (!activeTab || activeTab === nextTabElement) return;

    const panelId = activeTab.getAttribute('aria-controls');
    if (!panelId) return;

    const activePanel = document.getElementById(panelId);
    if (!activePanel || activePanel.hidden || !shouldPreHidePanelImmediately(activePanel)) {
        return;
    }

    armPanelHandoff(listElement, activePanel);
}

function armPanelHandoff(listElement, activePanel) {
    disposePanelHandoff(listElement);

    const rootElement = listElement.parentElement ?? document.body;
    let disposed = false;
    let frameId = 0;
    let timeoutId = 0;

    const cleanup = () => {
        if (disposed) return;

        disposed = true;
        observer.disconnect();

        if (frameId) {
            cancelAnimationFrame(frameId);
        }

        if (timeoutId) {
            clearTimeout(timeoutId);
        }

        state.panelHandoffObservers.delete(listElement);
    };

    const completeIfReady = () => {
        if (disposed) {
            return true;
        }

        if (!document.contains(activePanel)) {
            cleanup();
            return true;
        }

        if (!hasAlternateVisiblePanel(rootElement, activePanel)) {
            return false;
        }

        hidePanelImmediately(activePanel);
        cleanup();
        return true;
    };

    const observer = new MutationObserver(() => {
        completeIfReady();
    });

    observer.observe(rootElement, {
        childList: true,
        subtree: true,
        attributes: true,
        attributeFilter: ['aria-selected', 'hidden', 'data-hidden', 'style', 'class']
    });

    frameId = requestAnimationFrame(() => {
        frameId = 0;
        completeIfReady();
    });

    timeoutId = setTimeout(cleanup, 2000);
    state.panelHandoffObservers.set(listElement, { dispose: cleanup });
}

function disposePanelHandoff(listElement) {
    const handoff = state.panelHandoffObservers.get(listElement);
    if (!handoff) return;

    handoff.dispose();
}

function observePanelVisibility(listElement) {
    disposePanelVisibilityObserver(listElement);

    const rootElement = listElement.parentElement ?? document.body;
    let disposed = false;
    let isChecking = false;

    const observer = new MutationObserver(() => {
        checkPanelVisibility();
    });

    const cleanup = () => {
        if (disposed) return;

        disposed = true;
        observer.disconnect();
        state.panelVisibilityObservers.delete(listElement);
    };

    const checkPanelVisibility = () => {
        if (disposed || isChecking) {
            return;
        }

        isChecking = true;
        try {
            hideInactiveNoMotionPanels(rootElement);
        } finally {
            isChecking = false;
        }
    };

    observer.observe(rootElement, {
        childList: true,
        subtree: true,
        attributes: true,
        attributeFilter: ['aria-selected', 'hidden', 'data-hidden', 'style', 'class']
    });

    state.panelVisibilityObservers.set(listElement, { dispose: cleanup });
    checkPanelVisibility();
}

function disposePanelVisibilityObserver(listElement) {
    const observer = state.panelVisibilityObservers.get(listElement);
    if (!observer) return;

    observer.dispose();
}

function hideInactiveNoMotionPanels(rootElement) {
    const activeTab = rootElement.querySelector('[role="tab"][aria-selected="true"]');
    const activePanelId = activeTab?.getAttribute('aria-controls');
    const activePanel = activePanelId ? document.getElementById(activePanelId) : null;

    if (activeTab && !activePanel) {
        return;
    }

    if (activePanel && !isPanelVisible(activePanel)) {
        return;
    }

    const panels = rootElement.querySelectorAll('[role="tabpanel"]');
    for (const panel of panels) {
        if (panel === activePanel || !isPanelVisible(panel)) {
            continue;
        }

        if (shouldPreHidePanelImmediately(panel)) {
            hidePanelImmediately(panel);
        }
    }
}

function hasAlternateVisiblePanel(container, activePanel) {
    const panels = container.querySelectorAll('[role="tabpanel"]');
    for (const panel of panels) {
        if (panel !== activePanel && isPanelVisible(panel)) {
            return true;
        }
    }

    return false;
}

function isPanelVisible(panel) {
    if (panel.hidden) {
        return false;
    }

    const style = getComputedStyle(panel);
    return style.display !== 'none' && style.visibility !== 'hidden';
}

function shouldPreHidePanelImmediately(element) {
    const hadEndingStyle = element.hasAttribute('data-ending-style');

    if (!hadEndingStyle) {
        element.setAttribute('data-ending-style', '');
    }

    const shouldHide = shouldCompletePanelTransitionImmediately(element);

    if (!hadEndingStyle) {
        element.removeAttribute('data-ending-style');
    }

    return shouldHide;
}

function isTabDisabled(tabElement) {
    const dataDisabled = tabElement.getAttribute('data-disabled');
    return dataDisabled === '' || dataDisabled === 'true' || tabElement.disabled || tabElement.getAttribute('aria-disabled') === 'true';
}

function updateTabIndexes(listElement) {
    const items = getOrderedTabs(listElement);
    if (items.length === 0) return;

    const hasActive = items.some(item => item.element.getAttribute('aria-selected') === 'true');
    const firstEnabled = items.find(item => !isTabDisabled(item.element));

    for (const item of items) {
        const isActive = item.element.getAttribute('aria-selected') === 'true';
        const isDisabled = isTabDisabled(item.element);

        if (isDisabled) {
            item.element.tabIndex = -1;
        } else if (isActive) {
            item.element.tabIndex = 0;
        } else if (!hasActive && item === firstEnabled) {
            item.element.tabIndex = 0;
        } else {
            item.element.tabIndex = -1;
        }
    }
}

export async function navigateToPrevious(listElement, currentElement) {
    const listState = listStateMap.get(listElement);
    if (!listState) return false;

    const items = getOrderedTabs(listElement);
    const currentIndex = items.findIndex(item => item.element === currentElement);
    if (currentIndex < 0) return false;

    for (let i = currentIndex - 1; i >= 0; i--) {
        await setFocusedTab(listElement, items[i]);
        return true;
    }

    if (listState.loopFocus) {
        for (let i = items.length - 1; i > currentIndex; i--) {
            await setFocusedTab(listElement, items[i]);
            return true;
        }
    }

    return false;
}

export async function navigateToNext(listElement, currentElement) {
    const listState = listStateMap.get(listElement);
    if (!listState) return false;

    const items = getOrderedTabs(listElement);
    const currentIndex = items.findIndex(item => item.element === currentElement);
    if (currentIndex < 0) return false;

    for (let i = currentIndex + 1; i < items.length; i++) {
        await setFocusedTab(listElement, items[i]);
        return true;
    }

    if (listState.loopFocus) {
        for (let i = 0; i < currentIndex; i++) {
            await setFocusedTab(listElement, items[i]);
            return true;
        }
    }

    return false;
}

export async function navigateToFirst(listElement) {
    const items = getOrderedTabs(listElement);
    const item = items[0];
    if (item) {
        await setFocusedTab(listElement, item);
        return true;
    }
    return false;
}

export async function navigateToLast(listElement) {
    const items = getOrderedTabs(listElement);
    const item = items[items.length - 1];
    if (item) {
        await setFocusedTab(listElement, item);
        return true;
    }
    return false;
}

async function setFocusedTab(listElement, item) {
    const listState = listStateMap.get(listElement);
    if (!listState) return;

    const items = getOrderedTabs(listElement);
    for (const t of items) {
        t.element.tabIndex = t.element === item.element ? 0 : -1;
    }

    item.element.focus({ preventScroll: true });
    item.element.scrollIntoView({ block: 'nearest', inline: 'nearest' });

    if (listState.activateOnFocus && !isTabDisabled(item.element) && listState.dotNetRef) {
        try {
            await listState.dotNetRef.invokeMethodAsync('OnNavigateToTab', item.value);
        } catch {
        }
    }
}

export function getFirstEnabledTab(listElement) {
    const items = getOrderedTabs(listElement);
    for (const item of items) {
        if (!isTabDisabled(item.element)) {
            return item.element;
        }
    }
    return null;
}

export function getActiveElement() {
    return document.activeElement;
}

export function initializeTab(element) {
    if (!element) return;

    element._tabsKeydownHandler = (e) => {
        if (element.disabled || element.getAttribute('aria-disabled') === 'true') return;

        if (e.key === ' ' || e.key === 'Enter') {
            e.preventDefault();
            element.click();
        }
    };

    element.addEventListener('keydown', element._tabsKeydownHandler);
}

export function dispose(element) {
    if (!element) return;

    if (element._tabsKeydownHandler) {
        element.removeEventListener('keydown', element._tabsKeydownHandler);
        delete element._tabsKeydownHandler;
    }
}

export function focus(element) {
    if (!element) return;
    element.focus({ preventScroll: true });
    element.scrollIntoView({ block: 'nearest', inline: 'nearest' });
}

export function startPanelTransition(element, dotNetRef, opening) {
    if (!element || !dotNetRef) return;

    disposePanel(element);
    const token = {};
    element._tabsPanelTransitionToken = token;

    if (opening) {
        showPanelImmediately(element);
    }

    if (!opening && shouldCompletePanelTransitionImmediately(element)) {
        completePanelExitTransition(element, token, dotNetRef);
        return;
    }

    const callback = () => {
        element._tabsPanelTransitionFrame = null;
        if (opening) {
            invokePanelTransitionCallback(element, token, dotNetRef, 'OnPanelStartingStyleApplied');
        } else {
            waitForPanelAnimations(element, token, dotNetRef);
        }
    };

    element._tabsPanelTransitionFrame = requestAnimationFrame(callback);
}

export function disposePanel(element) {
    if (!element) return;

    if (element._tabsPanelTransitionFrame) {
        cancelAnimationFrame(element._tabsPanelTransitionFrame);
        delete element._tabsPanelTransitionFrame;
    }

    delete element._tabsPanelTransitionToken;
}

function waitForPanelAnimations(element, token, dotNetRef) {
    if (typeof element.getAnimations !== 'function' || globalThis.BASE_UI_ANIMATIONS_DISABLED) {
        completePanelExitTransition(element, token, dotNetRef);
        return;
    }

    const animations = element.getAnimations();
    if (animations.length === 0) {
        completePanelExitTransition(element, token, dotNetRef);
        return;
    }

    Promise.all(animations.map(animation => animation.finished))
        .then(() => completePanelExitTransition(element, token, dotNetRef))
        .catch(() => {
            if (element._tabsPanelTransitionToken !== token) {
                return;
            }

            const currentAnimations = element.getAnimations();
            const hasRunningAnimations = currentAnimations.some(animation =>
                animation.pending || animation.playState !== 'finished');

            if (hasRunningAnimations) {
                waitForPanelAnimations(element, token, dotNetRef);
            } else {
                completePanelExitTransition(element, token, dotNetRef);
            }
        });
}

function completePanelExitTransition(element, token, dotNetRef) {
    if (element._tabsPanelTransitionToken !== token) {
        return;
    }

    const container = element.parentElement;
    hidePanelImmediately(element);

    invokePanelTransitionCallback(element, token, dotNetRef, 'OnPanelTransitionEnded')
        .finally(() => scheduleOpenPanelCleanup(container));
}

function shouldCompletePanelTransitionImmediately(element) {
    if (globalThis.BASE_UI_ANIMATIONS_DISABLED || typeof element.getAnimations !== 'function') {
        return true;
    }

    if (element.getAnimations().length > 0) {
        return false;
    }

    return !hasCssMotion(element);
}

function hasCssMotion(element) {
    const style = getComputedStyle(element);
    return hasNonZeroTime(style.transitionDuration) || hasNonZeroTime(style.animationDuration);
}

function hasNonZeroTime(value) {
    if (!value) {
        return false;
    }

    return value
        .split(',')
        .some(part => parseCssTime(part.trim()) > 0);
}

function parseCssTime(value) {
    if (value.endsWith('ms')) {
        return Number.parseFloat(value);
    }

    if (value.endsWith('s')) {
        return Number.parseFloat(value) * 1000;
    }

    return 0;
}

function hidePanelImmediately(element) {
    element.hidden = true;
    element.setAttribute('hidden', '');
    element.setAttribute('data-hidden', '');
    element.removeAttribute('data-ending-style');
}

function showPanelImmediately(element) {
    element.hidden = false;
    element.removeAttribute('hidden');
    element.removeAttribute('data-hidden');
}

function scheduleOpenPanelCleanup(container) {
    if (!container) {
        return;
    }

    requestAnimationFrame(() => {
        for (const panel of container.querySelectorAll('[role="tabpanel"][tabindex="0"]')) {
            showPanelImmediately(panel);
        }
    });
}

function invokePanelTransitionCallback(element, token, dotNetRef, methodName) {
    if (element._tabsPanelTransitionToken !== token) {
        return Promise.resolve();
    }

    delete element._tabsPanelTransitionToken;
    return dotNetRef.invokeMethodAsync(methodName).catch(() => { });
}

export function getTabPosition(listElement, tabElement) {
    if (!listElement || !tabElement) {
        return { left: 0, top: 0, width: 0, height: 0 };
    }

    const tabRect = tabElement.getBoundingClientRect();
    const listRect = listElement.getBoundingClientRect();

    const left = tabRect.left - listRect.left + listElement.scrollLeft;
    const top = tabRect.top - listRect.top + listElement.scrollTop;

    return {
        left: left,
        top: top,
        width: tabRect.width,
        height: tabRect.height
    };
}

export function getIndicatorPosition(listElement, tabElement) {
    if (!listElement || !tabElement) {
        return { left: 0, right: 0, top: 0, bottom: 0, width: 0, height: 0 };
    }

    const tabRect = tabElement.getBoundingClientRect();
    const listRect = listElement.getBoundingClientRect();

    const listStyle = getComputedStyle(listElement);
    const listWidth = parseFloat(listStyle.width) || listRect.width;
    const listHeight = parseFloat(listStyle.height) || listRect.height;

    const scaleX = listWidth > 0 ? listRect.width / listWidth : 1;
    const scaleY = listHeight > 0 ? listRect.height / listHeight : 1;

    let left, top, width, height;

    const hasNonZeroScale = Math.abs(scaleX) > Number.EPSILON && Math.abs(scaleY) > Number.EPSILON;

    if (hasNonZeroScale) {
        const tabLeftDelta = tabRect.left - listRect.left;
        const tabTopDelta = tabRect.top - listRect.top;

        left = tabLeftDelta / scaleX + listElement.scrollLeft - listElement.clientLeft;
        top = tabTopDelta / scaleY + listElement.scrollTop - listElement.clientTop;
    } else {
        left = tabElement.offsetLeft;
        top = tabElement.offsetTop;
    }

    const tabStyle = getComputedStyle(tabElement);
    width = parseFloat(tabStyle.width) || tabRect.width;
    height = parseFloat(tabStyle.height) || tabRect.height;

    const right = listElement.scrollWidth - left - width;
    const bottom = listElement.scrollHeight - top - height;

    return {
        left: left,
        right: right,
        top: top,
        bottom: bottom,
        width: width,
        height: height
    };
}

export function observeResize(listElement, dotNetRef) {
    if (!listElement || !dotNetRef) return;

    if (typeof ResizeObserver === 'undefined') return;

    let refs = state.dotNetRefs.get(listElement);
    if (!refs) {
        refs = new Set();
        state.dotNetRefs.set(listElement, refs);
    }
    refs.add(dotNetRef);

    if (state.resizeObservers.has(listElement)) {
        return;
    }

    const observer = new ResizeObserver(() => {
        const currentRefs = state.dotNetRefs.get(listElement);
        if (currentRefs) {
            for (const ref of currentRefs) {
                ref.invokeMethodAsync('OnResizeAsync').catch(() => { });
            }
        }
    });

    observer.observe(listElement);
    for (const item of getOrderedTabs(listElement)) {
        observer.observe(item.element);
    }
    state.resizeObservers.set(listElement, observer);

    requestAnimationFrame(() => notifyIndicatorRefs(listElement));
}

export function unobserveResize(listElement, dotNetRef) {
    if (!listElement) return;

    const refs = state.dotNetRefs.get(listElement);
    if (refs && dotNetRef) {
        refs.delete(dotNetRef);
        if (refs.size > 0) {
            return;
        }
    }

    const observer = state.resizeObservers.get(listElement);
    if (observer) {
        observer.disconnect();
        state.resizeObservers.delete(listElement);
    }

    state.dotNetRefs.delete(listElement);
}

function observeTabForIndicators(listElement, tabElement) {
    const observer = state.resizeObservers.get(listElement);
    if (observer && tabElement) {
        observer.observe(tabElement);
    }
}

function unobserveTabForIndicators(listElement, tabElement) {
    const observer = state.resizeObservers.get(listElement);
    if (observer && tabElement) {
        observer.unobserve(tabElement);
    }
}

function notifyIndicatorRefs(listElement) {
    const refs = state.dotNetRefs.get(listElement);
    if (!refs) return;

    for (const ref of refs) {
        ref.invokeMethodAsync('OnResizeAsync').catch(() => { });
    }
}
