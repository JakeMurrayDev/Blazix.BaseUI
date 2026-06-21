const rootState = new WeakMap();
const warnedFirstSlotAriaLabelInputs = new WeakSet();

function getInputs(root) {
    if (!root) {
        return [];
    }

    return Array.from(root.querySelectorAll('[data-blazix-otp-input]'));
}

function getInputIndex(root, input) {
    return getInputs(root).indexOf(input);
}

function syncInputs(root, value) {
    const inputs = getInputs(root);
    const characters = Array.from(value ?? '');
    for (let index = 0; index < inputs.length; index += 1) {
        inputs[index].value = characters[index] ?? '';
    }
}

function focusInputElement(root, index) {
    const inputs = getInputs(root);
    if (inputs.length === 0) {
        return;
    }

    const targetIndex = Math.min(Math.max(index, 0), inputs.length - 1);
    const target = inputs[targetIndex];
    if (typeof target.focus === 'function') {
        target.focus();
    }
    if (typeof target.select === 'function') {
        target.select();
    }
}

function handleResult(root, result) {
    if (!result) {
        return;
    }

    syncInputs(root, result.value ?? '');

    if (Number.isInteger(result.focusIndex)) {
        queueMicrotask(() => {
            focusInputElement(root, result.focusIndex);
        });
    }
}

function isOtpInput(target) {
    return target instanceof HTMLInputElement && target.hasAttribute('data-blazix-otp-input');
}

function stopEvent(event) {
    event.preventDefault();
    event.stopPropagation();
}

function warnIgnoredFirstSlotAriaLabel(root) {
    const firstInput = getInputs(root)[0];
    if (!firstInput ||
        !firstInput.hasAttribute('data-blazix-otp-first-aria-label-warning') ||
        warnedFirstSlotAriaLabelInputs.has(firstInput) ||
        firstInput.labels?.length) {
        return;
    }

    warnedFirstSlotAriaLabelInputs.add(firstInput);
    console.warn('Base UI: <OtpFieldInput> ignores `aria-label` on the first input. Use a `<label>` or `<FieldLabel>` to label the OTP field.');
}

function isNavigationKey(key, direction) {
    const previousKey = direction === 'rtl' ? 'ArrowRight' : 'ArrowLeft';
    const nextKey = direction === 'rtl' ? 'ArrowLeft' : 'ArrowRight';
    return key === previousKey || key === nextKey || key === 'Home' || key === 'End' || key === 'ArrowUp' || key === 'ArrowDown';
}

function isMutationKey(event, config) {
    const hasBoundaryModifier = (event.ctrlKey || event.metaKey) && !event.altKey;
    return event.key === 'Delete' || event.key === 'Backspace' || (event.key === 'Backspace' && hasBoundaryModifier);
}

function matchesSlotPattern(pattern, key) {
    if (!pattern) {
        return true;
    }

    try {
        return new RegExp(`^(?:${pattern})$`).test(key);
    } catch {
        return true;
    }
}

function getConfig(root) {
    return rootState.get(root)?.config ?? { length: 0, disabled: false, readOnly: false, direction: 'ltr' };
}

function createHandlers(root, dotNetRef) {
    const onMouseDown = (event) => {
        const target = event.target;
        if (!isOtpInput(target)) {
            return;
        }

        const config = getConfig(root);
        if (config.disabled) {
            return;
        }

        event.preventDefault();
        focusInputElement(root, getInputIndex(root, target));
    };

    const onFocusIn = async (event) => {
        const target = event.target;
        if (!isOtpInput(target)) {
            return;
        }

        const config = getConfig(root);
        if (config.disabled) {
            return;
        }

        const index = getInputIndex(root, target);
        const focusIndex = await dotNetRef.invokeMethodAsync('HandleSlotFocusAsync', index);
        focusInputElement(root, Number.isInteger(focusIndex) ? focusIndex : index);
    };

    const onFocusOut = (event) => {
        if (root.contains(event.relatedTarget)) {
            return;
        }

        dotNetRef.invokeMethodAsync('HandleRootBlurAsync');
    };

    const onInput = async (event) => {
        const target = event.target;
        if (!isOtpInput(target)) {
            return;
        }

        const config = getConfig(root);
        if (config.disabled || config.readOnly) {
            return;
        }

        event.stopPropagation();
        const result = await dotNetRef.invokeMethodAsync('HandleSlotInputAsync', getInputIndex(root, target), target.value ?? '');
        handleResult(root, result);
    };

    const onKeyDown = async (event) => {
        const target = event.target;
        if (!isOtpInput(target)) {
            return;
        }

        const config = getConfig(root);
        if (config.disabled) {
            return;
        }

        const isNavigation = isNavigationKey(event.key, config.direction);
        if (!isNavigation && config.readOnly) {
            return;
        }

        const fullSelection = target.selectionStart === 0 && target.selectionEnd === target.value.length;
        if (!isNavigation && event.key.length === 1 && fullSelection && target.value === event.key) {
            stopEvent(event);
            const index = getInputIndex(root, target);
            if (index < getInputs(root).length - 1) {
                focusInputElement(root, index + 1);
            }
            return;
        }

        // Reject characters that cannot become part of the value so they are never inserted and
        // then removed by the async commit (which would otherwise read as a deletion). The slot
        // pattern filter runs before any custom normalization, so a pattern-failing character can
        // never be rescued; still notify .NET so OnValueInvalid fires.
        if (!isNavigation &&
            event.key.length === 1 &&
            !event.ctrlKey &&
            !event.metaKey &&
            !event.altKey &&
            !matchesSlotPattern(target.pattern, event.key)) {
            stopEvent(event);
            dotNetRef.invokeMethodAsync('HandleSlotInputAsync', getInputIndex(root, target), event.key);
            return;
        }

        if (!isNavigation && !isMutationKey(event, config)) {
            return;
        }

        stopEvent(event);
        const result = await dotNetRef.invokeMethodAsync('HandleSlotKeyDownAsync', getInputIndex(root, target), {
            key: event.key,
            ctrlKey: event.ctrlKey,
            metaKey: event.metaKey,
            altKey: event.altKey,
            selectionStart: target.selectionStart,
            selectionEnd: target.selectionEnd,
            value: target.value
        });
        handleResult(root, result);
    };

    const onPaste = async (event) => {
        const target = event.target;
        if (!isOtpInput(target)) {
            return;
        }

        const config = getConfig(root);
        if (config.disabled || config.readOnly) {
            return;
        }

        let rawValue = '';
        try {
            rawValue = event.clipboardData?.getData('text/plain') ?? '';
        } catch {
            console.warn('Base UI: <OtpFieldInput> could not read clipboard text during paste handling.');
            return;
        }

        event.preventDefault();
        const result = await dotNetRef.invokeMethodAsync('HandleSlotPasteAsync', getInputIndex(root, target), rawValue);
        handleResult(root, result);
    };

    const onHiddenInput = async (event) => {
        const target = event.target;
        if (!(target instanceof HTMLInputElement) || !target.hasAttribute('data-blazix-otp-hidden-input')) {
            return;
        }

        event.stopPropagation();
        const result = await dotNetRef.invokeMethodAsync('HandleHiddenInputChangeAsync', target.value ?? '');
        handleResult(root, result);
    };

    return {
        onMouseDown,
        onFocusIn,
        onFocusOut,
        onInput,
        onKeyDown,
        onPaste,
        onHiddenInput
    };
}

export function initialize(root, hiddenInput, dotNetRef, config) {
    if (!root) {
        return;
    }

    dispose(root);

    const handlers = createHandlers(root, dotNetRef);
    root.addEventListener('mousedown', handlers.onMouseDown);
    root.addEventListener('focusin', handlers.onFocusIn);
    root.addEventListener('focusout', handlers.onFocusOut);
    root.addEventListener('input', handlers.onInput, true);
    root.addEventListener('keydown', handlers.onKeyDown);
    root.addEventListener('paste', handlers.onPaste);
    if (hiddenInput) {
        hiddenInput.addEventListener('input', handlers.onHiddenInput);
        hiddenInput.addEventListener('change', handlers.onHiddenInput);
    }

    rootState.set(root, {
        hiddenInput,
        dotNetRef,
        config: { ...config, direction: getComputedStyle(root).direction || 'ltr' },
        handlers
    });
    if (hiddenInput) {
        syncInputs(root, hiddenInput.value ?? '');
    }
    warnIgnoredFirstSlotAriaLabel(root);
}

export function update(root, hiddenInput, config) {
    const state = rootState.get(root);
    if (!state) {
        return;
    }

    if (state.hiddenInput !== hiddenInput) {
        if (state.hiddenInput) {
            state.hiddenInput.removeEventListener('input', state.handlers.onHiddenInput);
            state.hiddenInput.removeEventListener('change', state.handlers.onHiddenInput);
        }
        if (hiddenInput) {
            hiddenInput.addEventListener('input', state.handlers.onHiddenInput);
            hiddenInput.addEventListener('change', state.handlers.onHiddenInput);
        }
        state.hiddenInput = hiddenInput;
    }

    state.config = { ...config, direction: getComputedStyle(root).direction || 'ltr' };
    if (hiddenInput) {
        syncInputs(root, hiddenInput.value ?? '');
    }
    warnIgnoredFirstSlotAriaLabel(root);
}

export function focusInput(root, index) {
    focusInputElement(root, index);
}

export function requestSubmit(root, hiddenInput, formId, value) {
    if (typeof value === 'string') {
        syncInputs(root, value);
        if (hiddenInput) {
            hiddenInput.value = value;
        }
    }

    let form = hiddenInput?.form ?? getInputs(root)[0]?.form ?? null;
    if (formId) {
        const associatedElement = root?.ownerDocument?.getElementById(formId);
        if (associatedElement?.tagName === 'FORM') {
            form = associatedElement;
        }
    }

    if (form) {
        if (typeof form.checkValidity === 'function' && !form.checkValidity()) {
            form.reportValidity?.();
            return false;
        }

        const submitter = form.ownerDocument.createElement('button');
        submitter.type = 'submit';
        submitter.hidden = true;
        submitter.tabIndex = -1;
        form.append(submitter);
        submitter.click();
        submitter.remove();
        return true;
    }

    return false;
}

export function getNativeValidity(element) {
    if (!element || !element.validity) {
        return null;
    }

    const validity = element.validity;
    return {
        state: {
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
        },
        validationMessage: element.validationMessage || ''
    };
}

export function setCustomValidity(element, message) {
    if (element && typeof element.setCustomValidity === 'function') {
        element.setCustomValidity(message || '');
    }
}

export function dispose(root) {
    const state = rootState.get(root);
    if (!state) {
        return;
    }

    const { hiddenInput, handlers } = state;
    root.removeEventListener('mousedown', handlers.onMouseDown);
    root.removeEventListener('focusin', handlers.onFocusIn);
    root.removeEventListener('focusout', handlers.onFocusOut);
    root.removeEventListener('input', handlers.onInput, true);
    root.removeEventListener('keydown', handlers.onKeyDown);
    root.removeEventListener('paste', handlers.onPaste);
    if (hiddenInput) {
        hiddenInput.removeEventListener('input', handlers.onHiddenInput);
        hiddenInput.removeEventListener('change', handlers.onHiddenInput);
    }

    rootState.delete(root);
}
