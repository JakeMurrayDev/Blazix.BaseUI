const STATE_KEY = Symbol.for('Blazix.BaseUI.Field.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        observers: new Map()
    };
}

const state = window[STATE_KEY];

export function getValidityState(element) {
    if (!element || !element.validity) {
        return null;
    }

    const validity = element.validity;

    return {
        badInput: validity.badInput,
        customError: validity.customError,
        patternMismatch: validity.patternMismatch,
        rangeOverflow: validity.rangeOverflow,
        rangeUnderflow: validity.rangeUnderflow,
        stepMismatch: validity.stepMismatch,
        tooLong: validity.tooLong,
        tooShort: validity.tooShort,
        typeMismatch: validity.typeMismatch,
        valueMissing: validity.valueMissing,
        valid: validity.valid
    };
}

export function getValidationMessage(element) {
    if (!element) {
        return '';
    }

    return element.validationMessage || '';
}

export function setCustomValidity(element, message) {
    if (!element || typeof element.setCustomValidity !== 'function') {
        return;
    }

    element.setCustomValidity(message || '');
}

export function checkValidity(element) {
    if (!element || typeof element.checkValidity !== 'function') {
        return true;
    }

    return element.checkValidity();
}

export function reportValidity(element) {
    if (!element || typeof element.reportValidity !== 'function') {
        return true;
    }

    return element.reportValidity();
}

export function focusElement(element) {
    if (!element || typeof element.focus !== 'function') {
        return;
    }

    element.focus();

    if (element.tagName === 'INPUT' && typeof element.select === 'function') {
        element.select();
    }
}

export function isActiveElement(element) {
    return Boolean(element && element.ownerDocument && element.ownerDocument.activeElement === element);
}

export function getValue(element) {
    if (!element) {
        return null;
    }

    if (element.type === 'checkbox') {
        return element.checked;
    }

    return element.value;
}

export function setValue(element, value) {
    if (!element) {
        return;
    }

    if (element.type === 'checkbox') {
        element.checked = Boolean(value);
    } else {
        element.value = value ?? '';
    }
}

export function observeValidity(element, dotNetRef, methodName) {
    if (!element) {
        return null;
    }

    const id = crypto.randomUUID();

    const handler = () => {
        const validityState = getValidityState(element);
        const validationMessage = getValidationMessage(element);
        dotNetRef.invokeMethodAsync(methodName, validityState, validationMessage);
    };

    element.addEventListener('input', handler);
    element.addEventListener('change', handler);
    element.addEventListener('invalid', handler);

    state.observers.set(id, {
        element,
        handler,
        dotNetRef
    });

    return id;
}

export function disposeObserver(observerId) {
    const observer = state.observers.get(observerId);
    if (!observer) {
        return;
    }

    const { element, handler } = observer;

    element.removeEventListener('input', handler);
    element.removeEventListener('change', handler);
    element.removeEventListener('invalid', handler);

    state.observers.delete(observerId);
}

export function dispose(element) {
    for (const [id, observer] of state.observers) {
        if (observer.element === element) {
            disposeObserver(id);
        }
    }
}
