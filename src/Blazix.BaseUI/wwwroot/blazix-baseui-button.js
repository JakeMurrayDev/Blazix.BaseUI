const STATE_KEY = Symbol.for('Blazix.BaseUI.Button.State');

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

    state.clickHandler = (event) => {
        if (state.disabled) {
            event.preventDefault();
        }
    };

    state.mouseDownHandler = (event) => {
        if (state.disabled) {
            event.preventDefault();
        }
    };

    state.pointerDownHandler = (event) => {
        if (state.disabled) {
            event.preventDefault();
        }
    };

    state.keydownHandler = (event) => {
        if (state.disabled && state.focusableWhenDisabled && event.key !== 'Tab') {
            event.preventDefault();
            return;
        }

        if (state.disabled) {
            return;
        }

        if (state.nativeButton) {
            return;
        }

        if (event.target !== event.currentTarget) {
            return;
        }

        const isValidLink = element.tagName === 'A' && element.href;
        if (isValidLink) {
            return;
        }

        const isEnterKey = event.key === 'Enter';
        const isSpaceKey = event.key === ' ';

        if (isSpaceKey || isEnterKey) {
            event.preventDefault();
            if (isEnterKey) {
                element.click();
            }
        }
    };

    state.keyupHandler = (event) => {
        if (state.disabled) {
            return;
        }

        if (state.nativeButton) {
            return;
        }

        if (event.target !== event.currentTarget) {
            return;
        }

        const isValidLink = element.tagName === 'A' && element.href;
        if (isValidLink) {
            return;
        }

        if (event.key === ' ') {
            element.click();
        }
    };

    element.addEventListener('click', state.clickHandler);
    element.addEventListener('mousedown', state.mouseDownHandler);
    element.addEventListener('pointerdown', state.pointerDownHandler);
    element.addEventListener('keydown', state.keydownHandler);
    element.addEventListener('keyup', state.keyupHandler);

    element[STATE_KEY] = state;
}
