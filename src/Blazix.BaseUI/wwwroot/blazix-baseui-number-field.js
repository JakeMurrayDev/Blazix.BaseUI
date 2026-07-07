const STATE_KEY = Symbol.for('Blazix.BaseUI.NumberField.State');
if (!window[STATE_KEY]) {
    window[STATE_KEY] = new WeakMap();
}
const state = window[STATE_KEY];

const CHANGE_VALUE_TICK_DELAY = 60;
const START_AUTO_CHANGE_DELAY = 400;
const TOUCH_TIMEOUT = 50;
const MAX_POINTER_MOVES_AFTER_TOUCH = 3;
const SCROLLING_POINTER_MOVE_DISTANCE = 8;
const DEFAULT_STEP = 1;
const NAVIGATE_KEYS = new Set(['Backspace', 'Delete', 'ArrowLeft', 'ArrowRight', 'Tab', 'Enter', 'Escape']);
const ACTION_KEYS = new Set(['ArrowUp', 'ArrowDown', 'Home', 'End']);
const BASE_NON_NUMERIC_SYMBOLS = ['.', ',', '．', '，', '٫', '٬'];
const PERCENTAGES = ['%', '٪', '％', '﹪'];
const PERMILLE = ['‰', '؉'];
const PLUS_SIGNS_WITH_ASCII = ['+', '＋', '﹢'];
const MINUS_SIGNS_WITH_ASCII = ['-', '−', '－', '‒', '–', '—', '﹣'];
const ANY_MINUS_RE = /[-−－‒–—﹣]/u;
const ANY_PLUS_RE = /[+＋﹢]/u;
const ARABIC_DETECT_RE = /[٠١٢٣٤٥٦٧٨٩]/u;
const PERSIAN_DETECT_RE = /[۰۱۲۳۴۵۶۷۸۹]/u;
const HAN_DETECT_RE = /[零〇一二三四五六七八九]/u;
const FULLWIDTH_DETECT_RE = /[０１２３４５６７８９]/u;
const SPACE_SEPARATOR_RE = /\p{Zs}/u;
const FORMAT_CONTROL_DETECT_RE = /\p{Cf}/u;

export function initialize(inputElement, dotNetRef, config) {
    if (!inputElement) return;

    let elementState = state.get(inputElement);
    if (elementState) {
        removeInputEventHandlers(inputElement, elementState);
    } else {
        elementState = {
            wheelHandler: null,
            autoChangeState: null
        };
    }

    Object.assign(elementState, {
        dotNetRef,
        config: normalizeConfig(config),
        keydownHandler: (event) => handleInputKeyDown(inputElement, event),
        pasteHandler: (event) => handleInputPaste(inputElement, event)
    });

    inputElement.addEventListener('keydown', elementState.keydownHandler);
    inputElement.addEventListener('paste', elementState.pasteHandler);

    state.set(inputElement, elementState);
}

export function dispose(inputElement) {
    if (!inputElement) return;

    const elementState = state.get(inputElement);
    if (elementState) {
        if (elementState.wheelHandler) {
            inputElement.removeEventListener('wheel', elementState.wheelHandler);
        }
        removeInputEventHandlers(inputElement, elementState);
        stopAutoChangeInternal(elementState);
    }

    state.delete(inputElement);
}

export function updateConfig(inputElement, config) {
    if (!inputElement) return;

    const elementState = state.get(inputElement);
    if (elementState) {
        elementState.config = normalizeConfig(config);
    }
}

function removeInputEventHandlers(inputElement, elementState) {
    if (elementState.keydownHandler) {
        inputElement.removeEventListener('keydown', elementState.keydownHandler);
        elementState.keydownHandler = null;
    }

    if (elementState.pasteHandler) {
        inputElement.removeEventListener('paste', elementState.pasteHandler);
        elementState.pasteHandler = null;
    }
}

function normalizeConfig(config) {
    return config || {};
}

function normalizeFormatOptions(format) {
    if (!format) return undefined;

    const options = {};
    Object.entries(format).forEach(([key, value]) => {
        if (value !== null && value !== undefined) {
            options[key] = value;
        }
    });

    return Object.keys(options).length === 0 ? undefined : options;
}

function getFormatter(locale, format) {
    try {
        return new Intl.NumberFormat(locale || undefined, normalizeFormatOptions(format));
    } catch {
        return new Intl.NumberFormat(undefined, normalizeFormatOptions(format));
    }
}

function getNumberLocaleDetails(locale, format) {
    const result = {};

    getFormatParts(locale, format).forEach((part) => {
        result[part.type] = part.value;
    });

    getFormatter(locale, undefined).formatToParts(0.1).forEach((part) => {
        if (part.type === 'decimal') {
            result.decimal = part.value;
        }
    });

    return result;
}

function getFormatParts(locale, format) {
    return getFormatter(locale, format).formatToParts(11111.1);
}

function getAllowedNonNumericKeys(config) {
    const format = config.format;
    const style = format?.style;
    const parts = getFormatParts(config.locale, format);
    const details = getNumberLocaleDetails(config.locale, format);
    const decimal = parts.find((part) => part.type === 'decimal')?.value ?? details.decimal;
    const keys = new Set(BASE_NON_NUMERIC_SYMBOLS);

    if (decimal) {
        keys.add(decimal);
    }

    parts.forEach((part) => {
        if (
            part.type === 'integer' ||
            part.type === 'fraction' ||
            part.type === 'exponentInteger' ||
            part.type === 'compact'
        ) {
            return;
        }

        Array.from(part.value).forEach((char) => keys.add(char));
        if (SPACE_SEPARATOR_RE.test(part.value)) {
            keys.add(' ');
        }
    });

    const allowPercentSymbols = style === 'percent' || (style === 'unit' && format?.unit === 'percent');
    const allowPermilleSymbols = style === 'percent' || (style === 'unit' && format?.unit === 'permille');

    if (allowPercentSymbols) {
        PERCENTAGES.forEach((key) => keys.add(key));
    }

    if (allowPermilleSymbols) {
        PERMILLE.forEach((key) => keys.add(key));
    }

    PLUS_SIGNS_WITH_ASCII.forEach((key) => keys.add(key));
    if ((config.minWithDefault ?? Number.MIN_SAFE_INTEGER) < 0 || config.allowOutOfRange) {
        MINUS_SIGNS_WITH_ASCII.forEach((key) => keys.add(key));
    }

    return keys;
}

function handleInputKeyDown(inputElement, event) {
    const elementState = state.get(inputElement);
    const config = elementState?.config || {};
    if (event.defaultPrevented || config.readOnly || config.disabled) {
        return;
    }

    if (event.which === 229 || event.ctrlKey || event.metaKey) {
        return;
    }

    const isStepKey = event.key === 'ArrowUp' || event.key === 'ArrowDown';
    const willSetHome = event.key === 'Home' && config.min != null;
    const willSetEnd = event.key === 'End' && config.max != null;

    if (isStepKey || willSetHome || willSetEnd) {
        event.preventDefault();
        return;
    }

    if (event.altKey || NAVIGATE_KEYS.has(event.key)) {
        return;
    }

    if (event.key.length > 1) {
        return;
    }

    if (isAllowedInputKey(inputElement, event, config)) {
        return;
    }

    event.preventDefault();
}

function isAllowedInputKey(inputElement, event, config) {
    const key = event.key;
    const allowedNonNumericKeys = getAllowedNonNumericKeys(config);
    let isAllowedNonNumericKey = allowedNonNumericKeys.has(key);
    const inputValue = inputElement.value || '';
    const selectionStart = inputElement.selectionStart;
    const selectionEnd = inputElement.selectionEnd;
    const isAllSelected = selectionStart === 0 && selectionEnd === inputValue.length;
    const selectionContainsIndex = (index) =>
        selectionStart != null &&
        selectionEnd != null &&
        index >= selectionStart &&
        index < selectionEnd;

    if (ANY_MINUS_RE.test(key) && Array.from(allowedNonNumericKeys).some((candidate) => ANY_MINUS_RE.test(candidate || ''))) {
        const existingIndex = inputValue.search(ANY_MINUS_RE);
        const isReplacingExisting = existingIndex !== -1 && selectionContainsIndex(existingIndex);
        isAllowedNonNumericKey =
            !(ANY_MINUS_RE.test(inputValue) || ANY_PLUS_RE.test(inputValue)) ||
            isAllSelected ||
            isReplacingExisting;
    }

    if (ANY_PLUS_RE.test(key) && Array.from(allowedNonNumericKeys).some((candidate) => ANY_PLUS_RE.test(candidate || ''))) {
        const existingIndex = inputValue.search(ANY_PLUS_RE);
        const isReplacingExisting = existingIndex !== -1 && selectionContainsIndex(existingIndex);
        isAllowedNonNumericKey =
            !(ANY_MINUS_RE.test(inputValue) || ANY_PLUS_RE.test(inputValue)) ||
            isAllSelected ||
            isReplacingExisting;
    }

    const { decimal, currency, percentSign } = getNumberLocaleDetails(config.locale, config.format);
    [decimal, currency, percentSign].forEach((symbol) => {
        if (key === symbol) {
            const symbolIndex = inputValue.indexOf(symbol);
            const isSymbolHighlighted = selectionContainsIndex(symbolIndex);
            isAllowedNonNumericKey = !inputValue.includes(symbol) || isAllSelected || isSymbolHighlighted;
        }
    });

    return isAllowedNonNumericKey ||
        (key >= '0' && key <= '9') ||
        ARABIC_DETECT_RE.test(key) ||
        PERSIAN_DETECT_RE.test(key) ||
        HAN_DETECT_RE.test(key) ||
        FULLWIDTH_DETECT_RE.test(key) ||
        FORMAT_CONTROL_DETECT_RE.test(key);
}

function handleInputPaste(inputElement, event) {
    const elementState = state.get(inputElement);
    const config = elementState?.config || {};
    if (event.defaultPrevented || config.readOnly || config.disabled || !elementState?.dotNetRef) {
        return;
    }

    let pastedData = '';
    try {
        pastedData = event.clipboardData?.getData('text/plain') ?? '';
    } catch {
        return;
    }

    const inputValue = inputElement.value || '';
    const selectionStart = inputElement.selectionStart ?? inputValue.length;
    const selectionEnd = inputElement.selectionEnd ?? inputValue.length;
    const nextText = inputValue.slice(0, selectionStart) + pastedData + inputValue.slice(selectionEnd);
    const caret = selectionStart + pastedData.length;

    event.preventDefault();
    elementState.dotNetRef
        .invokeMethodAsync('OnPasteText', nextText)
        .then(() => requestAnimationFrame(() => setSelectionRange(inputElement, caret, caret)))
        .catch(() => { });
}

export function registerWheelListener(inputElement, dotNetRef, disabled, readOnly) {
    if (!inputElement) return;

    let elementState = state.get(inputElement);
    if (!elementState) {
        elementState = { dotNetRef, config: {}, wheelHandler: null, autoChangeState: null };
        state.set(inputElement, elementState);
    }

    elementState.dotNetRef = dotNetRef;

    if (elementState.wheelHandler) {
        inputElement.removeEventListener('wheel', elementState.wheelHandler);
    }

    if (disabled || readOnly) return;

    elementState.wheelHandler = (event) => {
        if (event.ctrlKey || document.activeElement !== inputElement) {
            return;
        }

        event.preventDefault();

        const direction = event.deltaY > 0 ? -1 : 1;
        const altKey = event.altKey;
        const shiftKey = event.shiftKey;

        dotNetRef.invokeMethodAsync('OnWheelChange', direction, altKey, shiftKey);
    };

    inputElement.addEventListener('wheel', elementState.wheelHandler, { passive: false });
}

export function unregisterWheelListener(inputElement) {
    if (!inputElement) return;

    const elementState = state.get(inputElement);
    if (elementState && elementState.wheelHandler) {
        inputElement.removeEventListener('wheel', elementState.wheelHandler);
        elementState.wheelHandler = null;
    }
}

export function startAutoChange(inputElement, dotNetRef, isIncrement, step, min, max) {
    if (!inputElement) return;

    let elementState = state.get(inputElement);
    if (!elementState) {
        elementState = { dotNetRef, config: {}, wheelHandler: null, autoChangeState: null };
        state.set(inputElement, elementState);
    }

    stopAutoChangeInternal(elementState);

    elementState.autoChangeState = {
        isIncrement,
        step,
        min,
        max,
        tickTimeout: null,
        tickInterval: null,
        isPressing: true
    };

    const tick = () => {
        if (!elementState.autoChangeState || !elementState.autoChangeState.isPressing) return;
        dotNetRef.invokeMethodAsync('OnAutoChangeTick', isIncrement);
    };

    tick();

    elementState.autoChangeState.tickTimeout = setTimeout(() => {
        if (!elementState.autoChangeState) return;
        elementState.autoChangeState.tickInterval = setInterval(tick, CHANGE_VALUE_TICK_DELAY);
    }, START_AUTO_CHANGE_DELAY);

    const handlePointerUp = () => {
        stopAutoChangeInternal(elementState);
        dotNetRef.invokeMethodAsync('OnAutoChangeEnd', isIncrement);
        window.removeEventListener('pointerup', handlePointerUp);
    };

    window.addEventListener('pointerup', handlePointerUp, { once: true });
}

export function stopAutoChange(inputElement) {
    if (!inputElement) return;

    const elementState = state.get(inputElement);
    if (elementState) {
        stopAutoChangeInternal(elementState);
    }
}

function stopAutoChangeInternal(elementState) {
    if (!elementState || !elementState.autoChangeState) return;

    if (elementState.autoChangeState.tickTimeout) {
        clearTimeout(elementState.autoChangeState.tickTimeout);
    }
    if (elementState.autoChangeState.tickInterval) {
        clearInterval(elementState.autoChangeState.tickInterval);
    }
    elementState.autoChangeState = null;
}

export function initializeScrubArea(scrubAreaElement, dotNetRef, config) {
    if (!scrubAreaElement) return;

    const elementState = {
        dotNetRef,
        config,
        isScrubbingRef: false,
        virtualCursorCoords: { x: 0, y: 0 },
        visualScaleRef: 1,
        visualResizeHandler: null,
        cumulativeDelta: 0,
        boundHandlers: null,
        cursorElement: null
    };

    state.set(scrubAreaElement, elementState);

    scrubAreaElement.addEventListener('touchstart', (event) => {
        if (event.touches.length === 1) {
            event.preventDefault();
        }
    }, { passive: false });
}

export function disposeScrubArea(scrubAreaElement) {
    if (!scrubAreaElement) return;

    const elementState = state.get(scrubAreaElement);
    if (elementState && elementState.boundHandlers) {
        window.removeEventListener('pointerup', elementState.boundHandlers.pointerUp, true);
        window.removeEventListener('pointermove', elementState.boundHandlers.pointerMove, true);
    }
    unsubscribeFromVisualViewportResize(elementState);

    state.delete(scrubAreaElement);
}

export async function startScrub(scrubAreaElement, dotNetRef, cursorElement, config, clientX, clientY, isTouch) {
    if (!scrubAreaElement) return { success: false };

    let elementState = state.get(scrubAreaElement);
    if (!elementState) {
        elementState = {
            dotNetRef,
            config,
            isScrubbingRef: false,
            virtualCursorCoords: { x: 0, y: 0 },
            visualScaleRef: 1,
            visualResizeHandler: null,
            cumulativeDelta: 0,
            boundHandlers: null,
            cursorElement: null
        };
        state.set(scrubAreaElement, elementState);
    }

    elementState.dotNetRef = dotNetRef;
    elementState.config = config;
    elementState.cursorElement = cursorElement;
    elementState.isScrubbingRef = true;
    elementState.cumulativeDelta = 0;

    if (cursorElement) {
        subscribeToVisualViewportResize(elementState);
        const initialCoords = {
            x: clientX - cursorElement.offsetWidth / 2,
            y: clientY - cursorElement.offsetHeight / 2
        };
        elementState.virtualCursorCoords = initialCoords;
        updateCursorTransformInternal(cursorElement, initialCoords.x, initialCoords.y, elementState.visualScaleRef);
    }

    if (elementState.boundHandlers) {
        window.removeEventListener('pointerup', elementState.boundHandlers.pointerUp, true);
        window.removeEventListener('pointermove', elementState.boundHandlers.pointerMove, true);
    }

    elementState.boundHandlers = {
        pointerMove: (e) => handleScrubPointerMove(scrubAreaElement, e),
        pointerUp: (e) => handleScrubPointerUp(scrubAreaElement, e)
    };

    window.addEventListener('pointermove', elementState.boundHandlers.pointerMove, true);
    window.addEventListener('pointerup', elementState.boundHandlers.pointerUp, true);

    let pointerLockDenied = false;
    if (!isTouch && !isWebKit()) {
        try {
            await document.body.requestPointerLock();
        } catch {
            pointerLockDenied = true;
        }
    }

    return { success: true, pointerLockDenied };
}

function handleScrubPointerMove(scrubAreaElement, event) {
    const elementState = state.get(scrubAreaElement);
    if (!elementState || !elementState.isScrubbingRef || !elementState.config) return;

    event.preventDefault();

    const config = elementState.config;
    const { movementX, movementY } = event;

    if (elementState.cursorElement) {
        const rect = getViewportRect(config.teleportDistance, scrubAreaElement);
        const coords = elementState.virtualCursorCoords;
        const newCoords = {
            x: Math.round(coords.x + movementX),
            y: Math.round(coords.y + movementY)
        };

        const cursorWidth = elementState.cursorElement.offsetWidth;
        const cursorHeight = elementState.cursorElement.offsetHeight;

        if (newCoords.x + cursorWidth / 2 < rect.x) {
            newCoords.x = rect.width - cursorWidth / 2;
        } else if (newCoords.x + cursorWidth / 2 > rect.width) {
            newCoords.x = rect.x - cursorWidth / 2;
        }

        if (newCoords.y + cursorHeight / 2 < rect.y) {
            newCoords.y = rect.height - cursorHeight / 2;
        } else if (newCoords.y + cursorHeight / 2 > rect.height) {
            newCoords.y = rect.y - cursorHeight / 2;
        }

        elementState.virtualCursorCoords = newCoords;
        updateCursorTransformInternal(elementState.cursorElement, newCoords.x, newCoords.y, elementState.visualScaleRef);
    }

    const isVertical = config.direction === 'vertical';
    elementState.cumulativeDelta += isVertical ? movementY : movementX;

    if (Math.abs(elementState.cumulativeDelta) >= config.pixelSensitivity) {
        elementState.cumulativeDelta = 0;
        const dValue = isVertical ? -movementY : movementX;

        if (dValue !== 0) {
            const direction = dValue >= 0 ? 1 : -1;
            elementState.dotNetRef.invokeMethodAsync('OnScrubMove', Math.abs(dValue), direction, event.altKey, event.shiftKey);
        }
    }
}

function handleScrubPointerUp(scrubAreaElement, event) {
    const elementState = state.get(scrubAreaElement);
    if (!elementState) return;

    try {
        document.exitPointerLock();
    } catch { }

    if (elementState.boundHandlers) {
        window.removeEventListener('pointermove', elementState.boundHandlers.pointerMove, true);
        window.removeEventListener('pointerup', elementState.boundHandlers.pointerUp, true);
    }
    unsubscribeFromVisualViewportResize(elementState);

    elementState.isScrubbingRef = false;
    elementState.boundHandlers = null;

    if (elementState.dotNetRef) {
        elementState.dotNetRef.invokeMethodAsync('OnScrubEnd');
    }
}

function getViewportRect(teleportDistance, element) {
    const vv = window.visualViewport;
    if (teleportDistance != null) {
        const rect = element.getBoundingClientRect();
        return {
            x: rect.left - teleportDistance / 2,
            y: rect.top - teleportDistance / 2,
            width: rect.right + teleportDistance / 2,
            height: rect.bottom + teleportDistance / 2
        };
    }
    return {
        x: vv?.offsetLeft ?? 0,
        y: vv?.offsetTop ?? 0,
        width: vv ? vv.offsetLeft + vv.width : document.documentElement.clientWidth,
        height: vv ? vv.offsetTop + vv.height : document.documentElement.clientHeight
    };
}

function subscribeToVisualViewportResize(elementState) {
    unsubscribeFromVisualViewportResize(elementState);

    const vv = window.visualViewport;
    if (!vv) return;

    elementState.visualResizeHandler = () => {
        elementState.visualScaleRef = vv.scale || 1;
    };
    elementState.visualResizeHandler();
    vv.addEventListener('resize', elementState.visualResizeHandler);
}

function unsubscribeFromVisualViewportResize(elementState) {
    if (elementState?.visualResizeHandler && window.visualViewport) {
        window.visualViewport.removeEventListener('resize', elementState.visualResizeHandler);
        elementState.visualResizeHandler = null;
    }
}

function updateCursorTransformInternal(cursorElement, x, y, scale) {
    if (cursorElement) {
        cursorElement.style.transform = `translate3d(${x}px,${y}px,0) scale(${1 / scale})`;
    }
}

export function updateCursorTransform(cursorElement, x, y, scale) {
    updateCursorTransformInternal(cursorElement, x, y, scale);
}

export function focusInput(inputElement) {
    if (inputElement) {
        inputElement.focus({ preventScroll: true });
    }
}

export function setInputValue(inputElement, value) {
    if (inputElement) {
        inputElement.value = value;
    }
}

export function setSelectionRange(inputElement, start, end) {
    if (inputElement && typeof inputElement.setSelectionRange === 'function') {
        try {
            inputElement.setSelectionRange(start, end);
        } catch { }
    }
}

export function getSelectionStart(inputElement) {
    if (inputElement) {
        return inputElement.selectionStart;
    }
    return 0;
}

export function getSelectionEnd(inputElement) {
    if (inputElement) {
        return inputElement.selectionEnd;
    }
    return 0;
}

function isWebKit() {
    return /AppleWebKit/.test(navigator.userAgent) && !/Chrome/.test(navigator.userAgent);
}

export function isWebKitBrowser() {
    return isWebKit();
}

export function isFirefoxBrowser() {
    return /Firefox/.test(navigator.userAgent);
}
