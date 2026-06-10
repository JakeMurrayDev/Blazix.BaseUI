/**
 * Press-and-hold interaction module.
 * Provides auto-repeat tick behavior on pointer hold with scroll cancellation.
 * Based on Base UI usePressAndHold.
 */

const STATE_KEY = Symbol.for('Blazix.BaseUI.PressAndHold.State');
if (!window[STATE_KEY]) {
    window[STATE_KEY] = new WeakMap();
}
const state = window[STATE_KEY];

const DEFAULTS = {
    startDelay: 400,
    tickDelay: 60,
    scrollDistance: 8,
    callbackMethod: 'OnTick',
    stopMethod: 'OnStop'
};

function stopHold(entry) {
    if (entry.tickTimeout !== null) {
        clearTimeout(entry.tickTimeout);
        entry.tickTimeout = null;
    }
    if (entry.tickInterval !== null) {
        clearInterval(entry.tickInterval);
        entry.tickInterval = null;
    }
    entry.isPressing = false;
    entry.origin = null;
}

function handlePointerDown(event, element, entry) {
    if (entry.isPressing) return;

    entry.isPressing = true;
    entry.origin = { x: event.clientX, y: event.clientY };
    entry.skipNextClick = false;

    const { options, dotNetRef } = entry;

    dotNetRef.invokeMethodAsync(options.callbackMethod).catch(() => {});

    entry.tickTimeout = setTimeout(() => {
        if (!entry.isPressing) return;
        entry.skipNextClick = true;
        entry.tickInterval = setInterval(() => {
            if (!entry.isPressing) return;
            dotNetRef.invokeMethodAsync(options.callbackMethod).catch(() => {});
        }, options.tickDelay);
    }, options.startDelay);

    const handlePointerUp = () => {
        if (entry.isPressing) {
            const wasAutoRepeating = entry.tickInterval !== null;
            stopHold(entry);
            if (wasAutoRepeating) {
                entry.skipNextClick = true;
            }
            dotNetRef.invokeMethodAsync(options.stopMethod).catch(() => {});
        }
        window.removeEventListener('pointerup', handlePointerUp);
    };
    window.addEventListener('pointerup', handlePointerUp, { once: true });
}

function handlePointerMove(event, element, entry) {
    if (!entry.isPressing || !entry.origin) return;

    const dx = event.clientX - entry.origin.x;
    const dy = event.clientY - entry.origin.y;
    const distance = Math.sqrt(dx * dx + dy * dy);

    if (distance > entry.options.scrollDistance) {
        stopHold(entry);
        entry.dotNetRef.invokeMethodAsync(entry.options.stopMethod).catch(() => {});
    }
}

function handleClick(event, element, entry) {
    if (entry.skipNextClick) {
        event.preventDefault();
        event.stopPropagation();
        entry.skipNextClick = false;
    }
}

export function initialize(element, dotNetRef, userOptions) {
    dispose(element);

    const options = { ...DEFAULTS, ...userOptions };

    const entry = {
        options,
        dotNetRef,
        isPressing: false,
        origin: null,
        tickTimeout: null,
        tickInterval: null,
        skipNextClick: false,
        handlers: {}
    };

    entry.handlers.pointerdown = (e) => handlePointerDown(e, element, entry);
    entry.handlers.pointermove = (e) => handlePointerMove(e, element, entry);
    entry.handlers.click = (e) => handleClick(e, element, entry);

    element.addEventListener('pointerdown', entry.handlers.pointerdown);
    element.addEventListener('pointermove', entry.handlers.pointermove);
    element.addEventListener('click', entry.handlers.click, { capture: true });

    state.set(element, entry);
}

export function dispose(element) {
    if (!element) return;
    const entry = state.get(element);
    if (!entry) return;

    stopHold(entry);

    element.removeEventListener('pointerdown', entry.handlers.pointerdown);
    element.removeEventListener('pointermove', entry.handlers.pointermove);
    element.removeEventListener('click', entry.handlers.click, { capture: true });

    state.delete(element);
}
