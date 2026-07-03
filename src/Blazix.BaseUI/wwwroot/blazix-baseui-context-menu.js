const STATE_KEY = Symbol.for('Blazix.BaseUI.ContextMenu.State');
if (!window[STATE_KEY]) {
  window[STATE_KEY] = {
    roots: new Map()
  };
}
const state = window[STATE_KEY];

const LONG_PRESS_DELAY = 500;
const TOUCH_MOVE_THRESHOLD = 10;

/**
 * Initializes context menu behavior on a trigger element.
 * @param {string} rootId - Unique identifier for this context menu instance.
 * @param {HTMLElement} triggerElement - The element that responds to right-click/long-press.
 * @param {HTMLElement} virtualAnchorElement - Hidden element used as positioning anchor.
 * @param {object} dotNetRef - .NET object reference for callbacks.
 * @param {boolean} disabled - Whether context-menu interaction is disabled.
 */
export function initializeContextMenu(rootId, triggerElement, virtualAnchorElement, dotNetRef, disabled = false) {
  const root = {
    triggerElement,
    virtualAnchorElement,
    dotNetRef,
    disabled,
    isOpen: false,
    backdropElement: null,
    positionerElement: null,
    touchPosition: null,
    longPressTimeoutId: null,
    allowMouseUpTimeoutId: null,
    allowMouseUp: false,
    initialCursorPoint: null,
    documentContextMenuHandler: null,
    cleanupMouseUp: null
  };

  root.contextMenuHandler = (e) => handleContextMenu(rootId, e);
  root.touchStartHandler = (e) => handleTouchStart(rootId, e);
  root.touchMoveHandler = (e) => handleTouchMove(rootId, e);
  root.touchEndHandler = () => handleTouchEnd(rootId);

  // While the context menu is open, a right-click inside the trigger re-anchors the menu
  // to the new cursor (handled by the contextmenu event below) and must NOT dismiss it.
  // Stop the press from reaching the shared menu's global outside-press handler. Mirrors
  // React, where the modal internal backdrop swallows the press so the open menu
  // repositions instead of closing. Left-clicks, and right-clicks outside the trigger,
  // still fall through to the outside-press and dismiss.
  root.pointerDownHandler = (e) => {
    if (root.isOpen && e.button === 2) {
      e.stopPropagation();

      // A second right-click on the open menu is a reposition, not a click-drag-release
      // cancel. Tear down the pending cancel listener armed by the previous open and disarm
      // it (this runs before the upcoming mouseup) so this press's release cannot fire
      // OnCancelOpen — which on Server races the reopen over SignalR and wins, closing the
      // menu. handleContextMenu also skips re-arming the cancel while already open.
      if (root.cleanupMouseUp) {
        root.cleanupMouseUp();
        root.cleanupMouseUp = null;
      }
      if (root.allowMouseUpTimeoutId !== null) {
        clearTimeout(root.allowMouseUpTimeoutId);
        root.allowMouseUpTimeoutId = null;
      }
      root.allowMouseUp = false;
    }
  };

  triggerElement.addEventListener('contextmenu', root.contextMenuHandler);
  triggerElement.addEventListener('pointerdown', root.pointerDownHandler, true);
  triggerElement.addEventListener('touchstart', root.touchStartHandler, { passive: true });
  triggerElement.addEventListener('touchmove', root.touchMoveHandler, { passive: true });
  triggerElement.addEventListener('touchend', root.touchEndHandler);
  triggerElement.addEventListener('touchcancel', root.touchEndHandler);

  root.documentContextMenuHandler = (e) => {
    if (root.disabled) {
      return;
    }

    const target = e.target;
    if (triggerElement.contains(target)) {
      e.preventDefault();
      return;
    }
    if (root.backdropElement && root.backdropElement.contains(target)) {
      e.preventDefault();
      return;
    }

    // Mirrors React's modal internal backdrop (internalBackdropRef in ContextMenuRoot):
    // while the context menu is open it renders a full-viewport backdrop with no cutout,
    // so the native context menu is suppressed everywhere except over the popup itself.
    if (root.isOpen && root.positionerElement && !root.positionerElement.contains(target)) {
      e.preventDefault();
    }
  };
  document.addEventListener('contextmenu', root.documentContextMenuHandler);

  state.roots.set(rootId, root);
}

/**
 * Disposes context menu behavior and cleans up all listeners.
 * @param {string} rootId - Unique identifier for the context menu instance.
 */
export function disposeContextMenu(rootId) {
  const root = state.roots.get(rootId);
  if (!root) return;

  const { triggerElement } = root;

  triggerElement.removeEventListener('contextmenu', root.contextMenuHandler);
  triggerElement.removeEventListener('pointerdown', root.pointerDownHandler, true);
  triggerElement.removeEventListener('touchstart', root.touchStartHandler);
  triggerElement.removeEventListener('touchmove', root.touchMoveHandler);
  triggerElement.removeEventListener('touchend', root.touchEndHandler);
  triggerElement.removeEventListener('touchcancel', root.touchEndHandler);

  if (root.documentContextMenuHandler) {
    document.removeEventListener('contextmenu', root.documentContextMenuHandler);
  }

  if (root.longPressTimeoutId !== null) {
    clearTimeout(root.longPressTimeoutId);
  }
  if (root.allowMouseUpTimeoutId !== null) {
    clearTimeout(root.allowMouseUpTimeoutId);
  }
  if (root.cleanupMouseUp) {
    root.cleanupMouseUp();
  }

  state.roots.delete(rootId);
}

/**
 * Stores a reference to the backdrop element for native context menu suppression.
 * @param {string} rootId - Unique identifier for the context menu instance.
 * @param {HTMLElement} element - The backdrop DOM element.
 */
export function setBackdropElement(rootId, element) {
  const root = state.roots.get(rootId);
  if (root) {
    root.backdropElement = element;
  }
}

/**
 * Stores a reference to the positioner element for mouseup handling.
 * @param {string} rootId - Unique identifier for the context menu instance.
 * @param {HTMLElement} element - The menu positioner DOM element.
 */
export function setPositionerElement(rootId, element) {
  const root = state.roots.get(rootId);
  if (root) {
    root.positionerElement = element;
  }
}

/**
 * Updates whether context-menu interaction is disabled.
 * @param {string} rootId - Unique identifier for the context menu instance.
 * @param {boolean} disabled - Whether interaction should be ignored.
 */
export function setContextMenuDisabled(rootId, disabled) {
  const root = state.roots.get(rootId);
  if (root) {
    root.disabled = disabled;
    if (disabled) {
      cancelPendingGesture(root);
    }
  }
}

/**
 * Updates the open state of the context menu so the document `contextmenu` handler can
 * suppress the native menu over the modal region (React's internal backdrop) while open.
 * @param {string} rootId - Unique identifier for the context menu instance.
 * @param {boolean} isOpen - Whether the context menu is currently open.
 */
export function setContextMenuOpen(rootId, isOpen) {
  const root = state.roots.get(rootId);
  if (root) {
    root.isOpen = isOpen;
  }
}

function cancelPendingGesture(root) {
  if (root.longPressTimeoutId !== null) {
    clearTimeout(root.longPressTimeoutId);
    root.longPressTimeoutId = null;
  }

  if (root.allowMouseUpTimeoutId !== null) {
    clearTimeout(root.allowMouseUpTimeoutId);
    root.allowMouseUpTimeoutId = null;
  }

  root.allowMouseUp = false;
  root.touchPosition = null;
  root.initialCursorPoint = null;

  if (root.cleanupMouseUp) {
    root.cleanupMouseUp();
    root.cleanupMouseUp = null;
  }
}

function positionVirtualAnchor(root, x, y, isTouchEvent) {
  const el = root.virtualAnchorElement;
  const size = isTouchEvent ? 10 : 0;
  el.style.top = y + 'px';
  el.style.left = x + 'px';
  el.style.width = size + 'px';
  el.style.height = size + 'px';
}

function handleContextMenu(rootId, event) {
  const root = state.roots.get(rootId);
  if (!root) return;

  if (root.disabled) {
    return;
  }

  event.preventDefault();
  event.stopPropagation();

  // The menu is now opening (or, if already open, staying open and repositioning).
  // Mark it open synchronously so the trigger's pointerdown handler suppresses the shared
  // outside-press on an immediate second right-click, without waiting for the async C#
  // open-state push (which a fast double right-click can outrun). C# later confirms/clears it.
  const wasOpen = root.isOpen;
  root.isOpen = true;

  const x = event.clientX;
  const y = event.clientY;

  root.initialCursorPoint = { x, y };
  positionVirtualAnchor(root, x, y, false);

  // On reopen, the positioner persists in the DOM (hidden) with the previous open's
  // position. Strip `data-positioned` so the flash-of-unpositioned-content CSS hides it
  // until it is repositioned for the new cursor, avoiding a one-frame jump from the old
  // location. Skip while already open so an in-place reposition stays smooth.
  if (!wasOpen && root.positionerElement) {
    root.positionerElement.removeAttribute('data-positioned');
  }

  root.allowMouseUp = false;
  root.dotNetRef.invokeMethodAsync('OnContextMenu', x, y, false);

  // The click-drag-release cancel gesture (arm after LONG_PRESS_DELAY, then dismiss on an
  // outside mouseup) only applies to the INITIAL open. A second right-click while open is a
  // reposition; arming it would let a held second press (>500ms) dismiss the menu on release
  // — which closes it on Server (the cancel round-trip beats the reopen).
  if (!wasOpen) {
    root.allowMouseUpTimeoutId = setTimeout(() => {
      root.allowMouseUp = true;
      root.allowMouseUpTimeoutId = null;
    }, LONG_PRESS_DELAY);

    setupMouseUpListener(rootId);
  }
}

function setupMouseUpListener(rootId) {
  const root = state.roots.get(rootId);
  if (!root) return;

  if (root.cleanupMouseUp) {
    root.cleanupMouseUp();
  }

  const handler = (mouseEvent) => {
    root.cleanupMouseUp = null;

    if (!root.allowMouseUp) {
      return;
    }

    if (root.allowMouseUpTimeoutId !== null) {
      clearTimeout(root.allowMouseUpTimeoutId);
      root.allowMouseUpTimeoutId = null;
    }
    root.allowMouseUp = false;

    const mouseUpTarget = mouseEvent.target instanceof Element ? mouseEvent.target : null;

    if (root.positionerElement && mouseUpTarget && root.positionerElement.contains(mouseUpTarget)) {
      handlePositionerMouseUp(root, mouseEvent, mouseUpTarget);
      return;
    }

    if (mouseUpTarget && findRootOwnerId(mouseUpTarget) === rootId) {
      return;
    }

    root.initialCursorPoint = null;
    root.dotNetRef.invokeMethodAsync('OnCancelOpen');
  };

  document.addEventListener('mouseup', handler, { once: true });
  root.cleanupMouseUp = () => document.removeEventListener('mouseup', handler);
}

function handlePositionerMouseUp(root, mouseEvent, mouseUpTarget) {
  // Check if mouseup is on a menu item — activate it (click-drag-release gesture)
  const menuItem = mouseUpTarget.closest(
    '[role="menuitem"], [role="menuitemcheckbox"], [role="menuitemradio"]'
  );

  if (!menuItem) {
    return;
  }

  const initialPoint = root.initialCursorPoint;
  root.initialCursorPoint = null;

  // Don't activate if cursor barely moved from initial right-click position (1px threshold)
  if (initialPoint) {
    const dx = mouseEvent.clientX - initialPoint.x;
    const dy = mouseEvent.clientY - initialPoint.y;
    if (Math.abs(dx) <= 1 && Math.abs(dy) <= 1) return;
  }

  // On non-macOS, don't activate on right-button mouseup
  const isMac = /mac/i.test(navigator.userAgent);
  if (!isMac && mouseEvent.button === 2) return;

  // React useMenuItemCommonProps.ts:109 — a context-menu item only activates when the
  // releasing button is the right button (the same button that opened the menu). Combined
  // with the non-macOS guard above this makes click-drag-release activation macOS-only,
  // exactly matching React (on non-macOS the button-2 release is consumed above, and any
  // other button fails this gate).
  if (mouseEvent.button !== 2) return;

  // Don't activate submenu triggers or disabled items
  if (menuItem.hasAttribute('aria-haspopup')) return;
  if (menuItem.getAttribute('aria-disabled') === 'true') return;

  menuItem.click();
}

function findRootOwnerId(node) {
  let current = node;

  while (current) {
    if (current instanceof Element && current.hasAttribute('data-rootownerid')) {
      return current.getAttribute('data-rootownerid') || undefined;
    }

    if (current.parentNode) {
      current = current.parentNode;
      continue;
    }

    const rootNode = current.getRootNode?.();
    current = rootNode && 'host' in rootNode ? rootNode.host : null;
  }

  return undefined;
}

function handleTouchStart(rootId, event) {
  const root = state.roots.get(rootId);
  if (!root) return;

  if (root.disabled) {
    return;
  }

  if (event.touches.length !== 1) return;

  event.stopPropagation();

  const touch = event.touches[0];
  root.touchPosition = { x: touch.clientX, y: touch.clientY };

  root.longPressTimeoutId = setTimeout(() => {
    root.longPressTimeoutId = null;
    if (!root.touchPosition || root.disabled) {
      return;
    }

    const { x, y } = root.touchPosition;
    root.initialCursorPoint = { x, y };
    positionVirtualAnchor(root, x, y, true);
    root.dotNetRef.invokeMethodAsync('OnContextMenu', x, y, true);
  }, LONG_PRESS_DELAY);
}

function handleTouchMove(rootId, event) {
  const root = state.roots.get(rootId);
  if (!root || root.disabled || root.longPressTimeoutId === null || !root.touchPosition) return;

  if (event.touches.length !== 1) return;

  const touch = event.touches[0];
  const deltaX = Math.abs(touch.clientX - root.touchPosition.x);
  const deltaY = Math.abs(touch.clientY - root.touchPosition.y);

  if (deltaX > TOUCH_MOVE_THRESHOLD || deltaY > TOUCH_MOVE_THRESHOLD) {
    clearTimeout(root.longPressTimeoutId);
    root.longPressTimeoutId = null;
  }
}

function handleTouchEnd(rootId) {
  const root = state.roots.get(rootId);
  if (!root) return;

  if (root.longPressTimeoutId !== null) {
    clearTimeout(root.longPressTimeoutId);
    root.longPressTimeoutId = null;
  }
  root.touchPosition = null;
}
