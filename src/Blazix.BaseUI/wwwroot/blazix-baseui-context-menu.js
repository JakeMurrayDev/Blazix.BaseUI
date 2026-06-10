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

  triggerElement.addEventListener('contextmenu', root.contextMenuHandler);
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

  const x = event.clientX;
  const y = event.clientY;

  root.initialCursorPoint = { x, y };
  positionVirtualAnchor(root, x, y, false);

  root.allowMouseUp = false;
  root.dotNetRef.invokeMethodAsync('OnContextMenu', x, y, false);

  root.allowMouseUpTimeoutId = setTimeout(() => {
    root.allowMouseUp = true;
    root.allowMouseUpTimeoutId = null;
  }, LONG_PRESS_DELAY);

  setupMouseUpListener(rootId);
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
