/**
 * BlazorBaseUI Menu Component
 *
 * Menu-specific functionality that builds on the shared floating infrastructure.
 */

import { acquireScrollLock } from './blazor-baseui-scroll-lock.js';
import {
    createHoverInteraction,
    checkForTransitionOrAnimation,
    getMaxTransitionDuration,
    normalizeCollisionAvoidance,
    initializePositioner as floatingInitializePositioner,
    updatePositioner as floatingUpdatePositioner,
    disposePositioner as floatingDisposePositioner
} from './blazor-baseui-floating.js';

const PATIENT_CLICK_THRESHOLD = 500;
const TYPEAHEAD_TIMEOUT = 500;
const STATE_KEY = Symbol.for('BlazorBaseUI.Menu.State');
const MENUBAR_STATE_KEY = Symbol.for('BlazorBaseUI.MenuBar.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        roots: new Map(),
        positioners: new Map(),
        menubarTriggers: new Map(),
        openSequence: 0,
        globalListenersInitialized: false
    };
}
const state = window[STATE_KEY];
state.openSequence ??= 0;
state.menubarTriggers ??= new Map();

function getMenuBarState(element) {
    return window[MENUBAR_STATE_KEY]?.get(element) ?? null;
}

function initGlobalListeners() {
    if (state.globalListenersInitialized) return;

    document.addEventListener('keydown', handleGlobalKeyDown, { capture: true });
    document.addEventListener('pointerdown', handleGlobalPointerDown);
    state.globalListenersInitialized = true;
}

function handleGlobalKeyDown(e) {
    // Find all open menus and track the deepest one and any menubar root
    let menubarRoot = null;
    let topmostRoot = null;
    let openMenuCount = 0;
    const openMenus = [];

    for (const [id, rootState] of state.roots) {
        if (isRootEffectivelyOpen(rootState) && rootState.dotNetRef) {
            openMenus.push(rootState);
            openMenuCount++;

            // Find the deepest (topmost) menu for keyboard handling
            // Nested menus (submenus) take priority over parent menus
            // Among same type, prefer the later one (more recently opened = deeper nesting)
            // Only skip update if current topmostRoot is nested and this one is not
            if (!topmostRoot || !topmostRoot.isNested || rootState.isNested) {
                topmostRoot = rootState;
            }

            if (rootState.menubarElement) {
                menubarRoot = rootState;
            }
        }
    }

    if (!topmostRoot) return;
    // Use stored direction from the root state (passed explicitly from Blazor)
    const isRtl = topmostRoot.direction === 'rtl';

    if (e.key === 'Escape') {
        stopHandledKeyEvent(e);

        // If closeParentOnEsc is true on the topmost (submenu), close all menus in the chain
        if (topmostRoot.closeParentOnEsc && openMenuCount > 1) {
            // Close all open menus
            for (const rootState of openMenus) {
                invokeMenuMethodAsync(rootState.dotNetRef, 'OnEscapeKey');
            }
        } else {
            // Just close the topmost menu (the submenu)
            invokeMenuMethodAsync(topmostRoot.dotNetRef, 'OnEscapeKey');
        }
        return;
    }

    // Get current item info for submenu handling
    const currentItem = topmostRoot.popupElement ?
        getMenuItems(topmostRoot.popupElement)[topmostRoot.activeIndex ?? -1] : null;
    const isSubmenuTrigger = currentItem?.hasAttribute('aria-haspopup');
    const isInSubmenu = topmostRoot.parentType === 'menu';

    // Determine which arrow key opens/closes submenus based on direction
    // LTR: ArrowRight opens, ArrowLeft closes
    // RTL: ArrowLeft opens, ArrowRight closes
    const openSubmenuKey = isRtl ? 'ArrowLeft' : 'ArrowRight';
    const closeSubmenuKey = isRtl ? 'ArrowRight' : 'ArrowLeft';
    const openNestedMenus = openMenus.filter(rootState => rootState.parentType === 'menu');
    const deepestNestedMenu = openNestedMenus[openNestedMenus.length - 1] ?? null;
    const nestedCloseKey = deepestNestedMenu?.direction === 'rtl' ? 'ArrowRight' : 'ArrowLeft';

    if (deepestNestedMenu &&
        e.key === nestedCloseKey &&
        deepestNestedMenu.orientation !== 'horizontal') {
        stopHandledKeyEvent(e);
        invokeMenuMethodAsync(deepestNestedMenu.dotNetRef, 'OnEscapeKey');
        return;
    }

    // Handle arrow key to open submenu (for vertical menus)
    if (e.key === openSubmenuKey && topmostRoot.orientation !== 'horizontal' && isSubmenuTrigger) {
        stopHandledKeyEvent(e);
        openChildSubmenu(currentItem);
        return;
    }

    // Handle arrow key to close submenu (for vertical menus when in a submenu)
    if (e.key === closeSubmenuKey && topmostRoot.orientation !== 'horizontal' && isInSubmenu) {
        stopHandledKeyEvent(e);
        invokeMenuMethodAsync(topmostRoot.dotNetRef, 'OnEscapeKey');
        return;
    }

    // Handle ArrowLeft/Right for menubar navigation
    if (menubarRoot && (e.key === 'ArrowLeft' || e.key === 'ArrowRight')) {
        // For open submenu key on a submenu trigger in menubar context, open it
        if (e.key === openSubmenuKey && isSubmenuTrigger) {
            stopHandledKeyEvent(e);
            openChildSubmenu(currentItem);
            return;
        }

        // Navigate to sibling menubar item
        stopHandledKeyEvent(e);

        // Direction for menubar navigation (also respects RTL)
        const menubarIsRtl = menubarRoot.direction === 'rtl';
        const navDirection = (e.key === 'ArrowRight') !== menubarIsRtl ? 1 : -1;
        navigateMenubarSibling(menubarRoot, navDirection);
        return;
    }

    // Handle arrow key navigation within menu
    if (topmostRoot.popupElement) {
        const items = getMenuItems(topmostRoot.popupElement);
        if (items.length === 0) return;

        const currentIndex = topmostRoot.activeIndex ?? -1;
        let newIndex = currentIndex;
        const isHorizontal = topmostRoot.orientation === 'horizontal';

        // Map arrow keys based on orientation
        const nextKey = isHorizontal ? 'ArrowRight' : 'ArrowDown';
        const prevKey = isHorizontal ? 'ArrowLeft' : 'ArrowUp';

        switch (e.key) {
            case 'ArrowDown':
            case 'ArrowRight':
                if ((isHorizontal && e.key === 'ArrowRight') || (!isHorizontal && e.key === 'ArrowDown')) {
                    e.preventDefault();
                    newIndex = currentIndex < items.length - 1 ? currentIndex + 1 : (topmostRoot.loopFocus ? 0 : currentIndex);
                }
                break;
            case 'ArrowUp':
            case 'ArrowLeft':
                if ((isHorizontal && e.key === 'ArrowLeft') || (!isHorizontal && e.key === 'ArrowUp')) {
                    e.preventDefault();
                    newIndex = currentIndex > 0 ? currentIndex - 1 : (topmostRoot.loopFocus ? items.length - 1 : currentIndex);
                }
                break;
            case 'Home':
                e.preventDefault();
                newIndex = 0;
                break;
            case 'End':
                e.preventDefault();
                newIndex = items.length - 1;
                break;
            case 'Enter':
            case ' ':
                // Space during active typeahead: append to buffer, continue search
                if (e.key === ' ' && topmostRoot.typingBuffer.length > 0) {
                    e.preventDefault();
                    topmostRoot.lastTypeaheadTime = Date.now();

                    if (topmostRoot.typingTimer !== null) {
                        clearTimeout(topmostRoot.typingTimer);
                    }
                    topmostRoot.typingTimer = setTimeout(() => {
                        topmostRoot.typingBuffer = '';
                        topmostRoot.typingTimer = null;
                        topmostRoot.lastTypeaheadTime = 0;
                    }, TYPEAHEAD_TIMEOUT);

                    topmostRoot.typingBuffer += ' ';
                    const spaceSearchString = topmostRoot.typingBuffer;
                    const spaceStartIndex = currentIndex >= 0 ? currentIndex : 0;

                    for (let i = 0; i < items.length; i++) {
                        const idx = (spaceStartIndex + i) % items.length;
                        const label = items[idx].getAttribute('data-label');
                        const text = (label ?? items[idx].textContent)?.trim().toLowerCase() || '';
                        if (text.startsWith(spaceSearchString)) {
                            newIndex = idx;
                            break;
                        }
                    }
                    // Don't clear buffer on no-match for Space (React behavior)
                    break;
                }

                e.preventDefault();
                if (currentIndex >= 0 && currentIndex < items.length) {
                    // Disabled items are focusable but non-activatable
                    if (items[currentIndex].getAttribute('aria-disabled') === 'true') {
                        return;
                    }
                    items[currentIndex].click();
                }
                return;
            default:
                // Multi-character typeahead with accumulated buffer
                if (e.key.length === 1 && !e.ctrlKey && !e.metaKey && !e.altKey) {
                    e.preventDefault();
                    topmostRoot.lastTypeaheadTime = Date.now();

                    // Clear any existing reset timer, start a new 500ms timer
                    if (topmostRoot.typingTimer !== null) {
                        clearTimeout(topmostRoot.typingTimer);
                    }
                    topmostRoot.typingTimer = setTimeout(() => {
                        topmostRoot.typingBuffer = '';
                        topmostRoot.typingTimer = null;
                        topmostRoot.lastTypeaheadTime = 0;
                    }, TYPEAHEAD_TIMEOUT);

                    const char = e.key.toLowerCase();

                    // Repeated-character cycling: if all items have different first two chars,
                    // typing the same letter repeatedly cycles through items starting with that letter
                    const allowCycling = items.every(item => {
                        const text = (item.getAttribute('data-label') ?? item.textContent)?.trim().toLowerCase() || '';
                        return text.length < 2 || text[0] !== text[1];
                    });

                    if (allowCycling && topmostRoot.typingBuffer === char) {
                        // Same letter typed again - reset buffer, search from current+1
                        topmostRoot.typingBuffer = '';
                    }

                    topmostRoot.typingBuffer += char;
                    const searchString = topmostRoot.typingBuffer;
                    const startIndex = ((currentIndex + 1) % items.length + items.length) % items.length;

                    for (let i = 0; i < items.length; i++) {
                        const idx = (startIndex + i) % items.length;
                        const label = items[idx].getAttribute('data-label');
                        const text = (label ?? items[idx].textContent)?.trim().toLowerCase() || '';
                        if (text.startsWith(searchString)) {
                            newIndex = idx;
                            break;
                        }
                    }

                    // No match: clear buffer and end session
                    if (newIndex === currentIndex) {
                        const hasMatch = items.some(item => {
                            const text = (item.getAttribute('data-label') ?? item.textContent)?.trim().toLowerCase() || '';
                            return text.startsWith(searchString);
                        });
                        if (!hasMatch) {
                            topmostRoot.typingBuffer = '';
                            clearTimeout(topmostRoot.typingTimer);
                            topmostRoot.typingTimer = null;
                            topmostRoot.lastTypeaheadTime = 0;
                        }
                    }
                }
                break;
        }

        if (newIndex !== currentIndex && newIndex >= 0 && newIndex < items.length) {
            topmostRoot.activeIndex = newIndex;
            highlightItem(topmostRoot.popupElement, items, newIndex);
            topmostRoot.dotNetRef.invokeMethodAsync('OnActiveIndexChange', newIndex).catch(() => { });

            // Close child submenus when keyboard-navigating to a non-submenu-trigger item
            if (!items[newIndex].hasAttribute('aria-haspopup')) {
                closeChildSubmenus(topmostRoot.rootId);
            }
        }
    }
}

function navigateMenubarSibling(menubarRoot, direction) {
    const menubarElement = menubarRoot.menubarElement;
    if (!menubarElement) return;

    const menuBarState = getMenuBarState(menubarElement);
    const loopFocus = menuBarState?.loopFocus ?? true;
    const triggers = Array.from(menubarElement.querySelectorAll('[aria-haspopup="menu"]'))
        .filter(trigger => {
            if (!(trigger instanceof HTMLElement) || !document.contains(trigger)) {
                return false;
            }

            if (trigger.hasAttribute('disabled') && !trigger.hasAttribute('data-focusable')) {
                return false;
            }

            if (trigger.getAttribute('aria-disabled') === 'true' && !trigger.hasAttribute('data-focusable')) {
                return false;
            }

            return true;
        });
    if (triggers.length === 0) return;

    // Find the current trigger (the one whose menu is open)
    const currentTrigger = menubarRoot.triggerElement;
    const currentIndex = triggers.indexOf(currentTrigger);
    if (currentIndex === -1) return;

    let nextIndex = currentIndex + direction;
    if (nextIndex < 0 || nextIndex >= triggers.length) {
        if (!loopFocus) {
            return;
        }

        nextIndex = nextIndex < 0 ? triggers.length - 1 : 0;
    }

    const nextTrigger = triggers[nextIndex];
    if (!nextTrigger || nextTrigger === currentTrigger) return;

    // Close current menu first
    menubarRoot.dotNetRef.invokeMethodAsync('OnSiblingOpen').catch(() => { });

    // Focus the next trigger and click it to open its menu
    setTimeout(() => {
        nextTrigger.focus();
        if (typeof PointerEvent === 'function') {
            nextTrigger.dispatchEvent(new PointerEvent('pointerdown', {
                bubbles: true,
                cancelable: true,
                pointerId: 1,
                pointerType: 'mouse',
                isPrimary: true,
                button: 0,
                buttons: 1
            }));
        } else {
            nextTrigger.dispatchEvent(new MouseEvent('pointerdown', {
                bubbles: true,
                cancelable: true,
                button: 0,
                buttons: 1
            }));
        }
        nextTrigger.click();
    }, 10);
}

function getTextDirection(element) {
    if (!element) return 'ltr';

    // Check computed style direction
    const computedDirection = getComputedStyle(element).direction;
    if (computedDirection === 'rtl') return 'rtl';

    // Also check dir attribute up the DOM tree
    let current = element;
    while (current) {
        const dir = current.getAttribute?.('dir');
        if (dir === 'rtl' || dir === 'ltr') return dir;
        current = current.parentElement;
    }

    // Default to document direction or 'ltr'
    return document.documentElement.getAttribute('dir') || 'ltr';
}

function getMenuItems(popupElement) {
    if (!popupElement) return [];

    // Include disabled items in keyboard navigation (focusableWhenDisabled: true).
    // Disabled items are focusable but non-activatable - the activation guard is
    // in the keydown handler (Enter/Space) and click handler.
    const selector = '[role="menuitem"], [role="menuitemcheckbox"], [role="menuitemradio"]';

    return Array.from(popupElement.querySelectorAll(selector));
}

function updateItemHighlight(items, index) {
    items.forEach((item, i) => {
        if (i === index) {
            item.setAttribute('data-highlighted', '');
            item.setAttribute('tabindex', '0');
        } else {
            item.removeAttribute('data-highlighted');
            item.setAttribute('tabindex', '-1');
        }
    });
}

function highlightItem(popupElement, items, index) {
    updateItemHighlight(items, index);
    if (index >= 0 && index < items.length) {
        items[index].focus();
    }
}

function closeChildSubmenus(rootId) {
    const parentState = state.roots.get(rootId);
    if (!parentState?.popupElement) return;

    for (const [childId, childState] of state.roots) {
        if (childId === rootId || !childState.isOpen || !childState.isNested) continue;
        if (childState.triggerElement && parentState.popupElement.contains(childState.triggerElement)) {
            childState.dotNetRef?.invokeMethodAsync('OnEscapeKey').catch(() => {});
        }
    }
}

function setupPopupMouseDelegation(rootId, rootState, popupElement) {
    cleanupPopupMouseDelegation(rootState, popupElement);

    const handler = (e) => {
        if (!rootState.highlightItemOnHover) return;

        const item = e.target.closest('[role="menuitem"], [role="menuitemcheckbox"], [role="menuitemradio"]');
        if (!item || !popupElement.contains(item)) return;

        const items = getMenuItems(popupElement);
        const index = items.indexOf(item);
        if (index === -1) return;

        rootState.activeIndex = index;
        updateItemHighlight(items, index);

        // Close child submenus when hovering a non-submenu-trigger item
        if (!item.hasAttribute('aria-haspopup')) {
            closeChildSubmenus(rootId);
        }
    };

    popupElement.addEventListener('mouseover', handler);
    rootState.popupMouseHandler = handler;
}

function cleanupPopupMouseDelegation(rootState, popupElement) {
    if (rootState.popupMouseHandler && popupElement) {
        popupElement.removeEventListener('mouseover', rootState.popupMouseHandler);
        rootState.popupMouseHandler = null;
    }
}

function handleGlobalPointerDown(e) {
    for (const [id, rootState] of state.roots) {
        if (!rootState.isOpen || !rootState.dotNetRef) continue;

        const { triggerElement, popupElement } = rootState;

        let clickedInsidePopup = false;
        let clickedOnTrigger = false;

        if (popupElement && popupElement.contains(e.target)) {
            clickedInsidePopup = true;
        }

        if (triggerElement && triggerElement.contains(e.target)) {
            clickedOnTrigger = true;
        }

        const allMenuPopups = document.querySelectorAll('[role="menu"]');
        for (const popup of allMenuPopups) {
            if (popup.contains(e.target)) {
                clickedInsidePopup = true;
                break;
            }
        }

        if (!clickedInsidePopup && !clickedOnTrigger) {
            // Context menu grace period: don't dismiss within 500ms of opening
            // to prevent long-press touch from immediately closing the menu
            if (rootState.allowOutsidePressAt && Date.now() < rootState.allowOutsidePressAt) {
                continue;
            }

            // Touch close prevention: don't dismiss via touch within 300ms of
            // opening via trigger-focus to prevent focus->open->click->close flicker
            if (rootState.allowTouchToCloseAt && Date.now() < rootState.allowTouchToCloseAt
                && e.pointerType === 'touch') {
                continue;
            }

            rootState.dotNetRef.invokeMethodAsync('OnOutsidePress').catch(() => { });
        }
    }
}

// ============================================================================
// Root Management
// ============================================================================

export function initializeRoot(rootId, dotNetRef, closeParentOnEsc, loopFocus, modal, menubarElement, orientation, highlightItemOnHover, direction, isNested, finalFocusMode, finalFocusElement, parentType) {
    initGlobalListeners();

    state.roots.set(rootId, {
        dotNetRef,
        isOpen: false,
        triggerElement: null,
        positionerElement: null,
        popupElement: null,
        activeIndex: -1,
        loopFocus: loopFocus ?? true,
        closeParentOnEsc: closeParentOnEsc || false,
        modal: modal ?? true,
        releaseScrollLock: null,
        hoverInteraction: null,
        openSequence: 0,
        menubarElement: menubarElement || null,
        orientation: orientation || 'vertical',
        highlightItemOnHover: highlightItemOnHover ?? true,
        direction: direction || 'ltr',
        isNested: isNested || false,
        finalFocusMode: finalFocusMode || null,
        finalFocusElement: finalFocusElement || null,
        parentType: parentType || null,
        allowOutsidePressAt: null,
        allowTouchToCloseAt: null,
        hoverDisabledByClick: false,
        popupClickHandler: null,
        popupMouseHandler: null,
        stickIfOpen: false,
        patientClickTimeout: null,
        rootId: rootId,
        lastTypeaheadTime: 0,
        typingBuffer: '',
        typingTimer: null,
        allowMouseUpTrigger: false,
        popupMouseUpHandler: null
    });
}

export function updateRoot(rootId, modal, orientation, loopFocus, highlightItemOnHover, direction) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;
    rootState.modal = modal ?? true;
    rootState.orientation = orientation || 'vertical';
    rootState.loopFocus = loopFocus ?? true;
    rootState.highlightItemOnHover = highlightItemOnHover ?? true;
    rootState.direction = direction || 'ltr';
}

function isRootEffectivelyOpen(rootState) {
    return rootState.isOpen || rootState.popupElement?.hasAttribute('data-open') === true;
}

function stopHandledKeyEvent(event) {
    event.preventDefault();
    event.stopPropagation();
    event.stopImmediatePropagation?.();
}

function closeSiblingMenubarRoots(rootState) {
    if (rootState.parentType !== 'menubar') return;

    for (const siblingState of state.roots.values()) {
        const sameMenubar = rootState.menubarElement && siblingState.menubarElement
            ? siblingState.menubarElement === rootState.menubarElement
            : siblingState.parentType === 'menubar';

        if (siblingState === rootState ||
            !sameMenubar ||
            !isRootEffectivelyOpen(siblingState) ||
            !siblingState.dotNetRef) {
            continue;
        }

        siblingState.dotNetRef.invokeMethodAsync('OnSiblingOpen').catch(() => { });
    }
}

function invokeMenuMethodAsync(dotNetRef, methodName) {
    setTimeout(() => {
        dotNetRef?.invokeMethodAsync(methodName).catch(() => { });
    }, 0);
}

function openChildSubmenu(triggerElement) {
    const childRoot = findChildRootForTrigger(triggerElement);
    if (childRoot?.dotNetRef) {
        childRoot.dotNetRef.invokeMethodAsync('OnHoverOpen').catch(() => { });
    } else {
        triggerElement.click();
    }
}

function findChildRootForTrigger(triggerElement) {
    for (const rootState of state.roots.values()) {
        if (rootState.isNested && rootState.triggerElement === triggerElement) {
            return rootState;
        }
    }

    return null;
}

export function disposeRoot(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        // Release scroll lock if this menu had it
        if (rootState.releaseScrollLock) {
            rootState.releaseScrollLock();
            rootState.releaseScrollLock = null;
        }
        // Clean up hover interaction
        if (rootState.hoverInteraction) {
            rootState.hoverInteraction.cleanup();
        }
        // Clean up composite key suppression
        rootState.compositeKeyCleanup?.();
        // Clean up mouseup arm timeout
        if (rootState._mouseUpArmTimeout) {
            clearTimeout(rootState._mouseUpArmTimeout);
            rootState._mouseUpArmTimeout = null;
        }
        // Clean up mouse delegation handler
        cleanupPopupMouseDelegation(rootState, rootState.popupElement);
    }
    state.roots.delete(rootId);
}

// ============================================================================
// Hover Interaction Support
// ============================================================================

export async function initializeHoverInteraction(rootId, triggerElement, openDelay, closeDelay, callbackDotNetRef) {
    let rootState = state.roots.get(rootId);

    // For handle-based triggers, create a lightweight state entry if root doesn't exist
    if (!rootState && callbackDotNetRef) {
        rootState = { triggerElement, isOpen: false };
        state.roots.set(rootId, rootState);
    }

    // If root state doesn't exist yet, wait with retries for it to be initialized
    // This handles the case where the trigger's OnAfterRender runs before the root's InitializeJsAsync completes
    // On Server-side Blazor, SignalR latency can make this take longer, so we allow more retries
    if (!rootState) {
        for (let attempt = 0; attempt < 30; attempt++) {
            await new Promise(resolve => setTimeout(resolve, 100));
            rootState = state.roots.get(rootId);
            if (rootState) break;
        }
        if (!rootState) return;
    }

    // Store the trigger element if provided
    if (triggerElement) {
        rootState.triggerElement = triggerElement;
    }

    if (!rootState.triggerElement) return;

    // Clean up existing hover interaction and allowMouseEnter listeners
    if (rootState.hoverInteraction) {
        rootState.hoverInteraction.cleanup();
    }
    rootState.allowMouseEnterCleanup?.();
    rootState.allowMouseEnterCleanup = null;

    // Use callback dotnet ref if provided, otherwise fall back to root dotnet ref
    const dotNetRef = callbackDotNetRef || rootState.dotNetRef;

    // allowMouseEnter starts false - hover opens instantly until deliberate mouse movement
    const configuredOpenDelay = openDelay || 0;
    const configuredCloseDelay = closeDelay || 0;
    rootState.allowMouseEnter = false;
    rootState.openDelay = configuredOpenDelay;
    rootState.closeDelay = configuredCloseDelay;

    rootState.hoverInteraction = createHoverInteraction({
        interactionId: `menu-hover-${rootId}`,
        triggerElement: rootState.triggerElement,
        floatingElement: rootState.popupElement,
        openDelay: configuredOpenDelay,
        closeDelay: configuredCloseDelay,
        mouseOnly: true,
        useSafePolygon: true,
        safePolygonOptions: { blockPointerEvents: true },
        onOpen: (reason) => {
            // Skip if we're within the ignore period (e.g., after keyboard close)
            if (rootState.ignoreHoverUntil && Date.now() < rootState.ignoreHoverUntil) {
                return;
            }
            // Skip if hover was disabled by a click inside the popup
            if (rootState.hoverDisabledByClick) {
                return;
            }
            if (dotNetRef && !rootState.isOpen) {
                const openTask = dotNetRef.invokeMethodAsync('OnHoverOpen').catch(() => { });
                if (rootState.parentType === 'menubar') {
                    openTask.then(() => closeSiblingMenubarRoots(rootState));
                }
            }
        },
        onClose: (reason) => {
            if (rootState.parentType === 'menubar') {
                return;
            }

            if (dotNetRef && rootState.isOpen) {
                dotNetRef.invokeMethodAsync('OnHoverClose').catch(() => { });
            }
        }
    });
    rootState.hoverInteraction.setOpen(!!rootState.isOpen);

    // Once mouse moves over trigger or popup, switch to configured delays
    function onAllowMouseEnter() {
        if (!rootState.allowMouseEnter) {
            rootState.allowMouseEnter = true;
            rootState.hoverInteraction?.setDelays(configuredOpenDelay, configuredCloseDelay);
        }
    }
    rootState.triggerElement.addEventListener('mousemove', onAllowMouseEnter);
    if (rootState.popupElement) {
        rootState.popupElement.addEventListener('mousemove', onAllowMouseEnter);
    }
    rootState.allowMouseEnterCleanup = () => {
        rootState.triggerElement?.removeEventListener('mousemove', onAllowMouseEnter);
        rootState.popupElement?.removeEventListener('mousemove', onAllowMouseEnter);
    };
}

export function closeMenubarSiblingRoots(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        closeSiblingMenubarRoots(rootState);
    }
}

export function initializeMenubarTrigger(interactionId, triggerElement, dotNetRef) {
    disposeMenubarTrigger(interactionId);

    if (!triggerElement || !dotNetRef) {
        return;
    }

    let lastOpenAt = 0;
    const onPointerEnter = () => {
        const now = Date.now();
        if (now - lastOpenAt < 20) {
            return;
        }

        lastOpenAt = now;
        dotNetRef.invokeMethodAsync('OnHoverOpen').catch(() => { });
    };
    const onKeyDown = (event) => {
        if (event.key === 'Enter' || event.key === ' ' || event.key === 'Spacebar' || event.code === 'Space') {
            dotNetRef.invokeMethodAsync('OnKeyboardOpen').catch(() => { });
        }
    };

    triggerElement.addEventListener('pointerenter', onPointerEnter);
    triggerElement.addEventListener('pointerover', onPointerEnter);
    triggerElement.addEventListener('mouseenter', onPointerEnter);
    triggerElement.addEventListener('keydown', onKeyDown);
    state.menubarTriggers.set(interactionId, () => {
        triggerElement.removeEventListener('pointerenter', onPointerEnter);
        triggerElement.removeEventListener('pointerover', onPointerEnter);
        triggerElement.removeEventListener('mouseenter', onPointerEnter);
        triggerElement.removeEventListener('keydown', onKeyDown);
    });
}

export function disposeMenubarTrigger(interactionId) {
    const cleanup = state.menubarTriggers.get(interactionId);
    if (cleanup) {
        cleanup();
        state.menubarTriggers.delete(interactionId);
    }
}

export function disposeHoverInteraction(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.allowMouseEnterCleanup?.();
        rootState.allowMouseEnterCleanup = null;
        if (rootState.hoverInteraction) {
            rootState.hoverInteraction.cleanup();
            rootState.hoverInteraction = null;
        }
    }
}

function clearPatientClickTimeout(rootState) {
    if (rootState.patientClickTimeout !== null) {
        clearTimeout(rootState.patientClickTimeout);
        rootState.patientClickTimeout = null;
    }
    rootState.stickIfOpen = false;
}

export function consumeStickIfOpen(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return false;
    if (rootState.stickIfOpen) {
        clearPatientClickTimeout(rootState);
        return true;
    }
    return false;
}

/**
 * Arms the click-and-drag mouseup activation on the popup.
 * Called by MenuTrigger on pointerdown to enable drag-to-select.
 * After 200ms, releasing the mouse over a menu item will activate it.
 */
export function armMouseUpTrigger(rootId) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.allowMouseUpTrigger = false;

    if (rootState._mouseUpArmTimeout) {
        clearTimeout(rootState._mouseUpArmTimeout);
    }

    rootState._mouseUpArmTimeout = setTimeout(() => {
        rootState.allowMouseUpTrigger = true;
        rootState._mouseUpArmTimeout = null;
    }, 200);
}

export function updateHoverInteractionFloatingElement(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState?.hoverInteraction && rootState.popupElement) {
        rootState.hoverInteraction.setFloatingElement(rootState.popupElement);
    }
}

export function setHoverInteractionOpen(rootId, isOpen) {
    const rootState = state.roots.get(rootId);
    if (rootState?.hoverInteraction) {
        rootState.hoverInteraction.setOpen(isOpen);
    }
}

// ============================================================================
// Open/Close State
// ============================================================================

export async function setRootOpen(rootId, isOpen, reason, highlightLast, interactionType) {
    let rootState = state.roots.get(rootId);

    // If root state doesn't exist yet, wait briefly for it to be initialized
    // This handles the race condition on Server-side Blazor where setRootOpen
    // may be called before initializeRoot completes
    if (!rootState) {
        await new Promise(resolve => setTimeout(resolve, 50));
        rootState = state.roots.get(rootId);
        if (!rootState) return;
    }

    rootState.isOpen = isOpen;
    rootState.pendingOpen = isOpen;
    rootState.openReason = reason;
    rootState.highlightLast = highlightLast || false;

    // Sync with hover interaction
    if (rootState.hoverInteraction) {
        rootState.hoverInteraction.setOpen(isOpen);
        if (!isOpen) {
            rootState.allowMouseEnter = false;
            rootState.hoverInteraction.setDelays(rootState.openDelay ?? 0, rootState.closeDelay ?? 0);
        }
    }

    if (isOpen) {
        rootState.openSequence = ++state.openSequence;

        // For menubar menus, don't auto-highlight - user must press arrow key first
        // For other menus, start with first item highlighted (accessibility best practice)
        // If highlightLast is true, we'll set the index after we know the item count
        rootState.activeIndex = rootState.menubarElement ? -1 : (highlightLast ? -2 : 0);

        // Apply scroll lock if modal and not opened via hover or touch (guard against double acquisition)
        if (rootState.modal && reason !== 'trigger-hover' && interactionType !== 'touch' && !rootState.releaseScrollLock) {
            rootState.releaseScrollLock = acquireScrollLock(rootState.positionerElement);
        }

        // Touch close prevention: after opening via trigger-focus, block touch-click
        // dismissals for 300ms to prevent focus->open->click->close flicker on mobile
        if (reason === 'trigger-focus') {
            rootState.allowTouchToCloseAt = Date.now() + 300;
        } else {
            rootState.allowTouchToCloseAt = null;
        }

        // Context menu grace period: after opening from touch, block outside-press
        // dismissals for 500ms to prevent long-press touch from immediately closing.
        if (rootState.parentType === 'context-menu' && interactionType === 'touch') {
            rootState.allowOutsidePressAt = Date.now() + 500;
        } else {
            rootState.allowOutsidePressAt = null;
        }

        // Patient click protection: when hover-opened, suppress clicks for 500ms
        if (reason === 'trigger-hover') {
            clearPatientClickTimeout(rootState);
            rootState.stickIfOpen = true;
            rootState.patientClickTimeout = setTimeout(() => {
                rootState.stickIfOpen = false;
                rootState.patientClickTimeout = null;
            }, PATIENT_CLICK_THRESHOLD);
        }

        waitForPopupAndStartTransition(rootState, isOpen);
    } else {
        // Release scroll lock if this menu acquired it
        if (rootState.releaseScrollLock) {
            rootState.releaseScrollLock();
            rootState.releaseScrollLock = null;
        }

        // Return focus when menu closes via keyboard or item click
        // Don't focus for hover-based closes or outside clicks (user clicked elsewhere)
        const shouldReturnFocus = reason === 'escape-key' || reason === 'item-press' || reason === 'close-press';
        if (shouldReturnFocus && rootState.finalFocusMode !== 'none') {
            const focusTarget = rootState.finalFocusMode === 'element' && rootState.finalFocusElement
                ? rootState.finalFocusElement
                : rootState.triggerElement;

            if (focusTarget) {
                // Use setTimeout to ensure focus happens after the menu is fully closed
                setTimeout(() => {
                    if (focusTarget && document.contains(focusTarget)) {
                        focusTarget.focus();
                    }
                }, 0);
            }
        }

        // For keyboard closes, temporarily disable hover interaction to prevent immediate reopen
        // This handles the case where mouse is still hovering when Escape is pressed
        if (reason === 'escape-key' && rootState.hoverInteraction) {
            rootState.ignoreHoverUntil = Date.now() + 300;
        }

        // Clear grace period timers
        rootState.allowOutsidePressAt = null;
        rootState.allowTouchToCloseAt = null;

        // Reset hover click suppression so hover works on next open
        rootState.hoverDisabledByClick = false;

        // Clear patient click protection
        clearPatientClickTimeout(rootState);

        // Clear typeahead state
        rootState.typingBuffer = '';
        if (rootState.typingTimer !== null) {
            clearTimeout(rootState.typingTimer);
            rootState.typingTimer = null;
        }
        rootState.lastTypeaheadTime = 0;

        // Clean up popup click handler
        if (rootState.popupClickHandler && rootState.popupElement) {
            rootState.popupElement.removeEventListener('click', rootState.popupClickHandler);
            rootState.popupClickHandler = null;
        }

        // Clean up popup mouseup handler (click-and-drag)
        if (rootState.popupMouseUpHandler && rootState.popupElement) {
            rootState.popupElement.removeEventListener('mouseup', rootState.popupMouseUpHandler);
            rootState.popupMouseUpHandler = null;
        }
        rootState.allowMouseUpTrigger = false;
        if (rootState._mouseUpArmTimeout) {
            clearTimeout(rootState._mouseUpArmTimeout);
            rootState._mouseUpArmTimeout = null;
        }

        // Clean up mouse delegation handler
        cleanupPopupMouseDelegation(rootState, rootState.popupElement);

        startTransition(rootState, isOpen);
    }
}

export function setActiveIndex(rootId, index) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;

    rootState.activeIndex = index;

    if (rootState.popupElement) {
        const items = getMenuItems(rootState.popupElement);
        if (index >= 0 && index < items.length) {
            highlightItem(rootState.popupElement, items, index);
        }
    }
}

// ============================================================================
// Transition Handling
// ============================================================================

function waitForPopupAndStartTransition(rootState, isOpen) {
    const popupElement = rootState.popupElement;

    if (popupElement) {
        if (isOpen) {
            // Wait for menu items to be rendered before highlighting
            waitForItemsAndHighlight(rootState, popupElement);

            // Add click listener to suppress hover re-opens after click interactions
            if (!rootState.popupClickHandler) {
                rootState.popupClickHandler = () => {
                    rootState.hoverDisabledByClick = true;
                };
                popupElement.addEventListener('click', rootState.popupClickHandler);
            }

            // Add mouseup listener for click-and-drag from trigger activation
            // When allowMouseUpTrigger is set (by trigger pointerdown), releasing
            // the mouse over a menu item activates it - matching React's behavior.
            if (!rootState.popupMouseUpHandler) {
                rootState.popupMouseUpHandler = (e) => {
                    if (!rootState.allowMouseUpTrigger) return;
                    rootState.allowMouseUpTrigger = false;

                    const item = e.target.closest('[role="menuitem"], [role="menuitemcheckbox"], [role="menuitemradio"]');
                    if (!item || !popupElement.contains(item)) return;
                    if (item.getAttribute('aria-disabled') === 'true') return;

                    // Only activate regular items, not submenu triggers
                    if (!item.hasAttribute('aria-haspopup')) {
                        item.click();
                    }
                };
                popupElement.addEventListener('mouseup', rootState.popupMouseUpHandler);
            }
        }
        startTransition(rootState, isOpen);
        return;
    }

    let attempts = 0;
    const maxAttempts = 10;

    function checkForPopup() {
        attempts++;
        const element = rootState.popupElement;

        if (element) {
            // Update hover interaction with the new popup element
            if (rootState.hoverInteraction) {
                rootState.hoverInteraction.setFloatingElement(element);
            }
            if (rootState.pendingOpen === isOpen) {
                if (isOpen) {
                    // Wait for menu items to be rendered before highlighting
                    waitForItemsAndHighlight(rootState, element);
                }
                startTransition(rootState, isOpen);
            }
        } else if (attempts < maxAttempts && rootState.pendingOpen === isOpen) {
            requestAnimationFrame(checkForPopup);
        } else if (rootState.dotNetRef && rootState.pendingOpen === isOpen) {
            rootState.dotNetRef.invokeMethodAsync('OnStartingStyleApplied').catch(() => { });
        }
    }

    requestAnimationFrame(checkForPopup);
}

function waitForItemsAndHighlight(rootState, popupElement) {
    // -1 means no highlight (menubar), -2 means highlight last item
    if (rootState.activeIndex === -1) return;

    let attempts = 0;
    const maxAttempts = 10;
    let postHighlightFrames = 0;
    const maxPostHighlightFrames = 5;
    let focusedInitialItem = false;

    function checkForItems() {
        attempts++;
        const items = getMenuItems(popupElement);

        if (items.length > 0) {
            // If activeIndex is -2, highlight the last item
            let indexToHighlight = rootState.activeIndex;
            if (indexToHighlight === -2) {
                indexToHighlight = items.length - 1;
                rootState.activeIndex = indexToHighlight;
                // Notify .NET of the actual index
                if (rootState.dotNetRef) {
                    rootState.dotNetRef.invokeMethodAsync('OnActiveIndexChange', indexToHighlight).catch(() => { });
                }
            }
            if (focusedInitialItem) {
                updateItemHighlight(items, indexToHighlight);
            } else {
                highlightItem(popupElement, items, indexToHighlight);
                focusedInitialItem = true;
            }
            if (postHighlightFrames < maxPostHighlightFrames && rootState.isOpen) {
                postHighlightFrames++;
                requestAnimationFrame(checkForItems);
            }
        } else if (attempts < maxAttempts && rootState.isOpen) {
            requestAnimationFrame(checkForItems);
        }
    }

    requestAnimationFrame(checkForItems);
}

function startTransition(rootState, isOpen) {
    const popupElement = rootState.popupElement;

    if (!popupElement) {
        if (rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
        }
        return;
    }

    const hasTransition = checkForTransitionOrAnimation(popupElement);

    if (isOpen) {
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                if (rootState.pendingOpen !== isOpen) {
                    return;
                }
                if (hasTransition) {
                    setupTransitionEndListener(rootState, isOpen);
                } else {
                    // No transition - immediately notify that transition is complete
                    if (rootState.dotNetRef) {
                        rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
                    }
                }
                if (rootState.dotNetRef) {
                    rootState.dotNetRef.invokeMethodAsync('OnStartingStyleApplied').catch(() => { });
                }
            });
        });
    } else {
        if (hasTransition) {
            setupTransitionEndListener(rootState, isOpen);
        } else {
            if (rootState.dotNetRef) {
                rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
            }
        }
    }
}

function setupTransitionEndListener(rootState, isOpen) {
    const popupElement = rootState.popupElement;
    if (!popupElement) return;

    if (rootState.transitionCleanup) {
        rootState.transitionCleanup();
        rootState.transitionCleanup = null;
    }
    if (rootState.fallbackTimeoutId) {
        clearTimeout(rootState.fallbackTimeoutId);
        rootState.fallbackTimeoutId = null;
    }

    let called = false;
    const handleEnd = (event) => {
        if (event.target !== popupElement) return;
        if (called) return;
        called = true;
        cleanup();
        if (rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
        }
    };

    const cleanup = () => {
        popupElement.removeEventListener('transitionend', handleEnd);
        popupElement.removeEventListener('animationend', handleEnd);
        if (rootState.fallbackTimeoutId) {
            clearTimeout(rootState.fallbackTimeoutId);
            rootState.fallbackTimeoutId = null;
        }
        rootState.transitionCleanup = null;
    };

    popupElement.addEventListener('transitionend', handleEnd);
    popupElement.addEventListener('animationend', handleEnd);

    rootState.transitionCleanup = cleanup;

    const fallbackTimeout = getMaxTransitionDuration(popupElement);
    rootState.fallbackTimeoutId = setTimeout(() => {
        if (!called && rootState.dotNetRef) {
            called = true;
            cleanup();
            rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
        }
    }, fallbackTimeout);
}

// ============================================================================
// Element References
// ============================================================================

export async function setTriggerElement(rootId, element) {
    let rootState = state.roots.get(rootId);

    // Wait for root state to be initialized if not yet available
    if (!rootState) {
        for (let attempt = 0; attempt < 10; attempt++) {
            await new Promise(resolve => setTimeout(resolve, 50));
            rootState = state.roots.get(rootId);
            if (rootState) break;
        }
        if (!rootState) return;
    }

    rootState.triggerElement = element;
}

export async function setPositionerElement(rootId, element) {
    let rootState = state.roots.get(rootId);

    // Wait for root state to be initialized if not yet available
    if (!rootState) {
        for (let attempt = 0; attempt < 10; attempt++) {
            await new Promise(resolve => setTimeout(resolve, 50));
            rootState = state.roots.get(rootId);
            if (rootState) break;
        }
        if (!rootState) return;
    }

    rootState.positionerElement = element;
}

const COMPOSITE_KEYS = new Set(['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End']);

function setupCompositeKeySuppression(rootState) {
    rootState.compositeKeyCleanup?.();
    const popup = rootState.popupElement;
    if (!popup) return;
    const handler = (e) => {
        if (COMPOSITE_KEYS.has(e.key)) {
            e.stopPropagation();
        }
    };
    popup.addEventListener('keydown', handler);
    rootState.compositeKeyCleanup = () => popup.removeEventListener('keydown', handler);
}

export async function setPopupElement(rootId, element, insideToolbar) {
    let rootState = state.roots.get(rootId);

    // Wait for root state to be initialized if not yet available
    if (!rootState) {
        for (let attempt = 0; attempt < 10; attempt++) {
            await new Promise(resolve => setTimeout(resolve, 50));
            rootState = state.roots.get(rootId);
            if (rootState) break;
        }
        if (!rootState) return;
    }

    rootState.popupElement = element;
    rootState.insideToolbar = !!insideToolbar;

    // Set up mouse highlight delegation for cross-mode (keyboard->mouse) consistency
    if (element) {
        setupPopupMouseDelegation(rootId, rootState, element);
    }

    // Prevent composite keys (arrows, Home, End) from propagating to toolbar
    if (rootState.insideToolbar) {
        setupCompositeKeySuppression(rootState);
    }

    // Update hover interaction with the new popup element
    if (rootState.hoverInteraction && element) {
        rootState.hoverInteraction.setFloatingElement(element);
    }
}

// ============================================================================
// Positioning (delegated to shared floating module)
// ============================================================================

export async function initializePositioner(positionerElement, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, disableAnchorTracking, collisionAvoidanceSide, collisionAvoidanceAlign, collisionAvoidanceFallback, dotNetRef, hasViewport, shiftCrossAxis) {
    let onPositionUpdated = null;
    if (dotNetRef) {
        onPositionUpdated = (effectiveSide, effectiveAlign, anchorHidden, arrowUncentered) => {
            dotNetRef.invokeMethodAsync('OnPositionUpdated', effectiveSide, effectiveAlign, anchorHidden, arrowUncentered).catch(() => { });
        };
    }

    const positionerId = await floatingInitializePositioner({
        positionerElement,
        triggerElement,
        side,
        align,
        sideOffset,
        alignOffset,
        collisionPadding,
        collisionBoundary: collisionBoundary || 'clipping-ancestors',
        arrowPadding,
        arrowElement,
        sticky: sticky || false,
        positionMethod: positionMethod || 'absolute',
        disableAnchorTracking: disableAnchorTracking || false,
        collisionAvoidance: normalizeCollisionAvoidance({ side: collisionAvoidanceSide, align: collisionAvoidanceAlign, fallbackAxisSide: collisionAvoidanceFallback }),
        onPositionUpdated,
        dotNetRef: dotNetRef || null,
        hasViewport: hasViewport || false,
        shiftCrossAxis: shiftCrossAxis || false
    });

    if (positionerId) {
        state.positioners.set(positionerId, { positionerId });
    }

    return positionerId;
}

export async function updatePosition(positionerId, triggerElement, side, align, sideOffset, alignOffset, collisionPadding, collisionBoundary, arrowPadding, arrowElement, sticky, positionMethod, collisionAvoidanceSide, collisionAvoidanceAlign, collisionAvoidanceFallback, shiftCrossAxis) {
    await floatingUpdatePositioner(positionerId, {
        triggerElement,
        side,
        align,
        sideOffset,
        alignOffset,
        collisionPadding,
        collisionBoundary: collisionBoundary || 'clipping-ancestors',
        arrowPadding,
        arrowElement,
        sticky: sticky || false,
        positionMethod: positionMethod || 'absolute',
        collisionAvoidance: normalizeCollisionAvoidance({ side: collisionAvoidanceSide, align: collisionAvoidanceAlign, fallbackAxisSide: collisionAvoidanceFallback }),
        shiftCrossAxis: shiftCrossAxis || false
    });
}

export function disposePositioner(positionerId) {
    floatingDisposePositioner(positionerId);
    state.positioners.delete(positionerId);
}

// ============================================================================
// Viewport Content Transitions
// ============================================================================

const DIRECTION_TOLERANCE = 5;

export function initializeViewport(rootId, viewportElement, dotNetRef) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        rootState.viewportElement = viewportElement;
        rootState.viewportDotNetRef = dotNetRef;
    }
}

export function disposeViewport(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        // Remove any leftover cloned elements
        if (rootState.viewportElement?.parentNode) {
            const parent = rootState.viewportElement.parentNode;
            const clones = parent.querySelectorAll('[data-previous]');
            clones.forEach(clone => clone.remove());
        }
        rootState.viewportElement = null;
        rootState.viewportDotNetRef = null;
    }
}

export function initializeAutoResize(rootId, side, direction) {
    const rootState = state.roots.get(rootId);
    if (!rootState) return;
    rootState.currentSide = side || 'bottom';
    rootState.direction = direction || 'ltr';
    setupMenuAutoResize(rootState);
}

export function disposeAutoResize(rootId) {
    const rootState = state.roots.get(rootId);
    if (rootState) {
        cleanupMenuAutoResize(rootState);
    }
}

export function onViewportTriggerChange(rootId, previousTriggerElement, newTriggerElement) {
    const rootState = state.roots.get(rootId);
    if (!rootState?.viewportElement || !rootState.viewportDotNetRef) return;

    const currentContainer = rootState.viewportElement;
    const parent = currentContainer.parentNode;
    if (!parent) return;

    // Clone the current container as the "previous" content
    const clone = currentContainer.cloneNode(true);
    clone.removeAttribute('data-current');
    clone.setAttribute('data-previous', '');
    clone.setAttribute('inert', '');

    // Set dimensions on the clone for CSS transition use
    const width = currentContainer.offsetWidth;
    const height = currentContainer.offsetHeight;
    clone.style.setProperty('--popup-width', `${width}px`);
    clone.style.setProperty('--popup-height', `${height}px`);
    clone.style.position = 'absolute';

    // Calculate activation direction from trigger positions
    const prevRect = previousTriggerElement.getBoundingClientRect();
    const newRect = newTriggerElement.getBoundingClientRect();

    const prevCenterX = prevRect.left + prevRect.width / 2;
    const prevCenterY = prevRect.top + prevRect.height / 2;
    const newCenterX = newRect.left + newRect.width / 2;
    const newCenterY = newRect.top + newRect.height / 2;

    const dx = newCenterX - prevCenterX;
    const dy = newCenterY - prevCenterY;

    // Space-separated dual axis direction matching React
    const horizontal = Math.abs(dx) < DIRECTION_TOLERANCE ? '' : (dx > 0 ? 'right' : 'left');
    const vertical = Math.abs(dy) < DIRECTION_TOLERANCE ? '' : (dy > 0 ? 'down' : 'up');
    const directionStr = `${horizontal} ${vertical}`.trim();

    // Apply transition-hint data attributes
    clone.setAttribute('data-ending-style', '');
    currentContainer.setAttribute('data-starting-style', '');

    // Insert clone before current container
    parent.insertBefore(clone, currentContainer);

    // Notify Blazor of transition start
    rootState.viewportDotNetRef.invokeMethodAsync('OnViewportTransitionStart', directionStr).catch(() => { });

    // Wait two rAF frames then listen for transition/animation end
    requestAnimationFrame(() => {
        requestAnimationFrame(() => {
            // Remove data-starting-style from the current container after 2 rAF frames
            currentContainer.removeAttribute('data-starting-style');

            let ended = false;
            const onEnd = (event) => {
                if (event && event.target !== clone) return;
                if (ended) return;
                ended = true;
                clone.removeEventListener('transitionend', onEnd);
                clone.removeEventListener('animationend', onEnd);
                clearTimeout(fallbackId);
                clone.remove();
                if (rootState.viewportDotNetRef) {
                    rootState.viewportDotNetRef.invokeMethodAsync('OnViewportTransitionEnd').catch(() => { });
                }
            };

            clone.addEventListener('transitionend', onEnd);
            clone.addEventListener('animationend', onEnd);

            // Fallback timeout in case no transition/animation fires
            const fallbackId = setTimeout(onEnd, 500);
        });
    });
}

// ============================================================================
// Auto-Resize Support (for viewport content size changes)
// ============================================================================

function setPopupCssSize(el, size) {
    if (!el) return;
    if (size === 'auto') {
        el.style.setProperty('--popup-width', 'auto');
        el.style.setProperty('--popup-height', 'auto');
    } else {
        el.style.setProperty('--popup-width', `${size.width}px`);
        el.style.setProperty('--popup-height', `${size.height}px`);
    }
}

function setPositionerCssSize(el, size) {
    if (!el) return;
    if (size === 'max-content') {
        el.style.setProperty('--positioner-width', 'max-content');
        el.style.setProperty('--positioner-height', 'max-content');
    } else {
        el.style.setProperty('--positioner-width', `${size.width}px`);
        el.style.setProperty('--positioner-height', `${size.height}px`);
    }
}

function getCssDimensions(el) {
    if (!el) return { width: 0, height: 0 };
    const style = getComputedStyle(el);
    return {
        width: Math.ceil(parseFloat(style.width) || 0),
        height: Math.ceil(parseFloat(style.height) || 0)
    };
}

function applyAnchoringStyles(el, side, direction) {
    if (!el) return;
    const isRtl = direction === 'rtl';
    if (side === 'top') {
        el.style.position = 'absolute';
        el.style.bottom = '0';
        el.style.top = 'auto';
    } else if (side === 'bottom') {
        el.style.position = '';
        el.style.bottom = '';
        el.style.top = '';
    }
    if ((side === 'left' && !isRtl) || (side === 'right' && isRtl)) {
        el.style.position = 'absolute';
        el.style.right = '0';
        el.style.left = 'auto';
    } else if ((side === 'right' && !isRtl) || (side === 'left' && isRtl)) {
        el.style.position = '';
        el.style.right = '';
        el.style.left = '';
    }
}

function setupMenuAutoResize(rootState) {
    cleanupMenuAutoResize(rootState);

    const { popupElement, positionerElement } = rootState;
    if (!popupElement || !positionerElement || typeof ResizeObserver === 'undefined') return;

    const side = rootState.currentSide || 'bottom';
    const direction = rootState.direction || 'ltr';
    applyAnchoringStyles(popupElement, side, direction);

    const observer = new ResizeObserver((entries) => {
        const entry = entries[0];
        if (entry) {
            rootState.liveDimensions = {
                width: Math.ceil(entry.borderBoxSize[0]?.inlineSize || entry.contentRect.width),
                height: Math.ceil(entry.borderBoxSize[0]?.blockSize || entry.contentRect.height)
            };
        }
    });
    observer.observe(popupElement);

    // Initial measurement
    setPopupCssSize(popupElement, 'auto');
    setPositionerCssSize(positionerElement, 'max-content');
    const dims = getCssDimensions(popupElement);
    rootState.autoResizeCommitted = dims;
    setPositionerCssSize(positionerElement, dims);

    rootState.autoResizeObserver = observer;
}

function cleanupMenuAutoResize(rootState) {
    if (rootState.autoResizeObserver) {
        rootState.autoResizeObserver.disconnect();
        rootState.autoResizeObserver = null;
    }
    rootState.autoResizeCommitted = null;
    rootState.liveDimensions = null;
}

// ============================================================================
// Item Index Query
// ============================================================================

export function getItemIndex(rootId, element) {
    const root = state.roots.get(rootId);
    if (!root || !root.popupElement) return -1;
    const items = getMenuItems(root.popupElement);
    return items.indexOf(element);
}
