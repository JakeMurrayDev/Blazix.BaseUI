const STATE_KEY = Symbol.for('Blazix.BaseUI.Button.State');

function dispatchClickWithModifiers(element, event) {
    const win = element.ownerDocument?.defaultView || window;
    const ClickEvent = win.PointerEvent || win.MouseEvent;
    element.dispatchEvent(new ClickEvent('click', {
        bubbles: true,
        cancelable: true,
        composed: true,
        detail: 0,
        shiftKey: event.shiftKey,
        ctrlKey: event.ctrlKey,
        altKey: event.altKey,
        metaKey: event.metaKey
    }));
}

export function sync(element, disabled, focusableWhenDisabled, nativeButton, dispose) {
    if (!element) {
        return;
    }

    const existingState = element[STATE_KEY];

    if (dispose) {
        if (existingState) {
            element.removeEventListener('click', existingState.clickHandler);
            element.removeEventListener('mousedown', existingState.mouseDownHandler);
            element.removeEventListener('pointerdown', existingState.pointerDownHandler);
            element.removeEventListener('keydown', existingState.keydownHandler);
            element.removeEventListener('keyup', existingState.keyupHandler);
            delete element[STATE_KEY];
        }
        return;
    }

    if (existingState) {
        existingState.disabled = disabled;
        existingState.focusableWhenDisabled = focusableWhenDisabled;
        existingState.nativeButton = nativeButton;
        return;
    }

    const state = {
        disabled,
        focusableWhenDisabled,
        nativeButton,
        clickHandler: null,
        mouseDownHandler: null,
        pointerDownHandler: null,
        keydownHandler: null,
        keyupHandler: null
    };

    // Upstream returns from each handler *without* invoking the consumer's callback while
    // disabled. Blazor delegates events at the document root, so `preventDefault` alone would
    // still let a consumer's `@onclick`/`@onkeydown` run; `stopImmediatePropagation` keeps the
    // event from reaching that root listener, matching a native disabled button (which does not
    // dispatch these events at all).
    const suppressWhileDisabled = (event) => {
        event.stopImmediatePropagation();
    };

    state.clickHandler = (event) => {
        if (state.disabled) {
            event.preventDefault();
            suppressWhileDisabled(event);
        }
    };

    state.mouseDownHandler = (event) => {
        if (state.disabled) {
            event.preventDefault();
            suppressWhileDisabled(event);
        }
    };

    state.pointerDownHandler = (event) => {
        if (state.disabled) {
            event.preventDefault();
            suppressWhileDisabled(event);
        }
    };

    state.keydownHandler = (event) => {
        if (state.disabled) {
            suppressWhileDisabled(event);

            if (state.focusableWhenDisabled && event.key !== 'Tab') {
                event.preventDefault();
            }

            return;
        }

        if (state.nativeButton) {
            return;
        }

        if (event.target !== event.currentTarget) {
            return;
        }

        const isValidLink = element.tagName === 'A' && element.href;
        const isEnterKey = event.key === 'Enter';
        const isSpaceKey = event.key === ' ';

        if (isValidLink) {
            // Space activates links on keyup (`role="button"` semantics, matching the composite
            // path); prevent the page scroll Space would otherwise trigger. Enter is left to the
            // browser's native link activation.
            if (isSpaceKey) {
                event.preventDefault();
            }

            return;
        }

        if (!isSpaceKey && !isEnterKey) {
            return;
        }

        // Match native buttons: preventing the keydown's default cancels activation.
        // Limitation: Blazor delegates events at the document root, so a Blazor `@onkeydown`
        // handler always runs after this listener and its `preventDefault` cannot be observed
        // here. Only listeners that run earlier in the same phase are honoured.
        if (event.defaultPrevented) {
            return;
        }

        event.preventDefault();

        if (isEnterKey) {
            dispatchClickWithModifiers(element, event);
        }
    };

    state.keyupHandler = (event) => {
        if (state.disabled) {
            suppressWhileDisabled(event);
            return;
        }

        if (state.nativeButton) {
            return;
        }

        if (event.target !== event.currentTarget) {
            return;
        }

        if (event.key === ' ') {
            if (event.defaultPrevented) {
                return;
            }
            dispatchClickWithModifiers(element, event);
        }
    };

    element.addEventListener('click', state.clickHandler);
    element.addEventListener('mousedown', state.mouseDownHandler);
    element.addEventListener('pointerdown', state.pointerDownHandler);
    element.addEventListener('keydown', state.keydownHandler);
    element.addEventListener('keyup', state.keyupHandler);

    element[STATE_KEY] = state;
}
