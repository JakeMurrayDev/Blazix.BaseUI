import {
  contains,
  getTarget,
  initializePositioner as initializeFloatingPositioner,
  updatePositioner,
  disposePositioner as disposeFloatingPositioner,
  waitForPopupAndStartTransition,
  startSimpleTransition,
  cleanupTransitionState,
} from './blazix-baseui-floating.min.js';

const stateKey = Symbol.for('Blazix.BaseUI.Combobox.State');
const pendingInlineSelectionKey = Symbol.for('Blazix.BaseUI.Combobox.PendingInlineSelection');

if (!window[stateKey]) {
  window[stateKey] = {
    roots: new Map(),
    positioners: new Map(),
    documentListenersInitialized: false,
  };
}

const state = window[stateKey];

function getRoot(rootId) {
  return state.roots.get(rootId);
}

function createRootState(rootId, dotNetRef = null) {
  return {
    rootId,
    dotNetRef,
    isOpen: false,
    activeIndex: -1,
    itemCount: 0,
    loopFocus: true,
    pendingActiveIndex: null,
    pendingNavigation: null,
    pendingOpen: false,
    transitionCleanup: null,
    fallbackTimeoutId: null,
    navigationVersion: 0,
    inputElement: null,
    triggerElement: null,
    clearElement: null,
    listElement: null,
    popupElement: null,
    positionerElement: null,
    inputInsidePopup: false,
    inputCleanup: null,
    triggerCleanup: null,
    clearCleanup: null,
    listCleanup: null,
    popupCleanup: null,
  };
}

function ensureRoot(rootId) {
  let root = getRoot(rootId);
  if (!root) {
    root = createRootState(rootId);
    state.roots.set(rootId, root);
  }
  return root;
}

function isInsideRoot(root, target) {
  return (
    contains(root.inputElement, target) ||
    contains(root.triggerElement, target) ||
    contains(root.clearElement, target) ||
    contains(root.positionerElement, target) ||
    contains(root.popupElement, target) ||
    contains(root.listElement, target)
  );
}

function requestFocusOutClose(root) {
  if (!root?.isOpen || !root.dotNetRef) {
    return;
  }

  root.dotNetRef.invokeMethodAsync('OnFocusOut').catch(() => {});
}

function scheduleFocusOutClose(root) {
  window.setTimeout(() => {
    const target = document.activeElement;
    if (root.isOpen && !isInsideRoot(root, target)) {
      requestFocusOutClose(root);
    }
  });
}

function canInvokeRoot(root) {
  return root?.dotNetRef && typeof root.dotNetRef.invokeMethodAsync === 'function';
}

function getEffectiveActiveIndex(root) {
  return root.pendingActiveIndex ?? root.activeIndex;
}

function getNavigationTarget(root, delta) {
  const count = Math.max(0, Number(root.itemCount) || 0);
  if (count === 0) {
    return -1;
  }

  let nextIndex = getEffectiveActiveIndex(root) + delta;
  if (nextIndex < 0) {
    nextIndex = root.loopFocus ? count - 1 : 0;
  } else if (nextIndex >= count) {
    nextIndex = root.loopFocus ? 0 : count - 1;
  }

  return nextIndex;
}

function commitActiveItem(root) {
  root.dotNetRef.invokeMethodAsync('OnCommitActive').catch(() => {});
}

function initializeDocumentListeners() {
  if (state.documentListenersInitialized) {
    return;
  }

  document.addEventListener('mousedown', (event) => {
    const target = getTarget(event);
    for (const root of state.roots.values()) {
      if (canInvokeRoot(root) && root.isOpen && !isInsideRoot(root, target)) {
        root.dotNetRef.invokeMethodAsync('OnOutsidePress').catch(() => {});
      }
    }
  });

  document.addEventListener('touchstart', (event) => {
    const target = getTarget(event);
    for (const root of state.roots.values()) {
      if (canInvokeRoot(root) && root.isOpen && !isInsideRoot(root, target)) {
        root.dotNetRef.invokeMethodAsync('OnOutsidePress').catch(() => {});
      }
    }
  }, { passive: true });

  document.addEventListener('keydown', (event) => {
    if (event.key !== 'Escape') {
      return;
    }

    for (const root of state.roots.values()) {
      if (canInvokeRoot(root) && root.isOpen) {
        root.dotNetRef.invokeMethodAsync('OnEscapeKey').catch(() => {});
      }
    }
  });

  state.documentListenersInitialized = true;
}

function cleanupElement(root, key) {
  const cleanupKey = `${key}Cleanup`;
  if (root[cleanupKey]) {
    root[cleanupKey]();
    root[cleanupKey] = null;
  }
}

function focusInputIfNeeded(root) {
  if (!root?.isOpen || !root.inputInsidePopup || !root.inputElement) {
    return;
  }

  let attempts = 0;
  const focusInput = () => {
    requestAnimationFrame(() => {
      if (
        !root.isOpen ||
        !root.inputInsidePopup ||
        !root.inputElement ||
        document.activeElement === root.inputElement
      ) {
        return;
      }

      root.inputElement.focus({ preventScroll: true });
      attempts += 1;

      if (document.activeElement !== root.inputElement && attempts < 5) {
        window.setTimeout(focusInput, 16);
      }
    });
  };

  window.setTimeout(focusInput);
}

function preventInputBlur(root, event) {
  if (!root.inputElement) {
    return;
  }

  const target = getTarget(event);
  if (contains(root.inputElement, target)) {
    return;
  }

  event.preventDefault();
}

function attachKeyboardHandlers(root, element, key) {
  cleanupElement(root, key);
  if (!element || !root.dotNetRef) {
    return;
  }

  const onKeyDown = (event) => {
    if (event.key === 'Tab' && root.isOpen) {
      requestFocusOutClose(root);
      return;
    }

    if (event.ctrlKey || event.altKey || event.metaKey || event.shiftKey) {
      return;
    }

    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      event.stopPropagation();
      const delta = event.key === 'ArrowDown' ? 1 : -1;
      const navigationVersion = root.navigationVersion + 1;
      root.navigationVersion = navigationVersion;
      root.pendingActiveIndex = getNavigationTarget(root, delta);
      root.pendingNavigation = root.dotNetRef
        .invokeMethodAsync('OnNavigate', event.key === 'ArrowDown' ? 1 : -1).then((activeIndex) => {
          if (root.navigationVersion !== navigationVersion) {
            return;
          }
          root.activeIndex = typeof activeIndex === 'number' ? activeIndex : root.pendingActiveIndex;
          root.pendingActiveIndex = null;
          root.pendingNavigation = null;
        }).catch(() => {
          if (root.navigationVersion !== navigationVersion) {
            return;
          }
          root.pendingActiveIndex = null;
          root.pendingNavigation = null;
        });
      return;
    }

    if (event.key === 'Enter' && root.isOpen && getEffectiveActiveIndex(root) >= 0) {
      event.preventDefault();
      event.stopPropagation();
      if (root.pendingNavigation) {
        root.pendingNavigation.then(() => commitActiveItem(root), () => commitActiveItem(root));
      } else {
        commitActiveItem(root);
      }
      return;
    }

    if (event.key === 'Escape' && root.isOpen) {
      event.preventDefault();
      event.stopPropagation();
      root.dotNetRef.invokeMethodAsync('OnEscapeKey').catch(() => {});
    }
  };

  const onFocusOut = () => {
    scheduleFocusOutClose(root);
  };

  const onPointerActivity = () => {
    root.dotNetRef.invokeMethodAsync('OnKeyboardActiveChange', false).catch(() => {});
  };

  const onInput = () => {
    consumePendingInlineSelection(element);
  };

  element.addEventListener('keydown', onKeyDown, true);
  element.addEventListener('focusout', onFocusOut);
  element.addEventListener('input', onInput, true);
  element.addEventListener('pointermove', onPointerActivity, { passive: true });
  element.addEventListener('pointerdown', onPointerActivity, { passive: true });

  root[`${key}Cleanup`] = () => {
    element.removeEventListener('keydown', onKeyDown, true);
    element.removeEventListener('focusout', onFocusOut);
    element.removeEventListener('input', onInput, true);
    element.removeEventListener('pointermove', onPointerActivity);
    element.removeEventListener('pointerdown', onPointerActivity);
  };
}

function clearPendingInlineSelection(element) {
  const pending = element?.[pendingInlineSelectionKey];
  if (!pending) {
    return;
  }

  if (pending.handler) {
    element.removeEventListener('focus', pending.handler);
  }
  element[pendingInlineSelectionKey] = null;
}

function consumePendingInlineSelection(element) {
  const pending = element[pendingInlineSelectionKey];
  if (!pending || element.value === pending.display) {
    return;
  }

  if (element.value.startsWith(pending.display)) {
    element.value = pending.typed + element.value.slice(pending.display.length);
    clearPendingInlineSelection(element);
    return;
  }

  if (element.value.startsWith(pending.typed)) {
    clearPendingInlineSelection(element);
  }
}

function setPendingInlineSelection(element, typed, display) {
  clearPendingInlineSelection(element);

  const applyPendingSelection = () => {
    const pending = element[pendingInlineSelectionKey];
    if (pending?.handler) {
      element.removeEventListener('focus', pending.handler);
      pending.handler = null;
    }

    if (element.value !== display) {
      return;
    }

    try {
      element.setSelectionRange(typed.length, display.length);
    } catch {
      // Input types without text selection support can ignore inline completion selection.
    }
  };

  element[pendingInlineSelectionKey] = {
    handler: applyPendingSelection,
    typed,
    display,
  };
  element.addEventListener('focus', applyPendingSelection);
}

function attachTriggerHandlers(root, element) {
  cleanupElement(root, 'trigger');
  if (!element) {
    return;
  }

  const onPointerDown = (event) => {
    if (event.button !== 0) {
      return;
    }

    if (root.inputElement && event.pointerType !== 'touch') {
      root.inputElement.focus({ preventScroll: true });
    }
  };

  const onKeyDown = (event) => {
    if (event.key === 'Tab' && root.isOpen) {
      requestFocusOutClose(root);
    }
  };

  const onFocusOut = () => {
    scheduleFocusOutClose(root);
  };

  element.addEventListener('pointerdown', onPointerDown);
  element.addEventListener('keydown', onKeyDown, true);
  element.addEventListener('focusout', onFocusOut);
  root.triggerCleanup = () => {
    element.removeEventListener('pointerdown', onPointerDown);
    element.removeEventListener('keydown', onKeyDown, true);
    element.removeEventListener('focusout', onFocusOut);
  };
}

function attachClearHandlers(root, element) {
  cleanupElement(root, 'clear');
  if (!element) {
    return;
  }

  const onPointerDown = (event) => {
    preventInputBlur(root, event);
    if (root.inputElement && event.pointerType !== 'touch') {
      root.inputElement.focus({ preventScroll: true });
    }
  };

  element.addEventListener('pointerdown', onPointerDown);
  element.addEventListener('mousedown', onPointerDown);

  root.clearCleanup = () => {
    element.removeEventListener('pointerdown', onPointerDown);
    element.removeEventListener('mousedown', onPointerDown);
  };
}

function attachListHandlers(root, element) {
  attachKeyboardHandlers(root, element, 'list');
  if (!element) {
    return;
  }

  const onPointerDown = (event) => {
    preventInputBlur(root, event);
  };

  element.addEventListener('pointerdown', onPointerDown);
  element.addEventListener('mousedown', onPointerDown);

  const previousCleanup = root.listCleanup;
  root.listCleanup = () => {
    previousCleanup?.();
    element.removeEventListener('pointerdown', onPointerDown);
    element.removeEventListener('mousedown', onPointerDown);
  };
}

function attachPopupHandlers(root, element) {
  cleanupElement(root, 'popup');
  if (!element) {
    return;
  }

  const onPointerDown = (event) => {
    preventInputBlur(root, event);
  };

  element.addEventListener('pointerdown', onPointerDown);
  element.addEventListener('mousedown', onPointerDown);

  root.popupCleanup = () => {
    element.removeEventListener('pointerdown', onPointerDown);
    element.removeEventListener('mousedown', onPointerDown);
  };
}

export function initializeRoot(rootId, dotNetRef) {
  initializeDocumentListeners();
  const root = ensureRoot(rootId);
  root.dotNetRef = dotNetRef;
  attachKeyboardHandlers(root, root.inputElement, 'input');
  attachTriggerHandlers(root, root.triggerElement);
  attachListHandlers(root, root.listElement);
  attachPopupHandlers(root, root.popupElement);
}

export function disposeRoot(rootId) {
  const root = getRoot(rootId);
  if (!root) {
    return;
  }

  cleanupTransitionState(root);
  cleanupElement(root, 'input');
  cleanupElement(root, 'trigger');
  cleanupElement(root, 'clear');
  cleanupElement(root, 'list');
  cleanupElement(root, 'popup');
  state.roots.delete(rootId);
}

export function setRootOpen(rootId, open, activeIndex = -1, itemCount = 0, loopFocus = true) {
  const root = ensureRoot(rootId);
  const wasOpen = root.isOpen;
  root.pendingOpen = open;
  root.isOpen = open;
  root.activeIndex = activeIndex;
  root.itemCount = Math.max(0, Number(itemCount) || 0);
  root.loopFocus = !!loopFocus;
  if (!open) {
    cleanupTransitionState(root);
    root.navigationVersion += 1;
    root.pendingActiveIndex = null;
    root.pendingNavigation = null;
  } else if (root.pendingActiveIndex !== null && root.pendingActiveIndex >= root.itemCount) {
    root.pendingActiveIndex = root.itemCount > 0 ? root.itemCount - 1 : -1;
  }
  if (open && !wasOpen) {
    waitForPopupAndStartTransition(root, true, startSimpleTransition);
  }
  focusInputIfNeeded(root);
}

export function setInputElement(rootId, element, inputInsidePopup = false) {
  const root = ensureRoot(rootId);
  root.inputElement = element;
  root.inputInsidePopup = inputInsidePopup;
  attachKeyboardHandlers(root, element, 'input');
  focusInputIfNeeded(root);
}

export function syncInputSelection(element, typedValue, inputValue) {
  if (!element || typeof element.setSelectionRange !== 'function') {
    return;
  }

  const typed = typedValue || '';
  const display = inputValue || '';
  if (element.value !== display || display.length <= typed.length) {
    return;
  }

  if (!display.toLocaleLowerCase().startsWith(typed.toLocaleLowerCase())) {
    return;
  }

  const start = typed.length;
  const end = display.length;
  const applySelection = () => {
    if (element.value !== display) {
      return;
    }

    try {
      const selectionStart = document.activeElement === element ? start : 0;
      element.setSelectionRange(selectionStart, end);
    } catch {
      // Input types without text selection support can ignore inline completion selection.
    }
  };

  if (document.activeElement === element) {
    clearPendingInlineSelection(element);
    requestAnimationFrame(applySelection);
  } else {
    setPendingInlineSelection(element, typed, display);
    applySelection();
  }
}

export function setTriggerElement(rootId, element) {
  const root = ensureRoot(rootId);
  root.triggerElement = element;
  attachTriggerHandlers(root, element);
}

export function setClearElement(rootId, element) {
  const root = ensureRoot(rootId);
  root.clearElement = element;
  attachClearHandlers(root, element);
}

export function setListElement(rootId, element) {
  const root = ensureRoot(rootId);
  root.listElement = element;
  attachListHandlers(root, element);
}

export function setPopupElement(rootId, element) {
  const root = ensureRoot(rootId);
  root.popupElement = element;
  attachPopupHandlers(root, element);
  if (root.isOpen && root.pendingOpen) {
    waitForPopupAndStartTransition(root, true, startSimpleTransition);
  }
}

export function setPositionerElement(rootId, element) {
  const root = ensureRoot(rootId);
  root.positionerElement = element;
}

function collisionAvoidance(side, align, fallbackAxisSide) {
  return {
    side: side || 'flip',
    align: align || 'flip',
    fallbackAxisSide: fallbackAxisSide || 'end',
  };
}

export async function initializePositioner(
  positionerElement,
  anchorElement,
  side,
  align,
  sideOffset,
  alignOffset,
  collisionPadding,
  collisionBoundary,
  arrowPadding,
  arrowElement,
  sticky,
  positionMethod,
  disableAnchorTracking,
  collisionSide,
  collisionAlign,
  collisionFallbackAxisSide,
  dotNetRef = null,
) {
  const id = await initializeFloatingPositioner({
    positionerElement,
    triggerElement: anchorElement,
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
    collisionAvoidance: collisionAvoidance(collisionSide, collisionAlign, collisionFallbackAxisSide),
    onPositionUpdated: dotNetRef
      ? (side, align, anchorHidden, arrowUncentered) => {
          dotNetRef.invokeMethodAsync('OnPositionUpdated', side, align, anchorHidden, arrowUncentered).catch(() => {});
        }
      : null,
  });

  if (id) {
    state.positioners.set(id, { positionerId: id });
  }

  return id;
}

export async function updatePosition(
  positionerId,
  anchorElement,
  side,
  align,
  sideOffset,
  alignOffset,
  collisionPadding,
  collisionBoundary,
  arrowPadding,
  arrowElement,
  sticky,
  positionMethod,
  collisionSide,
  collisionAlign,
  collisionFallbackAxisSide,
) {
  await updatePositioner(positionerId, {
    triggerElement: anchorElement,
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
    collisionAvoidance: collisionAvoidance(collisionSide, collisionAlign, collisionFallbackAxisSide),
  });
}

export function disposePositioner(positionerId) {
  disposeFloatingPositioner(positionerId);
  state.positioners.delete(positionerId);
}

export function focusElement(element) {
  element?.focus?.({ preventScroll: true });
}

export function requestSubmit(element) {
  const form = element?.form;
  if (form && typeof form.requestSubmit === 'function') {
    form.requestSubmit();
  }
}
