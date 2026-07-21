const STATE_KEY = Symbol.for('Blazix.BaseUI.Portal.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = new Map();
}

const portalMap = window[STATE_KEY];

export function createPortal(id, target = "body", targetElement = null) {
    const content = document.getElementById(id);
    const container = targetElement instanceof Element || targetElement instanceof ShadowRoot
        ? targetElement
        : document.querySelector(target);

    if (content && container) {
        if (!portalMap.has(id)) {
            portalMap.set(id, content.parentNode);
        }
        container.appendChild(content);
        // Remove display:none immediately after moving so child positioners
        // can measure layout. The FOUC CSS rule (visibility:hidden; position:fixed)
        // on positioner elements keeps content invisible until positioned.
        content.style.removeProperty('display');
    }
}

export function restorePortal(id) {
    const content = document.getElementById(id);
    const originalParent = portalMap.get(id);

    if (content) {
        content.style.setProperty('display', 'none');
    }

    if (content && originalParent) {
        originalParent.appendChild(content);
        portalMap.delete(id);
    } else if (content) {
        content.remove();
        portalMap.delete(id);
    }
}
