const STATE_KEY = Symbol.for("Blazix.BaseUI.Label.State");

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        labelListeners: new WeakMap(),
    };
}

const state = window[STATE_KEY];

export function addLabelMouseDownListener(element, preventPointerDownDefault = false) {
    if (!element || state.labelListeners.has(element)) return;

    const isInteractiveTarget = (event) => {
        const target = event.target;
        return target.closest("button, input, select, textarea");
    };

    const onPointerDown = (event) => {
        if (!preventPointerDownDefault || isInteractiveTarget(event)) {
            return;
        }
        if (!event.defaultPrevented) {
            event.preventDefault();
        }
    };

    const onMouseDown = (event) => {
        if (isInteractiveTarget(event)) {
            return;
        }
        if (!event.defaultPrevented && event.detail > 1) {
            event.preventDefault();
        }
    };

    element.addEventListener("pointerdown", onPointerDown);
    element.addEventListener("mousedown", onMouseDown);
    state.labelListeners.set(element, { onPointerDown, onMouseDown });
}

export function removeLabelMouseDownListener(element) {
    if (!element) return;

    const listeners = state.labelListeners.get(element);
    if (listeners) {
        element.removeEventListener("pointerdown", listeners.onPointerDown);
        element.removeEventListener("mousedown", listeners.onMouseDown);
        state.labelListeners.delete(element);
    }
}

export function focusControlById(controlId) {
    if (!controlId) return;

    const controlElement = document.getElementById(controlId);
    if (controlElement) {
        controlElement.focus({ focusVisible: true });
    }
}

export function focusSliderControl(controlElement) {
    if (!controlElement) return;

    const inputs = controlElement.querySelectorAll('input[type="range"]');
    if (inputs.length === 1) {
        inputs[0].focus({ focusVisible: true });
    }
}
