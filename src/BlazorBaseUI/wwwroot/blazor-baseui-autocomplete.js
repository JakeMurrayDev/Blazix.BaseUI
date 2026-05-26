import {
  contains,
  getTarget,
  initializePositioner as initializeFloatingPositioner,
  updatePositioner,
  disposePositioner as disposeFloatingPositioner,
} from './blazor-baseui-floating.min.js';

const stateKey = Symbol.for('BlazorBaseUI.Autocomplete.State');
const pendingInlineSelectionKey = Symbol.for('BlazorBaseUI.Autocomplete.PendingInlineSelection');

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
    inputElement: null,
    triggerElement: null,
    listElement: null,
    popupElement: null,
    positionerElement: null,
    inputCleanup: null,
    triggerCleanup: null,
    listCleanup: null,
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
      root.dotNetRef.invokeMethodAsync('OnNavigate', event.key === 'ArrowDown' ? 1 : -1).catch(() => {});
      return;
    }

    if (event.key === 'Enter' && root.isOpen) {
      event.preventDefault();
      event.stopPropagation();
      root.dotNetRef.invokeMethodAsync('OnCommitActive').catch(() => {});
      return;
    }

    if (event.key === 'Escape' && root.isOpen) {
      event.preventDefault();
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
    const pending = element[pendingInlineSelectionKey];
    if (!pending || element.value === pending.display || !element.value.startsWith(pending.typed)) {
      return;
    }

    element.value = element.value.slice(pending.typed.length);
    clearPendingInlineSelection(element);
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
      element.setSelectionRange(0, display.length);
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

  const onMouseDown = (event) => {
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

  element.addEventListener('mousedown', onMouseDown);
  element.addEventListener('keydown', onKeyDown, true);
  element.addEventListener('focusout', onFocusOut);
  root.triggerCleanup = () => {
    element.removeEventListener('mousedown', onMouseDown);
    element.removeEventListener('keydown', onKeyDown, true);
    element.removeEventListener('focusout', onFocusOut);
  };
}

function attachListHandlers(root, element) {
  attachKeyboardHandlers(root, element, 'list');
  if (!element) {
    return;
  }

  const onPointerDown = (event) => {
    const target = getTarget(event);
    if (target?.closest?.('[data-autocomplete-item]')) {
      event.preventDefault();
    }
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

export function initializeRoot(rootId, dotNetRef) {
  initializeDocumentListeners();
  const root = ensureRoot(rootId);
  root.dotNetRef = dotNetRef;
  attachKeyboardHandlers(root, root.inputElement, 'input');
  attachTriggerHandlers(root, root.triggerElement);
  attachListHandlers(root, root.listElement);
}

export function disposeRoot(rootId) {
  const root = getRoot(rootId);
  if (!root) {
    return;
  }

  cleanupElement(root, 'input');
  cleanupElement(root, 'trigger');
  cleanupElement(root, 'list');
  state.roots.delete(rootId);
}

export function setRootOpen(rootId, open) {
  const root = ensureRoot(rootId);
  root.isOpen = open;
}

export function setInputElement(rootId, element) {
  const root = ensureRoot(rootId);
  root.inputElement = element;
  attachKeyboardHandlers(root, element, 'input');
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

export function setListElement(rootId, element) {
  const root = ensureRoot(rootId);
  root.listElement = element;
  attachListHandlers(root, element);
}

export function setPopupElement(rootId, element) {
  const root = ensureRoot(rootId);
  root.popupElement = element;
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
