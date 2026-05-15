/**
 * BlazorBaseUI Floating Infrastructure
 *
 * Shared positioning and interaction utilities for floating elements.
 * Ported from Base UI's floating-ui-react implementation.
 * Uses vendored Floating UI library for positioning.
 */

import { requestDoubleAnimationFrame } from './blazor-baseui-animations.js';

const STATE_KEY = Symbol.for('BlazorBaseUI.Floating.State');

if (!window[STATE_KEY]) {
    window[STATE_KEY] = {
        trees: new Map(),
        positioners: new Map(),
        interactions: new Map(),
        floatingUIPromise: null
    };
}

const state = window[STATE_KEY];

// Inject CSS to hide positioners until JS has computed their position.
// Prevents flash-of-unpositioned-content when elements are portaled.
if (!document.querySelector('[data-baseui-positioner-css]')) {
    const style = document.createElement('style');
    style.setAttribute('data-baseui-positioner-css', '');
    style.textContent = [
        '[data-blazor-base-ui-positioner][role="presentation"][data-side]:not([data-positioned]) { visibility: hidden !important; position: fixed !important; }',
        '[role="presentation"][data-side]:not([data-open]) { pointer-events: none; }'
    ].join('\n');
    document.head.appendChild(style);
}

// ============================================================================
// Browser Detection
// ============================================================================

export function isSafari() {
    return typeof navigator !== 'undefined' && /apple/i.test(navigator.vendor);
}

// ============================================================================
// Floating UI Library Loading
// ============================================================================

const FLOATING_UI_KEY = Symbol.for('BlazorBaseUI.FloatingUI');

async function loadScript(src) {
    return new Promise((resolve, reject) => {
        const existing = document.querySelector(`script[src="${src}"]`);
        if (existing) {
            // If already loaded, resolve immediately
            if (existing.dataset.loaded === 'true') {
                resolve();
                return;
            }
            // If still loading, wait for it
            existing.addEventListener('load', resolve);
            existing.addEventListener('error', reject);
            return;
        }

        const script = document.createElement('script');
        script.src = src;
        script.onload = () => {
            script.dataset.loaded = 'true';
            resolve();
        };
        script.onerror = reject;
        document.head.appendChild(script);
    });
}

async function ensureFloatingUI() {
    if (window[FLOATING_UI_KEY]) {
        return window[FLOATING_UI_KEY];
    }

    if (state.floatingUIPromise) {
        return state.floatingUIPromise;
    }

    state.floatingUIPromise = (async () => {
        const basePath = './_content/BlazorBaseUI/vendor/';

        await loadScript(basePath + 'floating-ui.core.min.js');
        await loadScript(basePath + 'floating-ui.dom.min.js');

        if (typeof FloatingUIDOM !== 'undefined') {
            window[FLOATING_UI_KEY] = FloatingUIDOM;
            return FloatingUIDOM;
        }

        throw new Error('Failed to load Floating UI libraries');
    })();

    return state.floatingUIPromise;
}

// ============================================================================
// Constants
// ============================================================================

const TYPEABLE_SELECTOR = 'input:not([type="hidden"]):not([disabled]),' +
    '[contenteditable]:not([contenteditable="false"]),' +
    'textarea:not([disabled])';

// ============================================================================
// Event Emitter
// ============================================================================

export function createEventEmitter() {
    const map = new Map();
    return {
        emit(event, data) {
            map.get(event)?.forEach(listener => listener(data));
        },
        on(event, listener) {
            if (!map.has(event)) {
                map.set(event, new Set());
            }
            map.get(event).add(listener);
        },
        off(event, listener) {
            map.get(event)?.delete(listener);
        }
    };
}

// ============================================================================
// DOM Utilities
// ============================================================================

export function activeElement(doc) {
    let element = doc.activeElement;
    while (element?.shadowRoot?.activeElement != null) {
        element = element.shadowRoot.activeElement;
    }
    return element;
}

export function contains(parent, child) {
    if (!parent || !child) {
        return false;
    }

    const rootNode = child.getRootNode?.();

    // First, attempt with faster native method
    if (parent.contains(child)) {
        return true;
    }

    // Fallback to custom implementation with Shadow DOM support
    if (rootNode && rootNode.nodeType === 11) { // ShadowRoot
        let next = child;
        while (next) {
            if (parent === next) {
                return true;
            }
            next = next.parentNode || next.host;
        }
    }

    return false;
}

export function getTarget(event) {
    if ('composedPath' in event) {
        return event.composedPath()[0];
    }
    return event.target;
}

export function isEventTargetWithin(event, node) {
    if (node == null) {
        return false;
    }

    if ('composedPath' in event) {
        return event.composedPath().includes(node);
    }

    return event.target != null && node.contains(event.target);
}

export function isRootElement(element) {
    return element.matches('html,body');
}

export function getDocument(node) {
    return node?.ownerDocument || document;
}

export function isTypeableElement(element) {
    return element instanceof HTMLElement && element.matches(TYPEABLE_SELECTOR);
}

export function isMouseLikePointerType(pointerType, strict = false) {
    const pointer = pointerType;
    if (!pointer) {
        return !strict;
    }
    return pointer === 'mouse' || pointer === 'pen';
}

export function isHTMLElement(value) {
    return value instanceof HTMLElement;
}

export function isElement(value) {
    return value instanceof Element;
}

// ============================================================================
// Floating Tree Store
// ============================================================================

export class FloatingTreeStore {
    constructor() {
        this.nodes = [];
        this.events = createEventEmitter();
    }

    addNode(node) {
        this.nodes.push(node);
    }

    removeNode(node) {
        const index = this.nodes.findIndex(n => n === node);
        if (index !== -1) {
            this.nodes.splice(index, 1);
        }
    }

    getNodeChildren(nodeId, onlyOpenChildren = true) {
        return this.nodes
            .filter(n => n.parentId === nodeId && (!onlyOpenChildren || n.context?.open))
            .flatMap(child => [child, ...this.getNodeChildren(child.id, onlyOpenChildren)]);
    }

    getDeepestNode(nodeId) {
        let deepestNodeId = nodeId;
        let maxDepth = -1;

        const findDeepest = (currentNodeId, depth) => {
            if (depth > maxDepth) {
                deepestNodeId = currentNodeId;
                maxDepth = depth;
            }
            const children = this.getNodeChildren(currentNodeId);
            children.forEach(child => findDeepest(child.id, depth + 1));
        };

        findDeepest(nodeId, 0);
        return this.nodes.find(n => n.id === deepestNodeId);
    }

    getNodeAncestors(nodeId) {
        const ancestors = [];
        let current = this.nodes.find(n => n.id === nodeId);
        while (current?.parentId) {
            const parent = this.nodes.find(n => n.id === current.parentId);
            if (parent) {
                ancestors.push(parent);
                current = parent;
            } else {
                break;
            }
        }
        return ancestors;
    }
}

export function getFloatingTree(treeId) {
    if (!state.trees.has(treeId)) {
        state.trees.set(treeId, new FloatingTreeStore());
    }
    return state.trees.get(treeId);
}

export function disposeFloatingTree(treeId) {
    state.trees.delete(treeId);
}

export function addTreeNode(treeId, nodeId, parentId) {
    const tree = state.trees.get(treeId);
    if (!tree) return;
    tree.addNode({ id: nodeId, parentId: parentId, context: null });
}

export function removeTreeNode(treeId, nodeId) {
    const tree = state.trees.get(treeId);
    if (!tree) return;
    const node = tree.nodes.find(n => n.id === nodeId);
    if (node) tree.removeNode(node);
}

export function updateTreeNodeContext(treeId, nodeId, context) {
    const tree = state.trees.get(treeId);
    if (!tree) return;
    const node = tree.nodes.find(n => n.id === nodeId);
    if (node) node.context = context;
}

// ============================================================================
// Timeout Utility
// ============================================================================

export class Timeout {
    constructor() {
        this.id = null;
    }

    start(ms, callback) {
        this.clear();
        this.id = setTimeout(callback, ms);
    }

    clear() {
        if (this.id !== null) {
            clearTimeout(this.id);
            this.id = null;
        }
    }
}

// ============================================================================
// Scroll Utilities
// ============================================================================

export function getScrollParents(element) {
    const scrollParents = [];
    let current = element.parentElement;

    while (current) {
        const style = getComputedStyle(current);
        const overflow = style.overflow + style.overflowX + style.overflowY;
        if (/auto|scroll|overlay/.test(overflow)) {
            scrollParents.push(current);
        }
        current = current.parentElement;
    }

    scrollParents.push(window);
    return scrollParents;
}

export function getCollisionBounds(element, collisionBoundary) {
    const viewportBounds = {
        top: 0,
        left: 0,
        right: window.innerWidth,
        bottom: window.innerHeight,
        width: window.innerWidth,
        height: window.innerHeight
    };

    if (collisionBoundary === 'viewport') {
        return viewportBounds;
    }

    let bounds = { ...viewportBounds };
    let current = element.parentElement;

    while (current && current !== document.body) {
        const style = getComputedStyle(current);
        const overflow = style.overflow + style.overflowX + style.overflowY;

        if (/auto|scroll|hidden/.test(overflow)) {
            const rect = current.getBoundingClientRect();
            bounds = {
                top: Math.max(bounds.top, rect.top),
                left: Math.max(bounds.left, rect.left),
                right: Math.min(bounds.right, rect.right),
                bottom: Math.min(bounds.bottom, rect.bottom),
                width: Math.min(bounds.right, rect.right) - Math.max(bounds.left, rect.left),
                height: Math.min(bounds.bottom, rect.bottom) - Math.max(bounds.top, rect.top)
            };
        }
        current = current.parentElement;
    }

    return bounds;
}

// ============================================================================
// Positioning Engine (using Floating UI)
// ============================================================================

/**
 * Converts our side/align to Floating UI placement.
 */
function toFloatingPlacement(side, align) {
    // Handle logical sides
    const isRtl = document.documentElement.dir === 'rtl';
    let physicalSide = side;
    if (side === 'inline-end') {
        physicalSide = isRtl ? 'left' : 'right';
    } else if (side === 'inline-start') {
        physicalSide = isRtl ? 'right' : 'left';
    }

    if (align === 'center') {
        return physicalSide;
    }
    return `${physicalSide}-${align}`;
}

/**
 * Parses a Floating UI placement back to side/align.
 */
function fromFloatingPlacement(placement) {
    const parts = placement.split('-');
    const side = parts[0];
    const align = parts[1] || 'center';
    return { side, align };
}

export async function initializePositioner(options) {
    const {
        positionerElement,
        triggerElement,
        virtualId = null,
        side = 'bottom',
        align = 'center',
        sideOffset = 0,
        alignOffset = 0,
        collisionPadding = 0,
        collisionBoundary = 'clipping-ancestors',
        arrowPadding = 0,
        arrowElement = null,
        sticky = false,
        positionMethod = 'absolute',
        disableAnchorTracking = false,
        collisionAvoidance = null,
        hasViewport = false
    } = options;

    // Support virtual element as alternative to DOM trigger
    const effectiveTrigger = virtualId
        ? virtualElements.get(virtualId)
        : triggerElement;

    if (!positionerElement || !effectiveTrigger) return null;

    const positionerId = positionerElement.id ||
        `positioner-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

    const positionerState = {
        positionerId,
        positionerElement,
        triggerElement: effectiveTrigger,
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
        collisionAvoidance,
        hasViewport,
        // Mirrors React's `alignItemWithTriggerActive ? FIXED : positioning.positionerStyles`
        // in SelectPositioner.tsx. When true, updatePositionInternal computes
        // placement (so onPositionUpdated still fires) but DOES NOT write
        // position/top/left/visibility/data-positioned. Component-specific
        // align-item passes own those writes.
        alignItemWithTriggerActive: !!options.alignItemWithTriggerActive,
        onPositionUpdated: options.onPositionUpdated || null,
        dotNetRef: options.dotNetRef || null,
        hasSideOffsetFn: options.hasSideOffsetFn || false,
        hasAlignOffsetFn: options.hasAlignOffsetFn || false,
        cleanup: null,
        computedSide: side,
        computedAlign: align
    };

    state.positioners.set(positionerId, positionerState);
    await updatePositionInternal(positionerState);
    await setupAutoUpdate(positionerState);

    return positionerId;
}

export async function updatePositioner(positionerId, options) {
    const positionerState = state.positioners.get(positionerId);
    if (!positionerState) return;

    Object.assign(positionerState, options);
    await updatePositionInternal(positionerState);
}

export function disposePositioner(positionerId) {
    if (!positionerId) return;

    const positionerState = state.positioners.get(positionerId);
    if (positionerState) {
        cleanupAutoUpdate(positionerState);
        positionerState.positionerElement?.removeAttribute('data-positioned');
        state.positioners.delete(positionerId);
    }
}

async function setupAutoUpdate(positionerState) {
    const { positionerElement, triggerElement } = positionerState;
    if (!positionerElement || !triggerElement) return;

    cleanupAutoUpdate(positionerState);

    try {
        const FloatingUI = await ensureFloatingUI();
        const trackAnchor = !positionerState.disableAnchorTracking;

        const cleanup = FloatingUI.autoUpdate(
            triggerElement,
            positionerElement,
            () => updatePositionInternal(positionerState),
            {
                ancestorScroll: true,
                ancestorResize: true,
                elementResize: trackAnchor && typeof ResizeObserver !== 'undefined',
                layoutShift: trackAnchor && typeof IntersectionObserver !== 'undefined'
            }
        );

        positionerState.cleanup = cleanup;
    } catch {
        // Fallback to manual scroll/resize listeners if Floating UI fails
        const update = () => updatePositionInternal(positionerState);
        const scrollParents = getScrollParents(triggerElement);

        scrollParents.forEach(parent => {
            parent.addEventListener('scroll', update, { passive: true });
        });
        window.addEventListener('resize', update, { passive: true });

        positionerState.cleanup = () => {
            scrollParents.forEach(parent => {
                parent.removeEventListener('scroll', update);
            });
            window.removeEventListener('resize', update);
        };
    }
}

function cleanupAutoUpdate(positionerState) {
    if (positionerState.cleanup) {
        positionerState.cleanup();
        positionerState.cleanup = null;
    }
}

/**
 * Normalizes collision avoidance settings from either string format (menu)
 * or object format (popover/tooltip) into a consistent object.
 */
export function normalizeCollisionAvoidance(collisionAvoidance) {
    if (!collisionAvoidance) {
        return { side: 'flip', align: 'flip', fallbackAxisSide: 'end' };
    }
    if (typeof collisionAvoidance === 'string') {
        switch (collisionAvoidance) {
            case 'none': return { side: 'none', align: 'none', fallbackAxisSide: 'none' };
            case 'shift': return { side: 'shift', align: 'shift', fallbackAxisSide: 'none' };
            case 'flip': return { side: 'flip', align: 'none', fallbackAxisSide: 'none' };
            case 'flip-shift': return { side: 'flip', align: 'shift', fallbackAxisSide: 'none' };
            default: return { side: 'flip', align: 'flip', fallbackAxisSide: 'end' };
        }
    }
    return {
        side: collisionAvoidance.side || 'flip',
        align: collisionAvoidance.align || 'flip',
        fallbackAxisSide: collisionAvoidance.fallbackAxisSide || 'end'
    };
}

async function updatePositionInternal(positionerState) {
    const {
        positionerElement,
        triggerElement,
        side,
        align,
        sideOffset,
        alignOffset,
        collisionPadding,
        arrowElement,
        sticky,
        positionMethod,
        collisionAvoidance
    } = positionerState;

    if (!positionerElement || !triggerElement) return;

    // Skip positioning when the element is inside a display:none container (e.g. portal
    // still has display:none during first render). autoUpdate's ResizeObserver will
    // trigger a recomputation once the element becomes measurable.
    if (positionerElement.getClientRects().length === 0) return;

    try {
        const FloatingUI = await ensureFloatingUI();

        // Ensure the element's CSS position matches the strategy BEFORE computePosition
        // reads offsetParent. The FOUC CSS rule applies position:fixed !important which
        // causes offsetParent to be null, breaking absolute strategy calculations.
        // Using setProperty with 'important' overrides the stylesheet !important rule.
        const strategy = positionMethod === 'absolute' ? 'absolute' : 'fixed';

        const placement = toFloatingPlacement(side, align);
        const ca = normalizeCollisionAvoidance(collisionAvoidance);

        // Build middleware array
        const middleware = [];

        // Offset middleware
        if (positionerState.hasSideOffsetFn || positionerState.hasAlignOffsetFn) {
            const dotNetRef = positionerState.dotNetRef;
            middleware.push(FloatingUI.offset(async ({ rects, placement }) => {
                const parts = placement.split('-');
                const data = {
                    anchorWidth: rects.reference.width,
                    anchorHeight: rects.reference.height,
                    popupWidth: rects.floating.width,
                    popupHeight: rects.floating.height,
                    side: parts[0],
                    align: parts[1] || 'center'
                };
                const mainAxis = positionerState.hasSideOffsetFn
                    ? await dotNetRef.invokeMethodAsync('ComputeSideOffset', data)
                    : sideOffset;
                const crossAxis = positionerState.hasAlignOffsetFn
                    ? await dotNetRef.invokeMethodAsync('ComputeAlignOffset', data)
                    : alignOffset;
                // Store computed offset for transform-origin calculation
                positionerState._computedSideOffset = mainAxis;
                return { mainAxis, crossAxis };
            }));
        } else if (sideOffset !== 0 || alignOffset !== 0) {
            middleware.push(FloatingUI.offset({
                mainAxis: sideOffset,
                crossAxis: alignOffset
            }));
        }

        // Normalize collision padding into per-side object and add iOS bias
        const rawPadding = typeof collisionPadding === 'object' && collisionPadding !== null
            ? collisionPadding
            : { top: collisionPadding || 0, right: collisionPadding || 0, bottom: collisionPadding || 0, left: collisionPadding || 0 };
        const bias = 1;
        const biasedPadding = {
            top: (rawPadding.top || 0) + (side === 'bottom' ? bias : 0),
            bottom: (rawPadding.bottom || 0) + (side === 'top' ? bias : 0),
            left: (rawPadding.left || 0) + (side === 'right' ? bias : 0),
            right: (rawPadding.right || 0) + (side === 'left' ? bias : 0)
        };

        // Flip middleware (handles side collision)
        const flipMiddleware = ca.side === 'none' ? null : FloatingUI.flip({
            padding: biasedPadding,
            mainAxis: ca.side === 'flip',
            crossAxis: ca.align === 'flip' ? 'alignment' : false,
            fallbackAxisSideDirection: ca.fallbackAxisSide
        });

        // Shift middleware (handles alignment collision)
        const shiftDisabled = ca.align === 'none' && ca.side !== 'shift';
        const crossAxisShiftEnabled = !shiftDisabled && (sticky || ca.side === 'shift');
        const shiftCrossAxis = positionerState.shiftCrossAxis || false;

        const shiftMiddleware = shiftDisabled ? null : FloatingUI.shift({
            padding: biasedPadding,
            mainAxis: ca.align !== 'none',
            crossAxis: crossAxisShiftEnabled,
            limiter: (sticky || shiftCrossAxis) ? undefined : FloatingUI.limitShift(arrowElement ? (limitData) => {
                const { width, height } = arrowElement.getBoundingClientRect();
                const placementSide = limitData.placement.split('-')[0];
                const sideAxis = (placementSide === 'top' || placementSide === 'bottom') ? 'y' : 'x';
                const arrowSize = sideAxis === 'y' ? width : height;
                const offsetAmount = sideAxis === 'y'
                    ? biasedPadding.left + biasedPadding.right
                    : biasedPadding.top + biasedPadding.bottom;
                return { offset: arrowSize / 2 + offsetAmount / 2 };
            } : undefined)
        });

        // Order matters: shift before flip when side/align is 'shift' or align is 'center'
        if (ca.side === 'shift' || ca.align === 'shift' || align === 'center') {
            if (shiftMiddleware) middleware.push(shiftMiddleware);
            if (flipMiddleware) middleware.push(flipMiddleware);
        } else {
            if (flipMiddleware) middleware.push(flipMiddleware);
            if (shiftMiddleware) middleware.push(shiftMiddleware);
        }

        // Size middleware for available space
        middleware.push(FloatingUI.size({
            padding: collisionPadding,
            apply({ availableWidth, availableHeight, rects }) {
                positionerElement.style.setProperty('--available-width', `${availableWidth}px`);
                positionerElement.style.setProperty('--available-height', `${availableHeight}px`);

                // Snap anchor dimensions to device pixels to ensure the popup's visual
                // width matches the anchor's, avoiding sub-pixel misalignment.
                const dpr = window.devicePixelRatio || 1;
                const ref = rects.reference;
                const anchorWidth = (Math.round((ref.x + ref.width) * dpr) - Math.round(ref.x * dpr)) / dpr;
                const anchorHeight = (Math.round((ref.y + ref.height) * dpr) - Math.round(ref.y * dpr)) / dpr;
                positionerElement.style.setProperty('--anchor-width', `${anchorWidth}px`);
                positionerElement.style.setProperty('--anchor-height', `${anchorHeight}px`);
            }
        }));

        // Hide middleware for anchor detection
        middleware.push(FloatingUI.hide({
            strategy: 'referenceHidden'
        }));

        // Arrow middleware
        if (arrowElement) {
            middleware.push(FloatingUI.arrow({
                element: arrowElement,
                padding: positionerState.arrowPadding || 0
            }));
        }

        positionerElement.style.setProperty('position', strategy, 'important');

        const { x, y, placement: finalPlacement, middlewareData } = await FloatingUI.computePosition(
            triggerElement,
            positionerElement,
            {
                placement,
                strategy,
                middleware
            }
        );

        // Parse the final placement
        const { side: effectiveSide, align: effectiveAlign } = fromFloatingPlacement(finalPlacement);

        // When `alignItemWithTriggerActive` is set on this positioner (Select's
        // align-item-with-trigger mode), skip writing visual placement styles and
        // skip setting `data-positioned`. Mirrors React's
        // `alignItemWithTriggerActive ? FIXED : positioning.positionerStyles`
        // in SelectPositioner.tsx — the popup-level align-item pass owns those
        // writes and is responsible for marking `data-positioned` once it has
        // committed its placement. CSS variables, `data-side`/`data-align`, and
        // anchor-hidden state below still apply because the align-item path
        // also reads/relies on them.
        if (!positionerState.alignItemWithTriggerActive) {
            // Apply position styles (switch from !important to normal inline now that
            // data-positioned will be set, removing the FOUC CSS override)
            positionerElement.style.setProperty('position', strategy);

            // Adaptive origin: switch CSS positioning properties when viewport transitions are active
            let sideX = 'left';
            let sideY = 'top';

            if (positionerState.hasViewport) {
                const computedStyle = getComputedStyle(positionerElement);
                const hasTransition = computedStyle.transitionDuration !== '0s'
                    && computedStyle.transitionDuration !== '';

                if (hasTransition) {
                    if (effectiveSide === 'left') {
                        sideX = 'right';
                    }
                    if (effectiveSide === 'top') {
                        sideY = 'bottom';
                    }
                }
            }

            // Clear opposite positioning properties when adaptive origin may switch sides
            if (positionerState.hasViewport) {
                positionerElement.style.left = '';
                positionerElement.style.right = '';
                positionerElement.style.top = '';
                positionerElement.style.bottom = '';
            }

            if (sideX === 'right') {
                const viewportWidth = strategy === 'fixed'
                    ? (window.visualViewport?.width ?? window.innerWidth)
                    : positionerElement.offsetParent?.clientWidth ?? document.documentElement.clientWidth;
                positionerElement.style.right = `${viewportWidth - x - positionerElement.offsetWidth}px`;
            } else {
                positionerElement.style.left = `${x}px`;
            }

            if (sideY === 'bottom') {
                const viewportHeight = strategy === 'fixed'
                    ? (window.visualViewport?.height ?? window.innerHeight)
                    : positionerElement.offsetParent?.clientHeight ?? document.documentElement.clientHeight;
                positionerElement.style.bottom = `${viewportHeight - y - positionerElement.offsetHeight}px`;
            } else {
                positionerElement.style.top = `${y}px`;
            }

            positionerElement.style.zIndex = '1000';
            positionerElement.style.visibility = '';
            positionerElement.setAttribute('data-positioned', '');
        }

        // Set data attributes
        positionerElement.setAttribute('data-side', effectiveSide);
        positionerElement.setAttribute('data-align', effectiveAlign);

        // Handle anchor hidden state
        if (middlewareData.hide?.referenceHidden) {
            positionerElement.setAttribute('data-anchor-hidden', '');
        } else {
            positionerElement.removeAttribute('data-anchor-hidden');
        }

        // Update arrow position first (data needed for transform-origin)
        // Floating UI's arrow middleware returns:
        // - x (horizontal offset) ONLY for vertical placements (top/bottom)
        // - y (vertical offset) ONLY for horizontal placements (left/right)
        // The perpendicular axis positioning (how far arrow extends from popup edge)
        // is handled by CSS using the data-side attribute, matching Base UI's approach.
        let arrowUncentered = false;
        const arrowData = middlewareData.arrow;
        if (arrowElement && arrowData) {
            const { x: arrowX, y: arrowY } = arrowData;
            arrowUncentered = arrowData.centerOffset !== 0;

            // Set data-side attribute for CSS-based perpendicular positioning
            arrowElement.setAttribute('data-side', effectiveSide);

            // Set data-uncentered when arrow cannot be centered on the anchor
            if (arrowUncentered) {
                arrowElement.setAttribute('data-uncentered', '');
            } else {
                arrowElement.removeAttribute('data-uncentered');
            }

            // Set position (matching Base UI's arrowStyles)
            arrowElement.style.position = 'absolute';

            // Set along-edge coordinate and clear perpendicular axis for CSS to handle
            // For top/bottom placements: set left (centering), clear top/bottom for CSS
            // For left/right placements: set top (centering), clear left/right for CSS
            if (effectiveSide === 'top' || effectiveSide === 'bottom') {
                arrowElement.style.left = arrowX != null ? `${arrowX}px` : '';
                arrowElement.style.right = '';
                arrowElement.style.top = '';
                arrowElement.style.bottom = '';
            } else {
                arrowElement.style.top = arrowY != null ? `${arrowY}px` : '';
                arrowElement.style.bottom = '';
                arrowElement.style.left = '';
                arrowElement.style.right = '';
            }
        }

        // Calculate pixel-precise transform origin using arrow position
        const arrowXVal = arrowData?.x || 0;
        const arrowYVal = arrowData?.y || 0;
        const arrowWidth = arrowElement?.clientWidth || 0;
        const arrowHeight = arrowElement?.clientHeight || 0;
        const transformX = arrowXVal + arrowWidth / 2;
        const transformY = arrowYVal + arrowHeight / 2;
        const shiftY = Math.abs(middlewareData.shift?.y || 0);
        const triggerRect = triggerElement.getBoundingClientRect();
        const halfAnchorHeight = triggerRect.height / 2;
        const currentSideOffset = positionerState._computedSideOffset ?? positionerState.sideOffset ?? 0;
        const isOverlappingAnchor = shiftY > currentSideOffset;

        const adjacentOrigins = {
            top: `${transformX}px calc(100% + ${currentSideOffset}px)`,
            bottom: `${transformX}px ${-currentSideOffset}px`,
            left: `calc(100% + ${currentSideOffset}px) ${transformY}px`,
            right: `${-currentSideOffset}px ${transformY}px`,
        };

        const sideAxis = (effectiveSide === 'top' || effectiveSide === 'bottom') ? 'y' : 'x';
        const overlapOrigin = `${transformX}px ${triggerRect.y + halfAnchorHeight - y}px`;

        const transformOrigin = (crossAxisShiftEnabled && sideAxis === 'y' && isOverlappingAnchor)
            ? overlapOrigin
            : adjacentOrigins[effectiveSide];

        positionerElement.style.setProperty('--transform-origin', transformOrigin);

        // Store computed placement for safePolygon
        positionerState.computedSide = effectiveSide;
        positionerState.computedAlign = effectiveAlign;

        // Notify C# of computed position values
        if (positionerState.onPositionUpdated) {
            positionerState.onPositionUpdated(effectiveSide, effectiveAlign, !!middlewareData.hide?.referenceHidden, arrowUncentered);
        }

    } catch {
        // Fallback to basic positioning if Floating UI fails
        updatePositionFallback(positionerState);
    }
}

/**
 * Fallback positioning when Floating UI is not available.
 */
function updatePositionFallback(positionerState) {
    const {
        positionerElement,
        triggerElement,
        side,
        align,
        sideOffset,
        positionMethod
    } = positionerState;

    if (!positionerElement || !triggerElement) return;

    const isRtl = positionerState.direction === 'rtl';
    const triggerRect = triggerElement.getBoundingClientRect();
    const popupWidth = positionerElement.offsetWidth || 200;
    const popupHeight = positionerElement.offsetHeight || 100;

    let top = 0;
    let left = 0;

    switch (side) {
        case 'top':
            top = triggerRect.top - popupHeight - sideOffset;
            break;
        case 'bottom':
            top = triggerRect.bottom + sideOffset;
            break;
        case 'left':
            left = triggerRect.left - popupWidth - sideOffset;
            break;
        case 'right':
            left = triggerRect.right + sideOffset;
            break;
    }

    if (side === 'top' || side === 'bottom') {
        // `align` is logical: in RTL, 'start' anchors to the trigger's right edge
        // and 'end' anchors to the left edge. Mirror React's `useAnchorPositioning`.
        const physicalAlign = isRtl
            ? (align === 'start' ? 'end' : align === 'end' ? 'start' : 'center')
            : align;
        switch (physicalAlign) {
            case 'start':
                left = triggerRect.left;
                break;
            case 'center':
                left = triggerRect.left + (triggerRect.width - popupWidth) / 2;
                break;
            case 'end':
                left = triggerRect.right - popupWidth;
                break;
        }
    } else {
        switch (align) {
            case 'start':
                top = triggerRect.top;
                break;
            case 'center':
                top = triggerRect.top + (triggerRect.height - popupHeight) / 2;
                break;
            case 'end':
                top = triggerRect.bottom - popupHeight;
                break;
        }
    }

    if (positionMethod === 'absolute') {
        top += window.scrollY;
        left += window.scrollX;
    }

    positionerElement.style.position = positionMethod === 'absolute' ? 'absolute' : 'fixed';
    positionerElement.style.top = `${top}px`;
    positionerElement.style.left = `${left}px`;
    positionerElement.style.zIndex = '1000';
    positionerElement.setAttribute('data-positioned', '');

    positionerElement.setAttribute('data-side', side);
    positionerElement.setAttribute('data-align', align);

    positionerState.computedSide = side;
    positionerState.computedAlign = align;
}

// ============================================================================
// Transition Utilities
// ============================================================================

export function parseCssDuration(durationStr) {
    if (!durationStr || durationStr === 'none') return 0;
    const durations = durationStr.split(',').map(d => d.trim());
    let maxMs = 0;
    for (const duration of durations) {
        let ms = 0;
        if (duration.endsWith('ms')) {
            ms = parseFloat(duration);
        } else if (duration.endsWith('s')) {
            ms = parseFloat(duration) * 1000;
        }
        if (!isNaN(ms) && ms > maxMs) {
            maxMs = ms;
        }
    }
    return maxMs;
}

export function checkForTransitionOrAnimation(element) {
    const style = getComputedStyle(element);
    const transitionDuration = parseCssDuration(style.transitionDuration);
    const hasTransition = transitionDuration > 0;
    const animationName = style.animationName;
    const animationDuration = parseCssDuration(style.animationDuration);
    const hasAnimation = animationName && animationName !== 'none' && animationDuration > 0;
    return hasTransition || hasAnimation;
}

export function getMaxTransitionDuration(element) {
    const style = getComputedStyle(element);
    const transitionDuration = parseCssDuration(style.transitionDuration);
    const transitionDelay = parseCssDuration(style.transitionDelay);
    const totalTransition = transitionDuration + transitionDelay;
    const animationDuration = parseCssDuration(style.animationDuration);
    const animationDelay = parseCssDuration(style.animationDelay);
    const totalAnimation = animationDuration + animationDelay;
    const maxDuration = Math.max(totalTransition, totalAnimation);
    const withBuffer = maxDuration + 50;
    const minTimeout = 100;
    const maxTimeout = 10000;
    return Math.max(minTimeout, Math.min(withBuffer, maxTimeout));
}

// ============================================================================
// Shared Transition Helpers
// ============================================================================

/**
 * Cleans up any pending transition/animation listeners and fallback timeouts
 * on a rootState object.
 * @param {Object} rootState - Must have transitionCleanup and fallbackTimeoutId properties
 */
export function cleanupTransitionState(rootState) {
    if (rootState.transitionCleanup) {
        rootState.transitionCleanup();
        rootState.transitionCleanup = null;
    }
    if (rootState.fallbackTimeoutId) {
        clearTimeout(rootState.fallbackTimeoutId);
        rootState.fallbackTimeoutId = null;
    }
}

/**
 * Sets up transitionend/animationend listeners on the popup element with a
 * fallback timeout. Calls rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen)
 * when the transition completes.
 * @param {Object} rootState - Must have popupElement, dotNetRef, transitionCleanup, fallbackTimeoutId
 * @param {boolean} isOpen - Whether the transition is for opening or closing
 */
export function setupTransitionEndListener(rootState, isOpen) {
    const popupElement = rootState.popupElement;
    if (!popupElement) return;

    cleanupTransitionState(rootState);

    let called = false;
    const handleEnd = (event) => {
        if (event.target !== popupElement) return;
        if (called) return;
        called = true;
        cleanup();
        if (rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
        }
    };

    const cleanup = () => {
        popupElement.removeEventListener('transitionend', handleEnd);
        popupElement.removeEventListener('animationend', handleEnd);
        if (rootState.fallbackTimeoutId) {
            clearTimeout(rootState.fallbackTimeoutId);
            rootState.fallbackTimeoutId = null;
        }
        rootState.transitionCleanup = null;
    };

    popupElement.addEventListener('transitionend', handleEnd);
    popupElement.addEventListener('animationend', handleEnd);

    rootState.transitionCleanup = cleanup;

    const fallbackTimeout = getMaxTransitionDuration(popupElement);
    rootState.fallbackTimeoutId = setTimeout(() => {
        if (!called && rootState.dotNetRef) {
            called = true;
            cleanup();
            rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
        }
    }, fallbackTimeout);
}

/**
 * Waits for a popup element to appear on rootState, then starts the transition.
 * Updates hover interaction with the popup element when found.
 * @param {Object} rootState - Must have popupElement, pendingOpen, dotNetRef, hoverInteraction
 * @param {boolean} isOpen - Whether transitioning to open or closed
 * @param {Function} startTransitionFn - The startTransition function to call once popup is found
 */
export function waitForPopupAndStartTransition(rootState, isOpen, startTransitionFn) {
    const popupElement = rootState.popupElement;

    if (popupElement) {
        startTransitionFn(rootState, isOpen);
        return;
    }

    let attempts = 0;
    const maxAttempts = 10;

    function checkForPopup() {
        attempts++;
        const element = rootState.popupElement;

        if (element) {
            // Update hover interaction with the new popup element
            if (rootState.hoverInteraction) {
                rootState.hoverInteraction.setFloatingElement(element);
            }
            if (rootState.pendingOpen === isOpen) {
                startTransitionFn(rootState, isOpen);
            }
        } else if (attempts < maxAttempts && rootState.pendingOpen === isOpen) {
            requestAnimationFrame(checkForPopup);
        } else if (rootState.dotNetRef && rootState.pendingOpen === isOpen) {
            rootState.dotNetRef.invokeMethodAsync('OnStartingStyleApplied').catch(() => { });
        }
    }

    requestAnimationFrame(checkForPopup);
}

/**
 * Starts a simple open/close transition for tooltip/preview-card style components.
 * Uses double-RAF for open transitions to ensure starting styles are applied.
 * @param {Object} rootState - Must have popupElement, pendingOpen, dotNetRef
 * @param {boolean} isOpen - Whether transitioning to open or closed
 */
export async function startSimpleTransition(rootState, isOpen) {
    const popupElement = rootState.popupElement;

    if (!popupElement) {
        if (rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
        }
        return;
    }

    const hasTransition = checkForTransitionOrAnimation(popupElement);

    if (isOpen) {
        await requestDoubleAnimationFrame();

        if (rootState.pendingOpen !== isOpen) {
            return;
        }
        if (hasTransition) {
            setupTransitionEndListener(rootState, isOpen);
        }
        if (rootState.dotNetRef) {
            rootState.dotNetRef.invokeMethodAsync('OnStartingStyleApplied').catch(() => { });
        }
    } else {
        if (hasTransition) {
            setupTransitionEndListener(rootState, isOpen);
        } else {
            if (rootState.dotNetRef) {
                rootState.dotNetRef.invokeMethodAsync('OnTransitionEnd', isOpen).catch(() => { });
            }
        }
    }
}

/**
 * Creates a keydown handler that closes the first open root on Escape.
 * @param {Map} roots - The component's roots Map
 * @param {string} methodName - The .NET method to invoke (e.g. 'OnEscapeKey')
 * @returns {Function} The keydown event handler
 */
export function createEscapeKeyHandler(roots, methodName) {
    return function handleKeyDown(e) {
        if (e.key !== 'Escape') return;

        for (const [id, rootState] of roots) {
            if (rootState.isOpen && rootState.dotNetRef) {
                rootState.dotNetRef.invokeMethodAsync(methodName).catch(() => { });
                break;
            }
        }
    };
}

/**
 * Disposes a hover interaction on a root state if present.
 * @param {Map} roots - The component's roots Map
 * @param {string} rootId - The root ID
 */
export function disposeHoverInteractionOnRoot(roots, rootId) {
    const rootState = roots.get(rootId);
    if (rootState?.hoverInteraction) {
        rootState.hoverInteraction.cleanup();
        rootState.hoverInteraction = null;
    }
}

/**
 * Updates the floating element reference on a hover interaction.
 * @param {Map} roots - The component's roots Map
 * @param {string} rootId - The root ID
 */
export function updateHoverInteractionFloatingOnRoot(roots, rootId) {
    const rootState = roots.get(rootId);
    if (rootState?.hoverInteraction && rootState.popupElement) {
        rootState.hoverInteraction.setFloatingElement(rootState.popupElement);
    }
}

/**
 * Sets the open state on a hover interaction.
 * @param {Map} roots - The component's roots Map
 * @param {string} rootId - The root ID
 * @param {boolean} isOpen - Whether the interaction is open
 */
export function setHoverInteractionOpenOnRoot(roots, rootId, isOpen) {
    const rootState = roots.get(rootId);
    if (rootState?.hoverInteraction) {
        rootState.hoverInteraction.setOpen(isOpen);
    }
}

// ============================================================================
// Safe Polygon Algorithm
// ============================================================================

/**
 * Checks if a point is inside a polygon using ray casting algorithm.
 * @param {[number, number]} point - [x, y] coordinates
 * @param {Array<[number, number]>} polygon - Array of [x, y] vertices
 * @returns {boolean}
 */
function isPointInPolygon(point, polygon) {
    const [x, y] = point;
    let isInside = false;
    const length = polygon.length;

    for (let i = 0, j = length - 1; i < length; j = i++) {
        const [xi, yi] = polygon[i] || [0, 0];
        const [xj, yj] = polygon[j] || [0, 0];
        const intersect = yi >= y !== yj >= y && x <= ((xj - xi) * (y - yi)) / (yj - yi) + xi;
        if (intersect) {
            isInside = !isInside;
        }
    }
    return isInside;
}

/**
 * Checks if a point is inside a rectangle.
 * @param {[number, number]} point - [x, y] coordinates
 * @param {Object} rect - Rectangle with x, y, width, height
 * @returns {boolean}
 */
function isPointInRect(point, rect) {
    return (
        point[0] >= rect.x &&
        point[0] <= rect.x + rect.width &&
        point[1] >= rect.y &&
        point[1] <= rect.y + rect.height
    );
}

/**
 * Creates a safe polygon handler for hover interactions.
 * This allows the user to move their cursor from the trigger to the floating element
 * without it closing prematurely.
 *
 * @param {Object} options
 * @param {number} options.buffer - Buffer zone around the polygon (default: 0.5)
 * @param {boolean} options.blockPointerEvents - Whether to block pointer events on other elements
 * @param {boolean} options.requireIntent - Whether to require cursor movement intent
 * @returns {Function} Handler function for mousemove events
 */
export function safePolygon(options = {}) {
    const { buffer = 0.5, blockPointerEvents = false, requireIntent = true } = options;

    const timeout = new Timeout();
    let hasLanded = false;
    let lastX = null;
    let lastY = null;
    let lastCursorTime = typeof performance !== 'undefined' ? performance.now() : 0;

    function getCursorSpeed(x, y) {
        const currentTime = performance.now();
        const elapsedTime = currentTime - lastCursorTime;

        if (lastX === null || lastY === null || elapsedTime === 0) {
            lastX = x;
            lastY = y;
            lastCursorTime = currentTime;
            return null;
        }

        const deltaX = x - lastX;
        const deltaY = y - lastY;
        const distance = Math.sqrt(deltaX * deltaX + deltaY * deltaY);
        const speed = distance / elapsedTime; // px / ms

        lastX = x;
        lastY = y;
        lastCursorTime = currentTime;

        return speed;
    }

    /**
     * @param {Object} context
     * @param {number} context.x - Last cursor X position when leaving reference
     * @param {number} context.y - Last cursor Y position when leaving reference
     * @param {string} context.placement - Current placement (top, bottom, left, right)
     * @param {HTMLElement} context.referenceElement - The trigger element
     * @param {HTMLElement} context.floatingElement - The floating element
     * @param {Function} context.onClose - Callback to close the floating element
     * @param {Object} context.tree - Optional floating tree for nested elements
     * @param {string} context.nodeId - Optional node ID in the tree
     */
    function handleClose(context) {
        const { x, y, placement, referenceElement, floatingElement, onClose, tree, nodeId } = context;

        return function onMouseMove(event) {
            function close() {
                timeout.clear();
                onClose();
            }

            timeout.clear();

            if (!referenceElement || !floatingElement || placement == null || x == null || y == null) {
                return;
            }

            const { clientX, clientY } = event;
            const clientPoint = [clientX, clientY];
            const target = getTarget(event);
            const isLeave = event.type === 'mouseleave';
            const isOverFloating = contains(floatingElement, target);
            const isOverReference = contains(referenceElement, target);
            const refRect = referenceElement.getBoundingClientRect();
            const floatRect = floatingElement.getBoundingClientRect();
            const side = placement.split('-')[0];
            const cursorLeaveFromRight = x > floatRect.right - floatRect.width / 2;
            const cursorLeaveFromBottom = y > floatRect.bottom - floatRect.height / 2;
            const isOverReferenceRect = isPointInRect(clientPoint, {
                x: refRect.left,
                y: refRect.top,
                width: refRect.width,
                height: refRect.height
            });
            const isFloatingWider = floatRect.width > refRect.width;
            const isFloatingTaller = floatRect.height > refRect.height;
            const left = (isFloatingWider ? refRect : floatRect).left;
            const right = (isFloatingWider ? refRect : floatRect).right;
            const top = (isFloatingTaller ? refRect : floatRect).top;
            const bottom = (isFloatingTaller ? refRect : floatRect).bottom;

            if (isOverFloating) {
                hasLanded = true;
                if (!isLeave) {
                    return;
                }
            }

            if (isOverReference) {
                hasLanded = false;
            }

            if (isOverReference && !isLeave) {
                hasLanded = true;
                return;
            }

            // Prevent overlapping floating element from being stuck in an open-close loop
            if (isLeave && event.relatedTarget instanceof Element && contains(floatingElement, event.relatedTarget)) {
                return;
            }

            // If any nested child is open, abort
            if (tree && nodeId) {
                const children = tree.getNodeChildren(nodeId);
                if (children.length > 0) {
                    return;
                }
            }

            // If the pointer is leaving from the opposite side, close immediately
            if (
                (side === 'top' && y >= refRect.bottom - 1) ||
                (side === 'bottom' && y <= refRect.top + 1) ||
                (side === 'left' && x >= refRect.right - 1) ||
                (side === 'right' && x <= refRect.left + 1)
            ) {
                return close();
            }

            // Build rectangular trough polygon between trigger and floating element
            let rectPoly = [];
            switch (side) {
                case 'top':
                    rectPoly = [
                        [left, refRect.top + 1],
                        [left, floatRect.bottom - 1],
                        [right, floatRect.bottom - 1],
                        [right, refRect.top + 1]
                    ];
                    break;
                case 'bottom':
                    rectPoly = [
                        [left, floatRect.top + 1],
                        [left, refRect.bottom - 1],
                        [right, refRect.bottom - 1],
                        [right, floatRect.top + 1]
                    ];
                    break;
                case 'left':
                    rectPoly = [
                        [floatRect.right - 1, bottom],
                        [floatRect.right - 1, top],
                        [refRect.left + 1, top],
                        [refRect.left + 1, bottom]
                    ];
                    break;
                case 'right':
                    rectPoly = [
                        [refRect.right - 1, bottom],
                        [refRect.right - 1, top],
                        [floatRect.left + 1, top],
                        [floatRect.left + 1, bottom]
                    ];
                    break;
            }

            // Build the safe polygon from cursor position to floating element
            function getPolygon([px, py]) {
                switch (side) {
                    case 'top': {
                        const cursorPointOne = [
                            isFloatingWider ? px + buffer / 2 : cursorLeaveFromRight ? px + buffer * 4 : px - buffer * 4,
                            py + buffer + 1
                        ];
                        const cursorPointTwo = [
                            isFloatingWider ? px - buffer / 2 : cursorLeaveFromRight ? px + buffer * 4 : px - buffer * 4,
                            py + buffer + 1
                        ];
                        const commonPoints = [
                            [floatRect.left, cursorLeaveFromRight ? floatRect.bottom - buffer : isFloatingWider ? floatRect.bottom - buffer : floatRect.top],
                            [floatRect.right, cursorLeaveFromRight ? (isFloatingWider ? floatRect.bottom - buffer : floatRect.top) : floatRect.bottom - buffer]
                        ];
                        return [cursorPointOne, cursorPointTwo, ...commonPoints];
                    }
                    case 'bottom': {
                        const cursorPointOne = [
                            isFloatingWider ? px + buffer / 2 : cursorLeaveFromRight ? px + buffer * 4 : px - buffer * 4,
                            py - buffer
                        ];
                        const cursorPointTwo = [
                            isFloatingWider ? px - buffer / 2 : cursorLeaveFromRight ? px + buffer * 4 : px - buffer * 4,
                            py - buffer
                        ];
                        const commonPoints = [
                            [floatRect.left, cursorLeaveFromRight ? floatRect.top + buffer : isFloatingWider ? floatRect.top + buffer : floatRect.bottom],
                            [floatRect.right, cursorLeaveFromRight ? (isFloatingWider ? floatRect.top + buffer : floatRect.bottom) : floatRect.top + buffer]
                        ];
                        return [cursorPointOne, cursorPointTwo, ...commonPoints];
                    }
                    case 'left': {
                        const cursorPointOne = [
                            px + buffer + 1,
                            isFloatingTaller ? py + buffer / 2 : cursorLeaveFromBottom ? py + buffer * 4 : py - buffer * 4
                        ];
                        const cursorPointTwo = [
                            px + buffer + 1,
                            isFloatingTaller ? py - buffer / 2 : cursorLeaveFromBottom ? py + buffer * 4 : py - buffer * 4
                        ];
                        const commonPoints = [
                            [cursorLeaveFromBottom ? floatRect.right - buffer : isFloatingTaller ? floatRect.right - buffer : floatRect.left, floatRect.top],
                            [cursorLeaveFromBottom ? (isFloatingTaller ? floatRect.right - buffer : floatRect.left) : floatRect.right - buffer, floatRect.bottom]
                        ];
                        return [...commonPoints, cursorPointOne, cursorPointTwo];
                    }
                    case 'right': {
                        const cursorPointOne = [
                            px - buffer,
                            isFloatingTaller ? py + buffer / 2 : cursorLeaveFromBottom ? py + buffer * 4 : py - buffer * 4
                        ];
                        const cursorPointTwo = [
                            px - buffer,
                            isFloatingTaller ? py - buffer / 2 : cursorLeaveFromBottom ? py + buffer * 4 : py - buffer * 4
                        ];
                        const commonPoints = [
                            [cursorLeaveFromBottom ? floatRect.left + buffer : isFloatingTaller ? floatRect.left + buffer : floatRect.right, floatRect.top],
                            [cursorLeaveFromBottom ? (isFloatingTaller ? floatRect.left + buffer : floatRect.right) : floatRect.left + buffer, floatRect.bottom]
                        ];
                        return [cursorPointOne, cursorPointTwo, ...commonPoints];
                    }
                    default:
                        return [];
                }
            }

            // Check if cursor is in the rectangular trough
            if (isPointInPolygon([clientX, clientY], rectPoly)) {
                return;
            }

            // If already landed on floating and now outside reference rect, close
            if (hasLanded && !isOverReferenceRect) {
                return close();
            }

            // Check cursor intent (speed threshold)
            if (!isLeave && requireIntent) {
                const cursorSpeed = getCursorSpeed(event.clientX, event.clientY);
                const cursorSpeedThreshold = 0.1;
                if (cursorSpeed !== null && cursorSpeed < cursorSpeedThreshold) {
                    return close();
                }
            }

            // Check if cursor is in the safe polygon
            if (!isPointInPolygon([clientX, clientY], getPolygon([x, y]))) {
                close();
            } else if (!hasLanded && requireIntent) {
                // Give a small delay before closing if intent is required
                timeout.start(40, close);
            }
        };
    }

    handleClose.__options = { blockPointerEvents };
    return handleClose;
}

// ============================================================================
// Hover Interaction Module
// ============================================================================

/**
 * Creates a hover interaction handler for floating elements.
 *
 * @param {Object} options
 * @param {string} options.interactionId - Unique ID for this interaction
 * @param {HTMLElement} options.triggerElement - The trigger element
 * @param {HTMLElement} options.floatingElement - The floating element (can be null initially)
 * @param {Function} options.onOpen - Callback when opening
 * @param {Function} options.onClose - Callback when closing
 * @param {number} options.openDelay - Delay before opening (ms)
 * @param {number} options.closeDelay - Delay before closing (ms)
 * @param {boolean} options.mouseOnly - Only respond to mouse events
 * @param {boolean} options.useSafePolygon - Whether to use safe polygon
 * @param {Object} options.safePolygonOptions - Options for safe polygon
 * @param {string|null} options.treeId - Optional floating tree ID for child close coordination
 * @param {string|null} options.nodeId - Optional floating node ID for child close coordination
 * @returns {Object} Interaction controller with cleanup method
 */

const INTERACTIVE_SELECTOR = 'button,a[href],[role="button"],select,[tabindex]:not([tabindex="-1"]),input:not([type="hidden"]):not([disabled]),[contenteditable]:not([contenteditable="false"]),textarea:not([disabled])';

function isInteractiveElement(element) {
    return element?.closest(INTERACTIVE_SELECTOR) != null;
}

export function createHoverInteraction(options) {
    const {
        interactionId,
        triggerElement,
        onOpen,
        onClose,
        openDelay = 0,
        closeDelay = 0,
        mouseOnly = false,
        useSafePolygon = true,
        safePolygonOptions = {},
        treeId = null,
        nodeId = null
    } = options;

    let floatingElement = options.floatingElement;
    let pointerType = null;
    let isOpen = false;
    let openTimeout = new Timeout();
    let closeTimeout = new Timeout();
    let lastCursorX = 0;
    let lastCursorY = 0;
    let mouseMoveHandler = null;
    let safePolygonHandler = useSafePolygon ? safePolygon(safePolygonOptions) : null;
    let placement = 'bottom';
    let interactedInside = false;
    let childCloseCleanup = null;
    let currentOpenDelay = openDelay;
    let currentCloseDelay = closeDelay;

    function cleanupChildCloseListener() {
        if (childCloseCleanup) {
            childCloseCleanup();
            childCloseCleanup = null;
        }
    }

    function handleMouseEnter(event) {
        if (mouseOnly && pointerType && !isMouseLikePointerType(pointerType)) {
            return;
        }

        closeTimeout.clear();

        if (currentOpenDelay > 0) {
            openTimeout.start(currentOpenDelay, () => {
                if (!isOpen) {
                    isOpen = true;
                    onOpen?.('trigger-hover');
                }
            });
        } else if (!isOpen) {
            isOpen = true;
            onOpen?.('trigger-hover');
        }
    }

    function handleMouseLeave(event) {
        openTimeout.clear();
        lastCursorX = event.clientX;
        lastCursorY = event.clientY;

        // If moving to floating element, don't close
        if (floatingElement && event.relatedTarget && contains(floatingElement, event.relatedTarget)) {
            return;
        }

        // A concrete outside target means the pointer did not move toward the
        // floating element. Close immediately instead of waiting for a later
        // mousemove that may never arrive in automated or low-motion paths.
        if (event.relatedTarget &&
            !contains(triggerElement, event.relatedTarget) &&
            (!floatingElement || !contains(floatingElement, event.relatedTarget))) {
            closeWithDelay();
            return;
        }

        if (safePolygonHandler && floatingElement && isOpen) {
            // Set up safe polygon tracking
            cleanupMouseMove();

            // Get the current placement from the positioner
            const positionerState = Array.from(state.positioners.values())
                .find(p => p.floatingElement === floatingElement || p.positionerElement?.contains(floatingElement));
            if (positionerState) {
                placement = positionerState.computedSide || 'bottom';
            }

            mouseMoveHandler = safePolygonHandler({
                x: lastCursorX,
                y: lastCursorY,
                placement,
                referenceElement: triggerElement,
                floatingElement,
                onClose: () => {
                    cleanupMouseMove();
                    closeWithDelay();
                }
            });

            document.addEventListener('mousemove', mouseMoveHandler);
            closeTimeout.start(Math.max(currentCloseDelay, 100), () => {
                cleanupMouseMove();
                if (isOpen) {
                    isOpen = false;
                    onClose?.('trigger-hover');
                }
            });
        } else {
            closeWithDelay();
        }
    }

    function handleFloatingMouseEnter() {
        closeTimeout.clear();
        cleanupMouseMove();
        cleanupChildCloseListener();
    }

    function handleFloatingMouseLeave(event) {
        // Check if any child floating elements are open in the tree first
        if (treeId && nodeId) {
            const tree = state.trees.get(treeId);
            if (tree) {
                const openChildren = tree.getNodeChildren(nodeId);
                if (openChildren.length > 0) {
                    // Defer close — wait for children to close
                    cleanupChildCloseListener();
                    const onChildClosed = () => {
                        // Defer by one tick to allow mouseenter to fire first — if the
                        // child closed because the mouse moved back into this parent,
                        // the parent should stay open.
                        setTimeout(() => {
                            const remaining = tree.getNodeChildren(nodeId);
                            if (remaining.length === 0) {
                                cleanupChildCloseListener();
                                closeWithDelay();
                            }
                        }, 0);
                    };
                    tree.events.on('node.closed', onChildClosed);
                    childCloseCleanup = () => tree.events.off('node.closed', onChildClosed);
                    return;
                }
            }
        }
        // If moving back to trigger, don't close
        if (event.relatedTarget && contains(triggerElement, event.relatedTarget)) {
            return;
        }
        // If user interacted with an interactive element inside, don't close on hover leave
        if (interactedInside) {
            return;
        }
        closeWithDelay();
    }

    function handleFloatingPointerDown(event) {
        const target = event.target;
        if (!isInteractiveElement(target)) {
            interactedInside = false;
            return;
        }
        interactedInside = target?.closest('[aria-haspopup]') != null;
    }

    function handlePointerDown(event) {
        pointerType = event.pointerType;
    }

    function closeWithDelay() {
        if (currentCloseDelay > 0) {
            closeTimeout.start(currentCloseDelay, () => {
                if (isOpen) {
                    isOpen = false;
                    onClose?.('trigger-hover');
                }
            });
        } else if (isOpen) {
            isOpen = false;
            onClose?.('trigger-hover');
        }
    }

    function cleanupMouseMove() {
        if (mouseMoveHandler) {
            document.removeEventListener('mousemove', mouseMoveHandler);
            mouseMoveHandler = null;
        }
    }

    // Attach event listeners to trigger
    triggerElement.addEventListener('mouseenter', handleMouseEnter);
    triggerElement.addEventListener('mouseleave', handleMouseLeave);
    triggerElement.addEventListener('pointerdown', handlePointerDown);

    const interaction = {
        id: interactionId,

        setFloatingElement(element) {
            // Remove listeners from old element
            if (floatingElement) {
                floatingElement.removeEventListener('mouseenter', handleFloatingMouseEnter);
                floatingElement.removeEventListener('mouseleave', handleFloatingMouseLeave);
                floatingElement.removeEventListener('pointerdown', handleFloatingPointerDown, true);
            }

            floatingElement = element;

            // Attach listeners to new element
            if (floatingElement) {
                floatingElement.addEventListener('mouseenter', handleFloatingMouseEnter);
                floatingElement.addEventListener('mouseleave', handleFloatingMouseLeave);
                floatingElement.addEventListener('pointerdown', handleFloatingPointerDown, true);
            }
        },

        setOpen(open) {
            isOpen = open;
            if (!open) {
                interactedInside = false;
                cleanupChildCloseListener();
                // Notify tree that this node has closed
                if (treeId && nodeId) {
                    const tree = state.trees.get(treeId);
                    if (tree) {
                        tree.events.emit('node.closed', { nodeId });
                    }
                }
            }
        },

        setDelays(open, close) {
            currentOpenDelay = open ?? 0;
            currentCloseDelay = close ?? 0;
        },

        cleanup() {
            openTimeout.clear();
            closeTimeout.clear();
            cleanupMouseMove();
            cleanupChildCloseListener();

            triggerElement.removeEventListener('mouseenter', handleMouseEnter);
            triggerElement.removeEventListener('mouseleave', handleMouseLeave);
            triggerElement.removeEventListener('pointerdown', handlePointerDown);

            if (floatingElement) {
                floatingElement.removeEventListener('mouseenter', handleFloatingMouseEnter);
                floatingElement.removeEventListener('mouseleave', handleFloatingMouseLeave);
                floatingElement.removeEventListener('pointerdown', handleFloatingPointerDown, true);
            }

            state.interactions.delete(interactionId);
        }
    };

    state.interactions.set(interactionId, interaction);
    return interaction;
}

/**
 * Gets an existing hover interaction by ID.
 */
export function getHoverInteraction(interactionId) {
    return state.interactions.get(interactionId);
}

// ============================================================================
// Dismiss Interaction Module
// ============================================================================

/**
 * Creates a dismiss interaction handler for floating elements.
 *
 * @param {Object} options
 * @param {string} options.interactionId - Unique ID for this interaction
 * @param {HTMLElement} options.triggerElement - The trigger element
 * @param {HTMLElement} options.floatingElement - The floating element
 * @param {Function} options.onDismiss - Callback when dismissing
 * @param {boolean} options.escapeKey - Whether to dismiss on Escape key
 * @param {boolean} options.outsidePress - Whether to dismiss on outside press
 * @param {boolean} options.ancestorScroll - Whether to dismiss on ancestor scroll
 * @param {string} [options.treeId] - FloatingTree ID for tree-aware dismiss
 * @param {string} [options.nodeId] - Node ID within the FloatingTree
 * @param {boolean|Object} [options.bubbles] - Bubble prevention configuration
 * @param {string|Object|Function} [options.outsidePressEvent='sloppy'] - Outside press event mode
 * @returns {Object} Interaction controller with cleanup method
 */
export function createDismissInteraction(options) {
    const {
        interactionId,
        triggerElement,
        floatingElement,
        onDismiss,
        escapeKey = true,
        outsidePress = true,
        ancestorScroll = false,
        treeId = null,
        nodeId = null,
        bubbles = undefined,
        outsidePressEvent = 'sloppy'
    } = options;

    const doc = getDocument(floatingElement);
    let isComposing = false;
    let currentPointerType = '';

    // Normalize and store bubble flags
    const normalizedBubbles = normalizeBubblesProp(bubbles);
    if (treeId && nodeId) {
        setNodeDismissBubbles(treeId, nodeId, normalizedBubbles);
    }

    // Check if any open child prevents bubbling
    function shouldPreventBubble(type) {
        if (!treeId || !nodeId) return false;
        const tree = state.trees.get(treeId);
        if (!tree) return false;

        const children = tree.getNodeChildren(nodeId);
        for (const child of children) {
            if (type === 'escape' && child.__escapeKeyBubbles === false) return true;
            if (type === 'outsidePress' && child.__outsidePressBubbles === false) return true;
        }
        return false;
    }

    // Resolve which event type to use for outside press
    function resolveOutsidePressEvent() {
        if (typeof outsidePressEvent === 'function') {
            return resolveEventType(outsidePressEvent());
        }
        return resolveEventType(outsidePressEvent);
    }

    function resolveEventType(mode) {
        if (typeof mode === 'object' && mode !== null) {
            const isTouch = currentPointerType === 'touch';
            return isTouch ? (mode.touch || 'pointerdown') : (mode.mouse || 'pointerdown');
        }
        if (mode === 'sloppy') {
            return currentPointerType === 'touch' ? '__touch_state_machine__' : 'pointerdown';
        }
        return mode || 'mousedown';
    }

    function handleKeyDown(event) {
        if (!escapeKey || event.key !== 'Escape') {
            return;
        }

        // Wait until IME is settled
        if (isComposing) {
            return;
        }

        if (shouldPreventBubble('escape')) {
            return;
        }

        event.preventDefault();

        // Stop propagation if escape doesn't bubble
        if (!normalizedBubbles.escapeKey) {
            event.stopPropagation();
        }

        onDismiss?.('escape-key');
    }

    function handleCompositionStart() {
        isComposing = true;
    }

    function handleCompositionEnd() {
        // Safari fires compositionend before keydown
        setTimeout(() => {
            isComposing = false;
        }, 5);
    }

    function closeOnPressOutside(event) {
        if (shouldPreventBubble('outsidePress')) {
            return;
        }

        const target = getTarget(event);

        // Check if click is inside floating or trigger
        if (isEventTargetWithin(event, floatingElement) || isEventTargetWithin(event, triggerElement)) {
            return;
        }

        // Check if target is in any registered trigger
        if (isTargetInRegisteredTriggers(interactionId, event)) {
            return;
        }

        // Check if target is a third-party element
        if (isThirdPartyElement(target, floatingElement)) {
            return;
        }

        // Check if click is on scrollbar
        if (isHTMLElement(target)) {
            const style = getComputedStyle(target);
            const scrollRe = /auto|scroll/;
            const isScrollableX = scrollRe.test(style.overflowX);
            const isScrollableY = scrollRe.test(style.overflowY);

            const canScrollX = isScrollableX && target.clientWidth > 0 && target.scrollWidth > target.clientWidth;
            const canScrollY = isScrollableY && target.clientHeight > 0 && target.scrollHeight > target.clientHeight;

            const isRTL = style.direction === 'rtl';

            const pressedVerticalScrollbar = canScrollY && (isRTL
                ? event.offsetX <= target.offsetWidth - target.clientWidth
                : event.offsetX > target.clientWidth);

            const pressedHorizontalScrollbar = canScrollX && event.offsetY > target.clientHeight;

            if (pressedVerticalScrollbar || pressedHorizontalScrollbar) {
                return;
            }
        }

        onDismiss?.('outside-press');
    }

    function handleScroll(event) {
        onDismiss?.('ancestor-scroll');
    }

    // Touch dismiss state machine
    const touchState = {
        startTime: 0,
        startX: 0,
        startY: 0,
        dismissOnTouchEnd: false,
        dismissOnMouseDown: true,
        timeout: null
    };

    function handleTouchStart(event) {
        const touch = event.touches[0];
        if (!touch) return;

        touchState.startTime = Date.now();
        touchState.startX = touch.clientX;
        touchState.startY = touch.clientY;
        touchState.dismissOnTouchEnd = false;
        touchState.dismissOnMouseDown = true;

        // 1-second timeout: long press = no dismiss
        if (touchState.timeout) clearTimeout(touchState.timeout);
        touchState.timeout = setTimeout(() => {
            touchState.dismissOnTouchEnd = false;
            touchState.dismissOnMouseDown = false;
            touchState.timeout = null;
        }, 1000);
    }

    function handleTouchMove(event) {
        const touch = event.touches[0];
        if (!touch) return;

        const dx = touch.clientX - touchState.startX;
        const dy = touch.clientY - touchState.startY;
        const distance = Math.sqrt(dx * dx + dy * dy);

        if (distance > 10) {
            // Fast scroll gesture — immediate dismiss
            if (touchState.timeout) {
                clearTimeout(touchState.timeout);
                touchState.timeout = null;
            }
            closeOnPressOutside(event);
            touchState.dismissOnTouchEnd = false;
            touchState.dismissOnMouseDown = false;
            return;
        }

        if (distance > 5) {
            // Scroll gesture — arm for touchend dismiss
            touchState.dismissOnTouchEnd = true;
        }
    }

    function handleTouchEnd(event) {
        if (touchState.timeout) {
            clearTimeout(touchState.timeout);
            touchState.timeout = null;
        }

        if (touchState.dismissOnTouchEnd) {
            closeOnPressOutside(event);
        }

        touchState.dismissOnTouchEnd = false;
    }

    // Track pointer type
    function handlePointerType(event) {
        currentPointerType = event.pointerType || '';
    }

    // Determine which event to listen for outside press
    function handlePointerDown(event) {
        // Skip touch pointer events — deferred to touch state machine
        if (event.pointerType === 'touch') return;
        closeOnPressOutside(event);
    }

    // Attach event listeners
    doc.addEventListener('keydown', handleKeyDown);
    doc.addEventListener('compositionstart', handleCompositionStart);
    doc.addEventListener('compositionend', handleCompositionEnd);
    doc.addEventListener('pointerdown', handlePointerType, true);

    let outsidePressCleanup = null;
    if (outsidePress) {
        const isDynamic = typeof outsidePressEvent === 'function' || typeof outsidePressEvent === 'object';
        const resolvedEvent = resolveOutsidePressEvent();

        if (isDynamic || resolvedEvent === '__touch_state_machine__' || outsidePressEvent === 'sloppy') {
            // Sloppy/dynamic mode: pointerdown for mouse, touch state machine for touch
            doc.addEventListener('pointerdown', handlePointerDown);
            doc.addEventListener('touchstart', handleTouchStart, { capture: true, passive: true });
            doc.addEventListener('touchmove', handleTouchMove, { capture: true, passive: true });
            doc.addEventListener('touchend', handleTouchEnd, { capture: true, passive: true });

            outsidePressCleanup = () => {
                doc.removeEventListener('pointerdown', handlePointerDown);
                doc.removeEventListener('touchstart', handleTouchStart, { capture: true });
                doc.removeEventListener('touchmove', handleTouchMove, { capture: true });
                doc.removeEventListener('touchend', handleTouchEnd, { capture: true });
            };
        } else {
            // Specific event mode (static string)
            doc.addEventListener(resolvedEvent, closeOnPressOutside);
            outsidePressCleanup = () => {
                doc.removeEventListener(resolvedEvent, closeOnPressOutside);
            };
        }
    }

    let scrollCleanup = null;
    if (ancestorScroll && triggerElement) {
        const scrollParents = getScrollParents(triggerElement);
        scrollParents.forEach(parent => {
            parent.addEventListener('scroll', handleScroll, { passive: true });
        });
        scrollCleanup = () => {
            scrollParents.forEach(parent => {
                parent.removeEventListener('scroll', handleScroll);
            });
        };
    }

    const interaction = {
        id: interactionId,

        cleanup() {
            doc.removeEventListener('keydown', handleKeyDown);
            doc.removeEventListener('compositionstart', handleCompositionStart);
            doc.removeEventListener('compositionend', handleCompositionEnd);
            doc.removeEventListener('pointerdown', handlePointerType, true);

            outsidePressCleanup?.();
            scrollCleanup?.();

            // Clean up registered triggers
            triggerMaps.delete(interactionId);

            // Clean up touch timeout
            if (touchState.timeout) clearTimeout(touchState.timeout);

            state.interactions.delete(interactionId);
        }
    };

    state.interactions.set(interactionId, interaction);
    return interaction;
}

// ============================================================================
// Tabbable Utilities (enableFocusInside / disableFocusInside)
// ============================================================================

/**
 * CSS selector matching all natively tabbable HTML elements.
 * Consolidates the duplicated selectors from dialog/popover JS modules.
 */
export const TABBABLE_SELECTOR =
    'a[href], button:not([disabled]), input:not([disabled]):not([type="hidden"]), ' +
    'select:not([disabled]), textarea:not([disabled]), ' +
    '[tabindex]:not([tabindex="-1"]), [contenteditable]:not([contenteditable="false"]), ' +
    'details > summary:first-of-type, audio[controls], video[controls]';

/**
 * Returns all tabbable elements within a container, ordered by DOM position.
 * Excludes elements with negative tabindex unless they have data-tabindex.
 */
export function getTabbableElements(container) {
    if (!container) return [];

    // Collect elements from the container and any shadow roots
    function collectElements(root, result) {
        const elements = root.querySelectorAll(TABBABLE_SELECTOR);
        for (const el of elements) {
            result.push(el);
        }
        // Walk shadow roots for shadow DOM elements
        const allElements = root.querySelectorAll('*');
        for (const el of allElements) {
            if (el.shadowRoot) {
                collectElements(el.shadowRoot, result);
            }
        }
    }

    const elements = [];
    collectElements(container, elements);

    return elements.filter(el => {
        if (el.hasAttribute('data-tabindex')) return true;
        const tabindex = el.getAttribute('tabindex');
        if (tabindex !== null && parseInt(tabindex, 10) < 0) return false;
        if (el.disabled) return false;
        if (el.offsetParent === null && getComputedStyle(el).position !== 'fixed') return false;

        // Skip non-first summary children of details elements
        if (el.tagName === 'SUMMARY') {
            const details = el.closest('details');
            if (details && details.querySelector('summary') !== el) return false;
        }

        return true;
    });
}

/**
 * Disables focus on all tabbable elements inside a container by setting
 * tabindex="-1" and preserving original values in data-tabindex attribute.
 * Idempotent — calling multiple times has no additional effect.
 */
export function disableFocusInside(container) {
    if (!container) return;
    const elements = getTabbableElements(container);
    for (const el of elements) {
        if (el.hasAttribute('data-tabindex')) continue;
        const currentTabindex = el.getAttribute('tabindex');
        el.setAttribute('data-tabindex', currentTabindex ?? '');
        el.setAttribute('tabindex', '-1');
    }
}

/**
 * Re-enables focus on all elements inside a container that were previously
 * disabled by disableFocusInside. Restores original tabindex from data-tabindex.
 */
export function enableFocusInside(container) {
    if (!container) return;
    const elements = container.querySelectorAll('[data-tabindex]');
    for (const el of elements) {
        const originalTabindex = el.getAttribute('data-tabindex');
        el.removeAttribute('data-tabindex');
        if (originalTabindex === '') {
            el.removeAttribute('tabindex');
        } else {
            el.setAttribute('tabindex', originalTabindex);
        }
    }
}

/**
 * Given the currently focused element, returns the next tabbable element
 * within the floating element scope. Wraps around if at the end.
 */
export function getNextTabbable(currentElement, container, scope) {
    const tabbable = getTabbableElements(scope || container);
    if (tabbable.length === 0) return null;
    const index = tabbable.indexOf(currentElement);
    if (index === -1 || index === tabbable.length - 1) return tabbable[0];
    return tabbable[index + 1];
}

/**
 * Given the currently focused element, returns the previous tabbable element
 * within the floating element scope. Wraps around if at the start.
 * @param {Element} currentElement - The currently focused element.
 * @param {Element} container - The floating element container.
 * @param {Element} [scope] - Optional broader scope to search (e.g. document.body for non-modal Tab-out).
 */
export function getPreviousTabbable(currentElement, container, scope) {
    const tabbable = getTabbableElements(scope || container);
    if (tabbable.length === 0) return null;
    const index = tabbable.indexOf(currentElement);
    if (index <= 0) return tabbable[tabbable.length - 1];
    return tabbable[index - 1];
}

// ============================================================================
// Focus Utilities
// ============================================================================

let pendingFocusRafId = 0;

/**
 * Enqueues a focus call on the next animation frame, optionally cancelling
 * any previously enqueued focus to prevent focus race conditions.
 */
function enqueueFocus(el, { preventScroll = false, cancelPrevious = true } = {}) {
    if (cancelPrevious) cancelAnimationFrame(pendingFocusRafId);
    pendingFocusRafId = requestAnimationFrame(() => el?.focus({ preventScroll }));
}

/**
 * Checks whether an element is a typeable form element (input/textarea/contenteditable).
 */
function isTypeableFormElement(el) {
    if (!el) return false;
    const tagName = el.tagName;
    if (tagName === 'TEXTAREA') return true;
    if (tagName === 'INPUT') {
        const type = el.getAttribute('type') || 'text';
        const nonTypeable = ['button', 'checkbox', 'color', 'file', 'hidden', 'image', 'radio', 'range', 'reset', 'submit'];
        return !nonTypeable.includes(type);
    }
    return el.isContentEditable;
}

/**
 * Checks whether an element is a typeable combobox (role="combobox" with a typeable element).
 */
function isTypeableCombobox(el) {
    if (!el) return false;
    return el.getAttribute('role') === 'combobox' && isTypeableFormElement(el);
}

/**
 * Checks whether the event's relatedTarget is outside the given container.
 */
function isOutsideEvent(event, container) {
    const relatedTarget = event.relatedTarget;
    return !relatedTarget || !contains(container, relatedTarget);
}

// ============================================================================
// Focus Guard Elements
// ============================================================================

const focusGuardPairs = new Map();
let focusGuardCounter = 0;

/**
 * Initializes a pair of focus guard elements around a floating element.
 * Creates before/after guard references and attaches focus redirect handlers.
 */
export function initializeFocusGuards(options) {
    const {
        floatingElement,
        triggerElement = null,
        modal = true,
        onClose = null,
        closeOnFocusOut = true,
        nextFocusableElement = null,
        previousFocusableElement = null
    } = options;

    const guardPairId = `guard-${++focusGuardCounter}`;
    const guards = floatingElement.parentElement?.querySelectorAll('[data-blazor-base-ui-focus-guard]');
    if (!guards || guards.length < 2) {
        // Guards not rendered yet; store config for deferred init
        focusGuardPairs.set(guardPairId, {
            floatingElement, triggerElement, modal, onClose,
            beforeGuard: null, afterGuard: null, cleanup: null
        });
        return guardPairId;
    }

    const beforeGuard = guards[0];
    const afterGuard = guards[guards.length - 1];

    function handleBeforeGuardFocus(event) {
        if (modal) {
            // Modal: wrap around to last tabbable element
            const tabbable = getTabbableElements(floatingElement);
            if (tabbable.length > 0) {
                tabbable[tabbable.length - 1].focus();
            } else {
                floatingElement.focus();
            }
        } else {
            // Non-modal: direction-aware behavior
            if (isOutsideEvent(event, floatingElement)) {
                // Focus came from outside (e.g., Shift+Tab from document) — focus previous tabbable in document
                if (previousFocusableElement) {
                    previousFocusableElement.focus();
                } else {
                    const tabbable = getTabbableElements(floatingElement);
                    if (tabbable.length > 0) {
                        tabbable[tabbable.length - 1].focus();
                    } else {
                        floatingElement.focus();
                    }
                }
            } else {
                // Focus came from inside (Shift+Tab from first tabbable) — move to previous focusable
                if (previousFocusableElement) {
                    previousFocusableElement.focus();
                } else {
                    const tabbable = getTabbableElements(floatingElement);
                    if (tabbable.length > 0) {
                        tabbable[tabbable.length - 1].focus();
                    } else {
                        floatingElement.focus();
                    }
                }
            }
        }
    }

    function handleAfterGuardFocus(event) {
        if (modal) {
            // Modal: wrap around to first tabbable element
            const tabbable = getTabbableElements(floatingElement);
            if (tabbable.length > 0) {
                tabbable[0].focus();
            } else {
                floatingElement.focus();
            }
        } else {
            // Non-modal: Tab past last tabbable
            if (nextFocusableElement) {
                nextFocusableElement.focus();
            } else if (closeOnFocusOut) {
                onClose?.();
            }
        }
    }

    beforeGuard.addEventListener('focus', handleBeforeGuardFocus);
    afterGuard.addEventListener('focus', handleAfterGuardFocus);

    const cleanup = () => {
        beforeGuard.removeEventListener('focus', handleBeforeGuardFocus);
        afterGuard.removeEventListener('focus', handleAfterGuardFocus);
    };

    focusGuardPairs.set(guardPairId, {
        floatingElement, triggerElement, modal, onClose,
        beforeGuard, afterGuard, cleanup
    });

    return guardPairId;
}

/**
 * Removes focus guard event listeners and cleans up internal state.
 * Does NOT remove DOM elements (Blazor manages FocusGuard.razor lifecycle).
 */
export function disposeFocusGuards(guardPairId) {
    const pair = focusGuardPairs.get(guardPairId);
    if (!pair) return;
    pair.cleanup?.();
    focusGuardPairs.delete(guardPairId);
}

/**
 * Updates the focus guard mode after initialization.
 */
export function updateFocusGuardMode(guardPairId, modal, onClose) {
    const pair = focusGuardPairs.get(guardPairId);
    if (!pair) return;
    pair.modal = modal;
    pair.onClose = onClose;
    // Re-initialize handlers with updated mode
    pair.cleanup?.();
    if (pair.beforeGuard && pair.afterGuard) {
        const result = initializeFocusGuards({
            floatingElement: pair.floatingElement,
            triggerElement: pair.triggerElement,
            modal,
            onClose
        });
        const newPair = focusGuardPairs.get(result);
        if (newPair) {
            pair.cleanup = newPair.cleanup;
            pair.beforeGuard = newPair.beforeGuard;
            pair.afterGuard = newPair.afterGuard;
            focusGuardPairs.delete(result);
        }
    }
}

/**
 * Checks whether focus is currently inside the guarded scope
 * (between the before and after guard elements, inclusive).
 */
export function isFocusInsideGuardedScope(guardPairId) {
    const pair = focusGuardPairs.get(guardPairId);
    if (!pair || !pair.floatingElement) return false;
    const doc = getDocument(pair.floatingElement);
    const active = activeElement(doc);
    if (!active) return false;
    return contains(pair.floatingElement, active) ||
        active === pair.beforeGuard ||
        active === pair.afterGuard;
}

// ============================================================================
// markOthers (inert/aria-hidden management)
// ============================================================================

const counterMaps = {
    'aria-hidden': new WeakMap(),
    'inert': new WeakMap(),
    'none': new WeakMap()
};
let markerMap = new WeakMap();
let uncontrolledElementsSet = new WeakSet();
let lockCount = 0;

function getCounterMap(control) {
    if (control === 'aria-hidden') return counterMaps['aria-hidden'];
    if (control === 'inert') return counterMaps['inert'];
    return counterMaps['none'];
}

function unwrapHost(node) {
    let current = node;
    while (current?.getRootNode() instanceof ShadowRoot) {
        current = current.getRootNode().host;
    }
    return current;
}

function applyAttributeToOthers(avoidElements, body, ariaHidden, inert) {
    const controlAttribute = inert ? 'inert' : ariaHidden ? 'aria-hidden' : null;
    const counterMap = getCounterMap(controlAttribute ?? 'none');
    const markedElements = [];

    // Add aria-live elements to avoid list so their ancestor chains are preserved
    const liveRegions = body.querySelectorAll('[aria-live]');
    for (const el of liveRegions) {
        if (!avoidElements.includes(el)) {
            avoidElements.push(el);
        }
    }

    // Build set of ancestor elements to skip
    const avoidAncestors = new Set();
    for (const el of avoidElements) {
        let current = unwrapHost(el);
        while (current && current !== body) {
            avoidAncestors.add(current);
            current = current.parentElement;
        }
    }

    // Walk body children recursively
    function walkAndMark(parent) {
        for (const child of parent.children) {
            // Skip the avoid elements and their ancestors
            if (avoidAncestors.has(child) || avoidElements.includes(child)) {
                // If this is an ancestor of an avoid element, walk its children
                // but do NOT recurse into avoid elements themselves (their children should stay interactive)
                if (avoidAncestors.has(child) && !avoidElements.includes(child)) {
                    walkAndMark(child);
                }
                continue;
            }

            // Skip script elements and aria-live regions
            if (child.tagName === 'SCRIPT' || child.hasAttribute('aria-live')) {
                continue;
            }

            // Check if element already has the attribute (uncontrolled)
            if (controlAttribute && child.hasAttribute(controlAttribute) && !markerMap.has(child)) {
                uncontrolledElementsSet.add(child);
                continue;
            }

            // Apply attribute
            const currentCount = counterMap.get(child) || 0;
            if (currentCount === 0) {
                if (controlAttribute) {
                    child.setAttribute(controlAttribute, controlAttribute === 'aria-hidden' ? 'true' : '');
                }
                child.setAttribute('data-blazor-base-ui-inert', '');
            }
            counterMap.set(child, currentCount + 1);

            const markerCount = markerMap.get(child) || 0;
            markerMap.set(child, markerCount + 1);

            markedElements.push(child);
        }
    }

    walkAndMark(body);
    lockCount++;

    return function cleanup() {
        for (const child of markedElements) {
            const currentCount = counterMap.get(child) || 0;
            const newCount = currentCount - 1;

            if (newCount <= 0) {
                counterMap.delete(child);
                if (controlAttribute && !uncontrolledElementsSet.has(child)) {
                    child.removeAttribute(controlAttribute);
                }
            } else {
                counterMap.set(child, newCount);
            }

            const markerCount = markerMap.get(child) || 0;
            const newMarkerCount = markerCount - 1;
            if (newMarkerCount <= 0) {
                markerMap.delete(child);
                child.removeAttribute('data-blazor-base-ui-inert');
            } else {
                markerMap.set(child, newMarkerCount);
            }
        }

        lockCount--;
        if (lockCount === 0) {
            // Reset all maps when no more active callers
            counterMaps['aria-hidden'] = new WeakMap();
            counterMaps['inert'] = new WeakMap();
            counterMaps['none'] = new WeakMap();
            uncontrolledElementsSet = new WeakSet();
            markerMap = new WeakMap();
        }
    };
}

/**
 * Marks all DOM elements outside the specified avoid elements as aria-hidden
 * or inert, creating a modal accessibility barrier.
 */
export function markOthers(avoidElements, ariaHidden = false, inert = false) {
    const body = avoidElements[0]?.ownerDocument?.body ?? document.body;
    if (!body) return () => {};
    return applyAttributeToOthers(avoidElements, body, ariaHidden, inert);
}

/**
 * Checks whether the browser supports the native `inert` attribute.
 */
export function supportsInert() {
    return 'inert' in HTMLElement.prototype;
}

// ============================================================================
// FloatingFocusManager
// ============================================================================

/**
 * Creates a shared focusout handler with enhanced checks (F4).
 * Used by both createFloatingFocusManager and updateFloatingFocusManager.
 * @param {object} mgr - Manager-like object with floatingElement, triggerElement, insideElements, onClose, treeId, nodeId
 * @param {object} interactionCtx - Interaction state with isPointerDown, pointerDownOutside getters, preventReturnFocus setter
 */
function createFocusOutHandler(mgr, interactionCtx) {
    return function handleFocusOut(event) {
        queueMicrotask(() => {
            // Suppress during pointer interactions
            if (interactionCtx?.isPointerDown) return;

            const relatedTarget = event.relatedTarget;

            // If focus moved to a focus guard, don't close
            if (relatedTarget?.hasAttribute('data-blazor-base-ui-focus-guard')) return;

            // Match Floating UI/Base UI: a null relatedTarget commonly means focus
            // temporarily fell back to the document body during DOM updates. Do not
            // dismiss until focus lands on a concrete unrelated element.
            if (!relatedTarget) return;

            // Check if focus stayed inside floating element or trigger
            if (contains(mgr.floatingElement, relatedTarget)) return;
            if (mgr.triggerElement && contains(mgr.triggerElement, relatedTarget)) return;
            for (const el of (mgr.insideElements || [])) {
                if (contains(el, relatedTarget)) return;
            }

            // Tree-aware checks: don't close if focus moved to a child/ancestor floating element
            const mgrTreeId = mgr.treeId;
            const mgrNodeId = mgr.nodeId;
            if (mgrTreeId && mgrNodeId) {
                const tree = state.trees.get(mgrTreeId);
                if (tree) {
                    const children = tree.getNodeChildren(mgrNodeId);
                    for (const child of children) {
                        if (child.context?.floatingElement && contains(child.context.floatingElement, relatedTarget)) {
                            return;
                        }
                        if (child.context?.domReference && contains(child.context.domReference, relatedTarget)) {
                            return;
                        }
                    }
                    const ancestors = tree.getNodeAncestors(mgrNodeId);
                    for (const ancestor of ancestors) {
                        if (ancestor.context?.floatingElement && contains(ancestor.context.floatingElement, relatedTarget)) {
                            return;
                        }
                        if (ancestor.context?.domReference === relatedTarget) {
                            return;
                        }
                    }
                }
            }

            // Restore focus if element was removed from DOM (A1)
            if (mgr.restoreFocus && event.currentTarget !== mgr.triggerElement) {
                const target = event.target;
                const activeEl = document.activeElement;
                if (activeEl === document.body || (target && !target.isConnected)) {
                    if (mgr.restoreFocusMode === 'popup') {
                        mgr.floatingElement.focus();
                        return;
                    }
                    const tabbableContent = getTabbableElements(mgr.floatingElement);
                    const nodeToFocus = tabbableContent[mgr.tabbableIndex] ||
                        tabbableContent[tabbableContent.length - 1] || mgr.floatingElement;
                    if (nodeToFocus && nodeToFocus.focus) nodeToFocus.focus();
                    return;
                }
            }

            if (interactionCtx) interactionCtx.preventReturnFocus = true;
            mgr.onClose?.();
        });
    };
}

const PREVIOUSLY_FOCUSED_LIMIT = 20;
let previouslyFocusedElements = [];

function addPreviouslyFocusedElement(el) {
    previouslyFocusedElements = previouslyFocusedElements.filter(e => e.isConnected);
    if (el && el.tagName !== 'BODY') {
        previouslyFocusedElements.push(el);
        if (previouslyFocusedElements.length > PREVIOUSLY_FOCUSED_LIMIT) {
            previouslyFocusedElements = previouslyFocusedElements.slice(-PREVIOUSLY_FOCUSED_LIMIT);
        }
    }
}

function getPreviouslyFocusedElement() {
    previouslyFocusedElements = previouslyFocusedElements.filter(e => e.isConnected);
    return previouslyFocusedElements[previouslyFocusedElements.length - 1];
}

const focusManagers = new Map();
let focusManagerCounter = 0;

/**
 * Creates a generalized floating focus manager that handles modal/non-modal
 * focus trapping, initial focus, return focus, focus restoration, and
 * aria-modal management.
 */
export function createFloatingFocusManager(options) {
    const {
        floatingElement,
        triggerElement = null,
        modal = true,
        initialFocus = true,
        initialFocusSelector = null,
        returnFocus = true,
        returnFocusElement = null,
        restoreFocus = false,
        restoreFocusMode = null,
        closeOnFocusOut = true,
        interactionType = '',
        dotNetRef = null,
        onClose: onCloseOption = null,
        treeId = null,
        nodeId = null,
        insideElements = [],
        order = null,
        nextFocusableElement = null,
        previousFocusableElement = null,
        portalNode = null,
        beforeOutsideGuard = null,
        afterOutsideGuard = null
    } = options;

    const onClose = onCloseOption ?? (dotNetRef ? () => dotNetRef.invokeMethodAsync('OnClose').catch(() => {}) : null);

    const managerId = `fm-${++focusManagerCounter}`;
    const doc = getDocument(floatingElement);
    addPreviouslyFocusedElement(document.activeElement);
    const previouslyFocusedElement = activeElement(doc);

    let markOthersCleanup = null;
    let focusOutCleanup = null;
    let mutationObserver = null;
    let tabKeydownCleanup = null;
    let focusInCleanup = null;
    let lastFocusedIndex = 0;
    let preventReturnFocus = false;

    // Create hidden fallback span for return focus when trigger disconnects (FFM-F14)
    let returnFocusFallback = null;
    if (triggerElement && triggerElement.isConnected) {
        returnFocusFallback = document.createElement('span');
        returnFocusFallback.setAttribute('aria-hidden', 'true');
        returnFocusFallback.setAttribute('tabindex', '-1');
        returnFocusFallback.style.cssText = 'position:fixed;top:0;left:0;width:0;height:0;overflow:hidden;pointer-events:none;';
        triggerElement.after(returnFocusFallback);
    }

    // Document-level interaction tracking (F11)
    let lastInteractionType = '';
    let isPointerDown = false;
    let pointerDownOutside = false;

    function handleDocPointerDown(event) {
        lastInteractionType = 'pointer';
        const target = event.target;
        // Only set isPointerDown when the event target is within the floating or trigger element
        if (contains(floatingElement, target) || (triggerElement && contains(triggerElement, target))) {
            isPointerDown = true;
        }
        pointerDownOutside = !contains(floatingElement, target) &&
            !(triggerElement && contains(triggerElement, target));

        // FFM-F10: Don't suppress return focus for nested floating elements or virtual clicks
        if (pointerDownOutside) {
            // Check if target is within a child floating element in the tree
            if (treeId && nodeId) {
                const tree = state.trees.get(treeId);
                if (tree) {
                    const children = tree.getNodeChildren(nodeId);
                    for (const child of children) {
                        if (child.context?.floatingElement && contains(child.context.floatingElement, target)) {
                            pointerDownOutside = false;
                            break;
                        }
                    }
                }
            }
            // Check for virtual click (programmatic click with no pointer type)
            if (event.detail === 0 && !event.pointerType) {
                pointerDownOutside = false;
            }
        }
    }
    function handleDocPointerUpOrCancel() {
        isPointerDown = false;
        pointerDownOutside = false;
    }
    function handleDocKeyDown() {
        lastInteractionType = 'keyboard';
    }

    doc.addEventListener('pointerdown', handleDocPointerDown, true);
    doc.addEventListener('pointerup', handleDocPointerUpOrCancel, true);
    doc.addEventListener('pointercancel', handleDocPointerUpOrCancel, true);
    doc.addEventListener('keydown', handleDocKeyDown, true);

    const interactionTrackingCleanup = () => {
        doc.removeEventListener('pointerdown', handleDocPointerDown, true);
        doc.removeEventListener('pointerup', handleDocPointerUpOrCancel, true);
        doc.removeEventListener('pointercancel', handleDocPointerUpOrCancel, true);
        doc.removeEventListener('keydown', handleDocKeyDown, true);
    };

    // Detect untrapped typeable combobox (F5)
    const isUntrappedTypeableCombobox = isTypeableCombobox(triggerElement) && !initialFocus;

    // Apply aria-modal in modal mode
    if (modal) {
        floatingElement.setAttribute('aria-modal', 'true');
    }

    // Mark other elements as inert/aria-hidden in modal or untrapped combobox mode
    if (modal || isUntrappedTypeableCombobox) {
        const markAvoid = [floatingElement, ...(insideElements || [])];
        if (triggerElement) markAvoid.push(triggerElement);
        if (options.beforeGuardElement) markAvoid.push(options.beforeGuardElement);
        if (options.afterGuardElement) markAvoid.push(options.afterGuardElement);
        if (nextFocusableElement) markAvoid.push(nextFocusableElement);
        if (previousFocusableElement) markAvoid.push(previousFocusableElement);
        markOthersCleanup = markOthers(markAvoid, modal || isUntrappedTypeableCombobox);
    }

    // Prevent Tab from escaping empty modal
    if (modal) {
        function handleTabKeyDown(event) {
            if (event.key === 'Tab') {
                const tabbable = getTabbableElements(floatingElement);
                if (tabbable.length === 0) {
                    event.preventDefault();
                }
            }
        }
        floatingElement.addEventListener('keydown', handleTabKeyDown);
        tabKeydownCleanup = () => floatingElement.removeEventListener('keydown', handleTabKeyDown);
    }

    // Handle initial focus
    function setInitialFocus() {
        if (initialFocus === false) return;

        // Touch interaction suppression — don't focus first tabbable on touch
        // to prevent virtual keyboard from appearing
        const isTouchInteraction = interactionType === 'touch';

        if (typeof initialFocusSelector === 'string' && initialFocusSelector) {
            const target = floatingElement.querySelector(initialFocusSelector);
            if (target) {
                target.focus();
                return;
            }
        }

        if (initialFocus instanceof HTMLElement) {
            initialFocus.focus();
            return;
        }

        if (isTouchInteraction) {
            // Focus the container itself to maintain focus context
            // without triggering virtual keyboard
            floatingElement.focus();
            return;
        }

        // Default: focus first tabbable element
        const tabbable = getTabbableElements(floatingElement);
        if (tabbable.length > 0) {
            tabbable[0].focus();
        } else {
            floatingElement.focus();
        }
    }

    // Delay initial focus to next frame to ensure DOM is ready
    requestAnimationFrame(() => {
        setInitialFocus();
    });

    // Setup focus restoration when focused element is removed from DOM
    if (restoreFocus) {
        function handleFocusIn(event) {
            const tabbable = getTabbableElements(floatingElement);
            const idx = tabbable.indexOf(event.target);
            if (idx !== -1) lastFocusedIndex = idx;
        }
        floatingElement.addEventListener('focusin', handleFocusIn);
        focusInCleanup = () => floatingElement.removeEventListener('focusin', handleFocusIn);

        mutationObserver = new MutationObserver(() => {
            const active = activeElement(doc);
            if (!active || active === doc.body) {
                if (restoreFocusMode === 'popup') {
                    floatingElement.focus();
                } else {
                    const tabbable = getTabbableElements(floatingElement);
                    if (tabbable.length > 0) {
                        const targetIndex = Math.min(lastFocusedIndex, tabbable.length - 1);
                        tabbable[targetIndex].focus();
                    } else {
                        floatingElement.focus();
                    }
                }
            }
        });
        mutationObserver.observe(floatingElement, { childList: true, subtree: true });
    }

    // Track tabbable index for restoreFocus (A1)
    let tabbableIndex = -1;
    const tabbableIndexHandler = function(event) {
        const tabbableContent = getTabbableElements(floatingElement);
        const idx = tabbableContent.indexOf(event.target);
        if (idx !== -1) tabbableIndex = idx;
    };
    floatingElement.addEventListener('focusin', tabbableIndexHandler);
    let tabbableIndexCleanup = () => floatingElement.removeEventListener('focusin', tabbableIndexHandler);

    // Store interaction state on a shared object accessible by createFocusOutHandler
    const interactionState = {
        get isPointerDown() { return isPointerDown; },
        get pointerDownOutside() { return pointerDownOutside; },
        set preventReturnFocus(v) { preventReturnFocus = v; }
    };

    // Setup closeOnFocusOut for non-modal
    if (!modal && closeOnFocusOut) {
        const handleFocusOut = createFocusOutHandler(
            { floatingElement, triggerElement, insideElements, onClose, treeId, nodeId,
              restoreFocus, restoreFocusMode, get tabbableIndex() { return tabbableIndex; } },
            interactionState
        );
        floatingElement.addEventListener('focusout', handleFocusOut);

        // Also attach to trigger for Safari button focus loss (F4)
        if (triggerElement) {
            triggerElement.addEventListener('focusout', handleFocusOut);
            function handleTriggerPointerDown(event) {
                // Safari doesn't focus buttons on click — suppress focusout close
                if (isSafari()) {
                    interactionState.preventReturnFocus = false;
                }
            }
            triggerElement.addEventListener('pointerdown', handleTriggerPointerDown);
            focusOutCleanup = () => {
                floatingElement.removeEventListener('focusout', handleFocusOut);
                triggerElement.removeEventListener('focusout', handleFocusOut);
                triggerElement.removeEventListener('pointerdown', handleTriggerPointerDown);
            };
        } else {
            focusOutCleanup = () => floatingElement.removeEventListener('focusout', handleFocusOut);
        }
    }

    // Call handleTabIndex when focus leaves trigger and enters floating element (FFM-F13)
    let triggerFocusOutCleanup = null;
    if (triggerElement) {
        function handleTriggerFocusOut(event) {
            // Use microtask to check relatedTarget after focus settles
            queueMicrotask(() => {
                const newFocus = document.activeElement;
                if (newFocus && contains(floatingElement, newFocus)) {
                    handleTabIndex(floatingElement, order);
                }
            });
        }
        triggerElement.addEventListener('focusout', handleTriggerFocusOut);
        triggerFocusOutCleanup = () => triggerElement.removeEventListener('focusout', handleTriggerFocusOut);
    }

    // Dynamic handleTabIndex (F7/F9)
    function handleTabIndex(floatingEl, orderOption) {
        if (!floatingEl) return;
        if (
            (!orderOption || !orderOption.includes('floating')) &&
            !floatingEl.getAttribute('role')?.includes('dialog')
        ) {
            return;
        }
        const allFocusable = getTabbableElements(floatingEl);
        const tabbableContent = allFocusable.filter(el => {
            const dataTabIndex = el.getAttribute('data-tabindex') || '';
            return el.tabIndex >= 0 ||
                (el.hasAttribute('data-tabindex') && !dataTabIndex.startsWith('-'));
        });
        const tabIndex = floatingEl.getAttribute('tabindex');
        if ((orderOption && orderOption.includes('floating')) || tabbableContent.length === 0) {
            if (tabIndex !== '0') floatingEl.setAttribute('tabindex', '0');
        } else if (
            tabIndex !== '-1' ||
            (floatingEl.hasAttribute('data-tabindex') &&
                floatingEl.getAttribute('data-tabindex') !== '-1')
        ) {
            floatingEl.setAttribute('tabindex', '-1');
            floatingEl.setAttribute('data-tabindex', '-1');
        }
    }

    handleTabIndex(floatingElement, order);

    const manager = {
        id: managerId,
        floatingElement,
        triggerElement,
        modal,
        closeOnFocusOut,
        returnFocus,
        returnFocusElement,
        previouslyFocusedElement,
        insideElements,
        onClose,
        order,
        nextFocusableElement,
        previousFocusableElement,
        markOthersCleanup,
        focusOutCleanup,
        treeId,
        nodeId,
        interactionState,
        isUntrappedTypeableCombobox,
        beforeGuardElement: options.beforeGuardElement || null,
        afterGuardElement: options.afterGuardElement || null,
        portalNode,
        beforeOutsideGuard,
        afterOutsideGuard,
        restoreFocus,
        restoreFocusMode,
        get tabbableIndex() { return tabbableIndex; },
        get lastInteractionType() { return lastInteractionType; },
        handleTabIndex,

        dispose(shouldReturnFocus = true) {
            // Remove aria-modal
            floatingElement.removeAttribute('aria-modal');

            // Clean up markOthers
            this.markOthersCleanup?.();

            // Clean up focus out listener
            this.focusOutCleanup?.();

            // Clean up trigger focusout listener (FFM-F13)
            triggerFocusOutCleanup?.();

            // Clean up Tab keydown listener
            tabKeydownCleanup?.();

            // Clean up focusin listener
            focusInCleanup?.();

            // Clean up tabbable index focusin listener
            tabbableIndexCleanup?.();

            // Clean up mutation observer
            mutationObserver?.disconnect();

            // Clean up interaction tracking listeners
            interactionTrackingCleanup();

            // Prune disconnected entries from the global previouslyFocusedElements stack
            previouslyFocusedElements = previouslyFocusedElements.filter(e => e.isConnected);

            // Return focus (F8 - enhanced suppression)
            if (shouldReturnFocus && this.returnFocus !== false) {
                if (preventReturnFocus) {
                    preventReturnFocus = false;
                } else {
                    const active = activeElement(doc);
                    // Don't return focus if activeElement has moved somewhere meaningful
                    const hasMoved = active && active !== doc.body && !contains(floatingElement, active);

                    // Suppress return focus for hover closes (FFM-F9):
                    // If the last interaction was pointer, focus is NOT inside the floating element,
                    // and isPointerDown is false, this indicates a hover-close where focus was never
                    // in the popup — don't steal focus back to the trigger.
                    const isHoverClose = lastInteractionType === 'pointer' &&
                        !isPointerDown &&
                        active && !contains(floatingElement, active);

                    if (!hasMoved && !isHoverClose) {
                        const returnTarget = triggerElement || getPreviouslyFocusedElement();
                        const returnEl = this.returnFocus instanceof HTMLElement
                            ? this.returnFocus
                            : this.returnFocusElement || this.previouslyFocusedElement || returnTarget;
                        if (returnEl?.isConnected) {
                            enqueueFocus(returnEl);
                        } else if (returnFocusFallback?.isConnected) {
                            // Fallback span for when trigger disconnects (FFM-F14)
                            enqueueFocus(returnFocusFallback);
                        }
                    }
                }
            }

            // Clean up return focus fallback span (FFM-F14)
            returnFocusFallback?.remove();
            returnFocusFallback = null;

            focusManagers.delete(managerId);
        }
    };

    focusManagers.set(managerId, manager);
    return managerId;
}

/**
 * Disposes a floating focus manager.
 */
export function disposeFloatingFocusManager(managerId, shouldReturnFocus = true, overrideReturnFocusElement = null) {
    const manager = focusManagers.get(managerId);
    if (!manager) return;
    if (overrideReturnFocusElement) {
        manager.returnFocusElement = overrideReturnFocusElement;
    }
    manager.dispose(shouldReturnFocus);
}

/**
 * Returns the last interaction type tracked by the focus manager (e.g. "pointer", "keyboard").
 * Used to pass the close interaction type to ReturnFocusCallback.
 */
export function getLastInteractionType(managerId) {
    const manager = focusManagers.get(managerId);
    return manager?.lastInteractionType ?? '';
}

/**
 * Updates focus manager options after creation.
 */
export function updateFloatingFocusManager(managerId, options) {
    const manager = focusManagers.get(managerId);
    if (!manager) return;
    if (options.returnFocus !== undefined) manager.returnFocus = options.returnFocus;

    const modalChanged = options.modal !== undefined && options.modal !== manager.modal;
    const insideElementsChanged = options.insideElements !== undefined;

    if (modalChanged) {
        manager.modal = options.modal;
        if (options.modal) {
            manager.floatingElement.setAttribute('aria-modal', 'true');
        } else {
            manager.floatingElement.removeAttribute('aria-modal');
        }
    }

    if (insideElementsChanged) {
        manager.insideElements = options.insideElements;
    }

    const closeOnFocusOutChanged = options.closeOnFocusOut !== undefined && options.closeOnFocusOut !== manager.closeOnFocusOut;

    if (closeOnFocusOutChanged) {
        manager.closeOnFocusOut = options.closeOnFocusOut;
    }

    // Re-apply markOthers when modal or insideElements change
    if (modalChanged || insideElementsChanged) {
        manager.markOthersCleanup?.();
        manager.markOthersCleanup = null;

        // Use stored isUntrappedTypeableCombobox from creation time (F6)
        if (manager.modal || manager.isUntrappedTypeableCombobox) {
            const markAvoid = [manager.floatingElement, ...(manager.insideElements || [])];
            if (manager.triggerElement) markAvoid.push(manager.triggerElement);
            if (manager.beforeGuardElement) markAvoid.push(manager.beforeGuardElement);
            if (manager.afterGuardElement) markAvoid.push(manager.afterGuardElement);
            if (manager.nextFocusableElement) markAvoid.push(manager.nextFocusableElement);
            if (manager.previousFocusableElement) markAvoid.push(manager.previousFocusableElement);
            manager.markOthersCleanup = markOthers(markAvoid, manager.modal || manager.isUntrappedTypeableCombobox);
        }
    }

    // Toggle focusout listener when modal or closeOnFocusOut changes
    if (modalChanged || closeOnFocusOutChanged) {
        manager.focusOutCleanup?.();
        manager.focusOutCleanup = null;

        if (!manager.modal && manager.closeOnFocusOut && manager.onClose) {
            const handleFocusOut = createFocusOutHandler(manager, manager.interactionState);
            manager.floatingElement.addEventListener('focusout', handleFocusOut);

            // Also attach to trigger for Safari button focus loss (F4)
            if (manager.triggerElement) {
                manager.triggerElement.addEventListener('focusout', handleFocusOut);
                function handleTriggerPointerDown() {
                    if (isSafari()) {
                        manager.interactionState.preventReturnFocus = false;
                    }
                }
                manager.triggerElement.addEventListener('pointerdown', handleTriggerPointerDown);
                manager.focusOutCleanup = () => {
                    manager.floatingElement.removeEventListener('focusout', handleFocusOut);
                    manager.triggerElement.removeEventListener('focusout', handleFocusOut);
                    manager.triggerElement.removeEventListener('pointerdown', handleTriggerPointerDown);
                };
            } else {
                manager.focusOutCleanup = () => manager.floatingElement.removeEventListener('focusout', handleFocusOut);
            }
        }
    }

    // Update order and re-run handleTabIndex
    if (options.order !== undefined) {
        manager.order = options.order;
        manager.handleTabIndex(manager.floatingElement, options.order);
    }
}

/**
 * Handles focus guard focus events by redirecting focus appropriately (F3).
 */
export function handleFocusGuardFocus(managerId, direction) {
    const manager = focusManagers.get(managerId);
    if (!manager) return;

    if (direction === 'before') {
        if (manager.modal) {
            const els = getTabbableElements(manager.floatingElement);
            const last = els[els.length - 1] || manager.floatingElement;
            if (last && last.focus) last.focus();
        } else if (manager.portalNode) {
            const target = manager.previousFocusableElement || manager.beforeOutsideGuard;
            if (target && target.focus) target.focus();
        } else {
            const target = manager.previousFocusableElement || manager.triggerElement;
            if (target && target.focus) target.focus();
        }
    } else {
        if (manager.modal) {
            const els = getTabbableElements(manager.floatingElement);
            const first = els[0] || manager.floatingElement;
            if (first && first.focus) first.focus();
        } else if (manager.portalNode) {
            if (manager.closeOnFocusOut) {
                manager.interactionState.preventReturnFocus = true;
            }
            const target = manager.nextFocusableElement || manager.afterOutsideGuard;
            if (target && target.focus) {
                target.focus();
            } else if (manager.closeOnFocusOut && manager.onClose) {
                manager.onClose();
            }
        } else {
            const target = manager.nextFocusableElement;
            if (target && target.focus) {
                target.focus();
            } else if (manager.closeOnFocusOut && manager.onClose) {
                manager.onClose();
            }
        }
    }
}

// ============================================================================
// Tree-Aware Bubble Prevention
// ============================================================================

/**
 * Normalizes the bubbles prop into separate escape and outsidePress booleans.
 */
function normalizeBubblesProp(bubbles) {
    if (bubbles === undefined || bubbles === true) {
        return { escapeKey: true, outsidePress: true };
    }
    if (bubbles === false) {
        return { escapeKey: false, outsidePress: false };
    }
    return {
        escapeKey: bubbles.escapeKey !== undefined ? bubbles.escapeKey : true,
        outsidePress: bubbles.outsidePress !== undefined ? bubbles.outsidePress : true
    };
}

/**
 * Sets the bubble prevention flags for a node in the floating tree.
 */
export function setNodeDismissBubbles(treeId, nodeId, options) {
    const tree = state.trees.get(treeId);
    if (!tree) return;

    const node = tree.nodes.find(n => n.id === nodeId);
    if (!node) return;

    const normalized = normalizeBubblesProp(options);
    node.__escapeKeyBubbles = normalized.escapeKey;
    node.__outsidePressBubbles = normalized.outsidePress;
}

// ============================================================================
// List Navigation Utilities
// ============================================================================

/**
 * CSS selector matching navigable list items.
 */
export const NAVIGABLE_ITEM_SELECTOR =
    '[role="option"], [role="menuitem"], [role="menuitemcheckbox"], ' +
    '[role="menuitemradio"], [data-blazor-base-ui-list-item]';

/**
 * Creates a shared item registry for tracking list items.
 */
export function createItemRegistry() {
    const items = new Map();
    let orderedItems = [];
    let needsSort = false;

    function sortItems() {
        if (!needsSort) return;
        orderedItems = Array.from(items.values()).sort((a, b) => {
            if (!a.element || !b.element) return 0;
            const position = a.element.compareDocumentPosition(b.element);
            if (position & Node.DOCUMENT_POSITION_FOLLOWING) return -1;
            if (position & Node.DOCUMENT_POSITION_PRECEDING) return 1;
            return 0;
        });
        orderedItems.forEach((item, i) => item.index = i);
        needsSort = false;
    }

    return {
        registerItem(id, element, label, disabled) {
            items.set(id, { id, element, label, disabled, index: items.size });
            needsSort = true;
        },

        unregisterItem(id) {
            items.delete(id);
            needsSort = true;
        },

        getItems() {
            sortItems();
            return [...orderedItems];
        },

        getItemByIndex(index) {
            sortItems();
            return orderedItems[index] ?? null;
        },

        getItemCount() {
            return items.size;
        },

        getDisabledIndices() {
            sortItems();
            return orderedItems
                .filter(item => item.disabled)
                .map(item => item.index);
        },

        updateItemDisabled(id, disabled) {
            const item = items.get(id);
            if (item) item.disabled = disabled;
        },

        dispose() {
            items.clear();
            orderedItems = [];
        }
    };
}

/**
 * Creates a grid cell map for multi-column navigation.
 */
export function createGridCellMap(itemCount, cols) {
    return {
        getPosition(index) {
            const row = Math.floor(index / cols);
            const col = index % cols;
            return [row, col];
        },
        getIndex(row, col) {
            return row * cols + col;
        },
        getRowCount() {
            return Math.ceil(itemCount / cols);
        }
    };
}

/**
 * Calculates the target index for a grid navigation action.
 */
export function getGridNavigatedIndex(options) {
    const {
        currentIndex,
        itemCount,
        cols,
        direction,
        loop = false,
        disabledIndices = [],
        rtl = false
    } = options;

    const grid = createGridCellMap(itemCount, cols);
    const [row, col] = grid.getPosition(currentIndex);
    const rows = grid.getRowCount();

    let targetRow = row;
    let targetCol = col;

    const effectiveDirection = rtl
        ? (direction === 'left' ? 'right' : direction === 'right' ? 'left' : direction)
        : direction;

    switch (effectiveDirection) {
        case 'up':
            targetRow = row - 1;
            if (targetRow < 0) targetRow = loop ? rows - 1 : -1;
            break;
        case 'down':
            targetRow = row + 1;
            if (targetRow >= rows) targetRow = loop ? 0 : -1;
            break;
        case 'left':
            targetCol = col - 1;
            if (targetCol < 0) {
                if (loop) {
                    targetCol = cols - 1;
                    targetRow = row - 1;
                    if (targetRow < 0) targetRow = rows - 1;
                } else {
                    return -1;
                }
            }
            break;
        case 'right':
            targetCol = col + 1;
            if (targetCol >= cols) {
                if (loop) {
                    targetCol = 0;
                    targetRow = row + 1;
                    if (targetRow >= rows) targetRow = 0;
                } else {
                    return -1;
                }
            }
            break;
    }

    if (targetRow < 0) return -1;

    let targetIndex = grid.getIndex(targetRow, targetCol);
    if (targetIndex >= itemCount) {
        if (loop) targetIndex = targetIndex % itemCount;
        else return -1;
    }

    // Skip disabled indices
    if (disabledIndices.includes(targetIndex)) {
        const nextDir = (effectiveDirection === 'up' || effectiveDirection === 'left') ? -1 : 1;
        return findNextEnabledIndex(targetIndex, itemCount, disabledIndices, nextDir, loop);
    }

    return targetIndex;
}

/**
 * Finds the next enabled index in a list, skipping disabled indices.
 */
export function findNextEnabledIndex(currentIndex, itemCount, disabledIndices, direction, loop) {
    let index = currentIndex + direction;
    let steps = 0;

    while (steps < itemCount) {
        if (index < 0) {
            if (loop) index = itemCount - 1;
            else return -1;
        }
        if (index >= itemCount) {
            if (loop) index = 0;
            else return -1;
        }
        if (!disabledIndices.includes(index)) {
            return index;
        }
        index += direction;
        steps++;
    }

    return -1;
}

/**
 * Applies active/highlight state to a list item element and removes from previous.
 */
export function applyActiveState(listElement, itemElement, virtual) {
    // Remove previous active state
    const prev = listElement.querySelector('[data-blazor-base-ui-active]');
    if (prev) {
        prev.removeAttribute('data-blazor-base-ui-active');
        if (!virtual) prev.setAttribute('tabindex', '-1');
    }

    if (itemElement) {
        itemElement.setAttribute('data-blazor-base-ui-active', '');
        if (virtual) {
            listElement.setAttribute('aria-activedescendant', itemElement.id || '');
        } else {
            itemElement.setAttribute('tabindex', '0');
            itemElement.focus();
        }
    } else if (virtual) {
        listElement.removeAttribute('aria-activedescendant');
    }
}

/**
 * Creates a list navigation controller for keyboard-driven item navigation.
 */
export function createListNavigation(options) {
    const {
        listElement,
        registry,
        orientation = 'vertical',
        cols = 1,
        loop = false,
        allowEscape = false,
        virtual = false,
        focusItemOnOpen = true,
        focusItemOnHover = true,
        scrollIntoView = true,
        nested = false,
        rtl = false,
        onNavigate = null,
        onActivate = null,
        treeId = null,
        nodeId = null
    } = options;

    const navId = `nav-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    let activeIndex = -1;
    const isGrid = cols > 1;

    function getArrowDirection(key) {
        const isVertical = orientation === 'vertical' || orientation === 'both';
        const isHorizontal = orientation === 'horizontal' || orientation === 'both';

        const effectiveKey = rtl
            ? (key === 'ArrowLeft' ? 'ArrowRight' : key === 'ArrowRight' ? 'ArrowLeft' : key)
            : key;

        if (isGrid) {
            switch (effectiveKey) {
                case 'ArrowUp': return 'up';
                case 'ArrowDown': return 'down';
                case 'ArrowLeft': return 'left';
                case 'ArrowRight': return 'right';
            }
            return null;
        }

        if (isVertical && effectiveKey === 'ArrowDown') return 'down';
        if (isVertical && effectiveKey === 'ArrowUp') return 'up';
        if (isHorizontal && effectiveKey === 'ArrowRight') return 'right';
        if (isHorizontal && effectiveKey === 'ArrowLeft') return 'left';

        // Nested submenu: arrow key opens/closes
        if (nested) {
            if (effectiveKey === 'ArrowRight') return 'open';
            if (effectiveKey === 'ArrowLeft') return 'close';
        }

        return null;
    }

    function navigateTo(index) {
        if (index < 0 || index >= registry.getItemCount()) return;
        activeIndex = index;
        const item = registry.getItemByIndex(index);
        if (!item) return;
        applyActiveState(listElement, item.element, virtual);
        if (scrollIntoView && item.element) {
            item.element.scrollIntoView?.({ block: 'nearest' });
        }
        onNavigate?.(index);
    }

    function handleKeyDown(event) {
        const itemCount = registry.getItemCount();
        if (itemCount === 0) return;

        const disabledIndices = registry.getDisabledIndices();

        if (event.key === 'Home') {
            event.preventDefault();
            const first = findNextEnabledIndex(-1, itemCount, disabledIndices, 1, false);
            if (first !== -1) navigateTo(first);
            return;
        }

        if (event.key === 'End') {
            event.preventDefault();
            const last = findNextEnabledIndex(itemCount, itemCount, disabledIndices, -1, false);
            if (last !== -1) navigateTo(last);
            return;
        }

        if (event.key === 'Enter' || event.key === ' ') {
            if (activeIndex >= 0) {
                event.preventDefault();
                onActivate?.(activeIndex);
            }
            return;
        }

        const direction = getArrowDirection(event.key);
        if (!direction) return;

        if (direction === 'open' || direction === 'close') {
            // Let parent handle submenu open/close
            return;
        }

        event.preventDefault();

        if (isGrid) {
            const target = getGridNavigatedIndex({
                currentIndex: activeIndex < 0 ? 0 : activeIndex,
                itemCount,
                cols,
                direction,
                loop,
                disabledIndices,
                rtl
            });
            if (target !== -1) navigateTo(target);
        } else {
            const dir = (direction === 'down' || direction === 'right') ? 1 : -1;
            if (activeIndex < 0) {
                const first = dir === 1
                    ? findNextEnabledIndex(-1, itemCount, disabledIndices, 1, loop)
                    : findNextEnabledIndex(itemCount, itemCount, disabledIndices, -1, loop);
                if (first !== -1) navigateTo(first);
            } else {
                const next = findNextEnabledIndex(activeIndex, itemCount, disabledIndices, dir, loop);
                if (next !== -1) {
                    navigateTo(next);
                } else if (allowEscape) {
                    activeIndex = -1;
                    applyActiveState(listElement, null, virtual);
                }
            }
        }
    }

    function handlePointerMove(event, index) {
        if (!focusItemOnHover) return;
        const item = registry.getItemByIndex(index);
        if (item && !item.disabled) {
            navigateTo(index);
        }
    }

    function setActiveIndex(index) {
        navigateTo(index);
    }

    function getActiveIndex() {
        return activeIndex;
    }

    // Handle focus-on-open
    if (focusItemOnOpen === true) {
        requestAnimationFrame(() => {
            const disabledIndices = registry.getDisabledIndices();
            const first = findNextEnabledIndex(-1, registry.getItemCount(), disabledIndices, 1, false);
            if (first !== -1) navigateTo(first);
        });
    } else if (focusItemOnOpen === 'selected') {
        // Caller must provide selected index via setActiveIndex
    }

    return {
        navId,
        handleKeyDown,
        handlePointerMove,
        setActiveIndex,
        getActiveIndex,
        dispose() {
            activeIndex = -1;
            applyActiveState(listElement, null, virtual);
        }
    };
}

// ============================================================================
// Typeahead
// ============================================================================

/**
 * Creates a typeahead controller for type-ahead matching in lists.
 */
export function createTypeahead(options) {
    const {
        registry,
        resetMs = 750,
        ignoreKeys = [],
        findMatch = null,
        onMatch = null,
        onTypingChange = null
    } = options;

    const typeaheadId = `ta-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    let stringRef = '';
    let prevIndex = -1;
    let resetTimeout = null;
    let isTyping = false;

    function setTyping(value) {
        if (isTyping !== value) {
            isTyping = value;
            onTypingChange?.(value);
        }
    }

    function handleKeyDown(event) {
        // Ignore modifier keys
        if (event.ctrlKey || event.metaKey || event.altKey) return;

        // Only process single characters
        if (event.key.length !== 1) return;

        // Check ignore list
        if (ignoreKeys.includes(event.key)) return;

        // Handle space: prevent closing popup if mid-search
        if (event.key === ' ' && stringRef.length > 0 && !stringRef.startsWith(' ')) {
            event.preventDefault();
        }

        const items = registry.getItems();
        if (items.length === 0) return;

        // Clear existing timeout
        if (resetTimeout) clearTimeout(resetTimeout);

        // Same-character cycling
        if (stringRef === event.key) {
            // Reset buffer and advance from current match
            stringRef = '';
            const cycleResult = findMatchingItem(items, event.key, prevIndex + 1);
            if (cycleResult !== -1) {
                prevIndex = cycleResult;
                onMatch?.(cycleResult);
                startResetTimer();
                return;
            }
        }

        stringRef += event.key;
        setTyping(true);

        // Custom match function
        if (findMatch) {
            const labels = items.map(item => item.label || '');
            const matchLabel = findMatch(labels, stringRef);
            if (matchLabel) {
                const matchIndex = items.findIndex(item => (item.label || '') === matchLabel);
                if (matchIndex !== -1 && !items[matchIndex].disabled) {
                    prevIndex = matchIndex;
                    onMatch?.(matchIndex);
                }
            }
        } else {
            // Default: search from prevIndex + 1, wrapping around
            const result = findMatchingItem(items, stringRef, prevIndex + 1);
            if (result !== -1) {
                prevIndex = result;
                onMatch?.(result);
            }
        }

        startResetTimer();
    }

    function findMatchingItem(items, search, startFrom) {
        const lowerSearch = search.toLocaleLowerCase();
        const count = items.length;

        // Search from startFrom to end
        for (let i = 0; i < count; i++) {
            const index = (startFrom + i) % count;
            const item = items[index];
            if (item.disabled) continue;
            const label = (item.label || '').toLocaleLowerCase();
            if (label.startsWith(lowerSearch)) return index;
        }

        return -1;
    }

    function startResetTimer() {
        resetTimeout = setTimeout(() => {
            stringRef = '';
            setTyping(false);
            resetTimeout = null;
        }, resetMs);
    }

    function reset() {
        if (resetTimeout) clearTimeout(resetTimeout);
        stringRef = '';
        setTyping(false);
    }

    return {
        typeaheadId,
        handleKeyDown,
        reset,
        isTyping: () => isTyping,
        dispose() {
            if (resetTimeout) clearTimeout(resetTimeout);
            stringRef = '';
            isTyping = false;
        }
    };
}

// ============================================================================
// Multi-Trigger Element Map
// ============================================================================

const triggerMaps = new Map();

/**
 * Registers a trigger element with a dismiss interaction.
 */
export function registerTriggerElement(interactionId, triggerId, element) {
    if (!triggerMaps.has(interactionId)) {
        triggerMaps.set(interactionId, new Map());
    }
    triggerMaps.get(interactionId).set(triggerId, element);
}

/**
 * Unregisters a trigger element from a dismiss interaction.
 */
export function unregisterTriggerElement(interactionId, triggerId) {
    const map = triggerMaps.get(interactionId);
    if (!map) return;
    map.delete(triggerId);
    if (map.size === 0) triggerMaps.delete(interactionId);
}

/**
 * Checks if an event target is within any registered trigger for a dismiss interaction.
 */
function isTargetInRegisteredTriggers(interactionId, event) {
    const map = triggerMaps.get(interactionId);
    if (!map) return false;
    for (const element of map.values()) {
        if (isEventTargetWithin(event, element)) return true;
    }
    return false;
}

// ============================================================================
// Virtual Element / clientPoint
// ============================================================================

const virtualElements = new Map();
let virtualCounter = 0;

/**
 * Creates a virtual element at specified coordinates that conforms to
 * FloatingUI's VirtualElement interface.
 */
export function createVirtualElement(x, y) {
    const virtualId = `virtual-${++virtualCounter}`;

    const virtualElement = {
        virtualId,
        _x: x,
        _y: y,
        getBoundingClientRect() {
            return {
                x: this._x,
                y: this._y,
                top: this._y,
                left: this._x,
                bottom: this._y,
                right: this._x,
                width: 0,
                height: 0
            };
        },
        update(newX, newY) {
            this._x = newX;
            this._y = newY;
        }
    };

    virtualElements.set(virtualId, virtualElement);
    return virtualElement;
}

/**
 * Updates the coordinates of an existing virtual element.
 */
export function updateVirtualElement(virtualId, x, y) {
    const ve = virtualElements.get(virtualId);
    if (!ve) return;
    ve._x = x;
    ve._y = y;
}

/**
 * Disposes a virtual element and cleans up internal state.
 */
export function disposeVirtualElement(virtualId) {
    virtualElements.delete(virtualId);
}

const clientPointInteractions = new Map();

/**
 * Creates an interaction that tracks pointer position and updates
 * a virtual element's coordinates accordingly.
 */
export function createClientPointInteraction(options) {
    const {
        enabled = true,
        axis = 'both',
        floatingElement,
        virtualId,
        onUpdate = null
    } = options;

    const interactionId = `cp-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    const doc = getDocument(floatingElement);

    function handlePointerMove(event) {
        if (!enabled) return;
        const ve = virtualElements.get(virtualId);
        if (!ve) return;

        const newX = (axis === 'both' || axis === 'x') ? event.clientX : ve._x;
        const newY = (axis === 'both' || axis === 'y') ? event.clientY : ve._y;

        ve._x = newX;
        ve._y = newY;
        onUpdate?.(newX, newY);
    }

    doc.addEventListener('pointermove', handlePointerMove);

    const interaction = {
        interactionId,
        dispose() {
            doc.removeEventListener('pointermove', handlePointerMove);
            clientPointInteractions.delete(interactionId);
        }
    };

    clientPointInteractions.set(interactionId, interaction);
    return interaction;
}

// ============================================================================
// getOverflowAncestors Standalone
// ============================================================================

/**
 * Alias for getScrollParents that matches FloatingUI's naming convention.
 */
export function getOverflowAncestors(element) {
    return getScrollParents(element);
}

// ============================================================================
// Third-Party Element Detection
// ============================================================================

/**
 * Checks whether an event target is a third-party element injected into
 * the DOM after the floating element rendered.
 */
export function isThirdPartyElement(target, floatingElement) {
    if (!target || !floatingElement) return false;

    // Walk up to find the root ancestor
    let root = target;
    while (root.parentElement) {
        root = root.parentElement;
    }

    // If the root is not the document element, it's detached — treat as third-party
    if (root !== document.documentElement && root !== document) return true;

    // Check if the target's root contains any data-blazor-base-ui-inert markers
    const inertMarkers = document.querySelectorAll('[data-blazor-base-ui-inert]');
    if (inertMarkers.length === 0) return false;

    // If target is a direct ancestor of the floating element (e.g., overlay), not third-party
    if (contains(target, floatingElement)) return false;

    // Check if the target itself or its ancestors have inert markers
    // If they don't, and inert markers exist, this element was injected after markOthers ran
    let current = target;
    while (current && current !== document.body) {
        if (current.hasAttribute('data-blazor-base-ui-inert')) return false;
        current = current.parentElement;
    }

    // The element's ancestors have no inert markers — it was injected after modal opened
    return true;
}

// ============================================================================
// FloatingDelayGroup
// ============================================================================

const DELAY_GROUP_KEY = Symbol.for('BlazorBaseUI.DelayGroup.State');
if (!window[DELAY_GROUP_KEY]) {
    window[DELAY_GROUP_KEY] = { groups: new Map(), counter: 0 };
}
const delayGroupState = window[DELAY_GROUP_KEY];

/**
 * Creates a delay group for coordinating open/close delays across
 * multiple floating elements (typically tooltips).
 */
export function createDelayGroup(options) {
    const { delay, timeoutMs = 0 } = options;

    const groupId = `dg-${++delayGroupState.counter}`;
    const openDelay = typeof delay === 'number' ? delay : (delay?.open ?? 0);
    const closeDelay = typeof delay === 'number' ? delay : (delay?.close ?? 0);

    const group = {
        groupId,
        openDelay,
        closeDelay,
        timeoutMs,
        setIsInstantPhaseRef: options.setIsInstantPhaseRef || null,
        members: new Map(),
        isInstantPhase: false,
        currentOpenId: null,
        timeoutHandle: null,

        getDelay() {
            if (this.isInstantPhase) {
                return { open: 0, close: this.closeDelay };
            }
            return { open: this.openDelay, close: this.closeDelay };
        },

        dispose() {
            if (this.timeoutHandle) clearTimeout(this.timeoutHandle);
            this.members.clear();
            delayGroupState.groups.delete(groupId);
        }
    };

    delayGroupState.groups.set(groupId, group);
    return group;
}

/**
 * Registers a floating element as a member of a delay group.
 */
export function registerDelayGroupMember(groupId, interactionId, callbacks) {
    const group = delayGroupState.groups.get(groupId);
    if (!group) return;

    group.members.set(interactionId, {
        ...callbacks,
        interactionId
    });
}

/**
 * Notifies the delay group that a member has opened.
 * Cancels pending timeout, sets instant phase, and closes previous member.
 */
export function notifyDelayGroupMemberOpened(groupId, interactionId) {
    const group = delayGroupState.groups.get(groupId);
    if (!group) return;

    if (group.timeoutHandle) {
        clearTimeout(group.timeoutHandle);
        group.timeoutHandle = null;
    }

    const previousOpenId = group.currentOpenId;
    group.currentOpenId = interactionId;

    if (previousOpenId !== null && previousOpenId !== interactionId) {
        group.isInstantPhase = true;

        // Notify group-level context once
        group.setIsInstantPhaseRef?.invokeMethodAsync('SetIsInstantPhase', true);

        // Notify current member
        const currentMember = group.members.get(interactionId);
        if (currentMember) {
            currentMember.closeMember?.invokeMethodAsync('SetMemberInstantPhase', true);
        }

        // Notify and close previous member
        const prevMember = group.members.get(previousOpenId);
        if (prevMember) {
            prevMember.closeMember?.invokeMethodAsync('SetMemberInstantPhase', true);
            prevMember.closeMember?.invokeMethodAsync('CloseMember', 'delay-group');
        }
    } else {
        // No previous member open, or same member re-opened — not instant phase
        group.isInstantPhase = false;

        // Notify group-level context once
        group.setIsInstantPhaseRef?.invokeMethodAsync('SetIsInstantPhase', false);
    }
}

/**
 * Notifies the delay group that a member has closed.
 * Starts timeout to exit instant phase if applicable.
 */
export function notifyDelayGroupMemberClosed(groupId, interactionId) {
    const group = delayGroupState.groups.get(groupId);
    if (!group) return;

    if (group.currentOpenId !== interactionId) return;

    group.currentOpenId = null;

    // Notify only the closing member
    const closingMember = group.members.get(interactionId);
    if (closingMember) {
        closingMember.closeMember?.invokeMethodAsync('SetMemberInstantPhase', false);
    }

    if (group.timeoutMs > 0) {
        group.timeoutHandle = setTimeout(() => {
            // Guard: if another member opened during the timeout, skip the reset
            if (group.currentOpenId !== null) {
                group.timeoutHandle = null;
                return;
            }

            group.isInstantPhase = false;
            group.setIsInstantPhaseRef?.invokeMethodAsync('SetIsInstantPhase', false);
            group.timeoutHandle = null;
        }, group.timeoutMs);
    } else {
        group.isInstantPhase = false;
        group.setIsInstantPhaseRef?.invokeMethodAsync('SetIsInstantPhase', false);
    }
}

/**
 * Unregisters a floating element from a delay group.
 */
export function unregisterDelayGroupMember(groupId, interactionId) {
    const group = delayGroupState.groups.get(groupId);
    if (!group) return;

    if (group.currentOpenId === interactionId) {
        notifyDelayGroupMemberClosed(groupId, interactionId);
    }

    group.members.delete(interactionId);
}

/**
 * Disposes a delay group and all its internal state.
 */
export function disposeDelayGroup(groupId) {
    const group = delayGroupState.groups.get(groupId);
    if (!group) return;
    group.dispose();
}

/**
 * Updates delay group options after creation (e.g., when parameters change).
 */
export function updateDelayGroupOptions(groupId, options) {
    const group = delayGroupState.groups.get(groupId);
    if (!group) return;
    const { delay, timeoutMs } = options;
    if (delay !== undefined) {
        group.openDelay = typeof delay === 'number' ? delay : (delay?.open ?? group.openDelay);
        group.closeDelay = typeof delay === 'number' ? delay : (delay?.close ?? group.closeDelay);
    }
    if (timeoutMs !== undefined) group.timeoutMs = timeoutMs;
}

// ============================================================================
// Floating Tree Event Bridge (FT-F7)
// ============================================================================

let treeEventListenerId = 0;

/**
 * Emits an event on the JS-side tree event emitter.
 */
export function emitTreeEvent(treeId, eventName, data) {
    const tree = state.trees?.get(treeId);
    if (tree) tree.events.emit(eventName, data);
}

/**
 * Subscribes a .NET callback to a JS-side tree event.
 * Returns a string listener ID for cleanup.
 */
export function onTreeEvent(treeId, eventName, dotNetRef, methodName) {
    const tree = state.trees?.get(treeId);
    if (!tree) return null;
    const id = String(++treeEventListenerId);
    const listener = (data) => dotNetRef.invokeMethodAsync(methodName, data).catch(() => {});
    tree.events.on(eventName, listener);
    if (!tree._jsListeners) tree._jsListeners = new Map();
    tree._jsListeners.set(id, { eventName, listener });
    return id;
}

/**
 * Unsubscribes a previously registered JS-side tree event listener.
 */
export function offTreeEvent(treeId, listenerId) {
    const tree = state.trees?.get(treeId);
    if (!tree?._jsListeners) return;
    const entry = tree._jsListeners.get(listenerId);
    if (entry) {
        tree.events.off(entry.eventName, entry.listener);
        tree._jsListeners.delete(listenerId);
    }
}

// ============================================================================
// InternalBackdrop — cutout clip-path management
// ============================================================================
//
// BLAZOR WORKAROUND: In the React source, InternalBackdrop computes the cutout
// clip-path synchronously during render via getBoundingClientRect(). Blazor
// cannot read DOM layout during render, so instead we initialize the clip-path
// once via JS interop and then keep it updated reactively using ResizeObserver
// and scroll listeners. This avoids per-render JS interop calls and ensures the
// clip-path stays in sync when the cutout element moves (scroll, resize, layout).
//
// React source: .base-ui/packages/react/src/utils/InternalBackdrop.tsx
// ============================================================================

const backdropInstances = new Map();
let backdropCounter = 0;

/**
 * Computes the clip-path polygon for a cutout element and applies it to the
 * backdrop element's style directly.
 */
function updateBackdropClipPath(backdropElement, cutoutElement) {
    if (!cutoutElement || !backdropElement) return;

    const rect = cutoutElement.getBoundingClientRect();
    backdropElement.style.clipPath =
        `polygon(0% 0%, 100% 0%, 100% 100%, 0% 100%, 0% 0%, ` +
        `${rect.left}px ${rect.top}px, ${rect.left}px ${rect.bottom}px, ` +
        `${rect.right}px ${rect.bottom}px, ${rect.right}px ${rect.top}px, ` +
        `${rect.left}px ${rect.top}px)`;
}

/**
 * Collects all scrollable ancestors of an element for scroll listening.
 */
function getScrollAncestors(element) {
    const ancestors = [];
    let current = element.parentElement;
    while (current) {
        const style = getComputedStyle(current);
        if (/(auto|scroll|overlay)/.test(style.overflow + style.overflowX + style.overflowY)) {
            ancestors.push(current);
        }
        current = current.parentElement;
    }
    ancestors.push(window);
    return ancestors;
}

/**
 * Initializes reactive clip-path management for an InternalBackdrop.
 * Sets up ResizeObserver and scroll listeners to keep the cutout in sync.
 *
 * @param {HTMLElement} backdropElement - The backdrop div element.
 * @param {HTMLElement} cutoutElement - The element to cut out.
 * @returns {string} The backdrop instance ID.
 */
export function initializeBackdrop(backdropElement, cutoutElement) {
    const id = `backdrop-${++backdropCounter}`;

    // Initial clip-path
    updateBackdropClipPath(backdropElement, cutoutElement);

    const update = () => updateBackdropClipPath(backdropElement, cutoutElement);

    // Observe resize of the cutout element
    const resizeObserver = new ResizeObserver(update);
    resizeObserver.observe(cutoutElement);

    // Listen for scroll on all scrollable ancestors
    const scrollAncestors = getScrollAncestors(cutoutElement);
    for (const ancestor of scrollAncestors) {
        ancestor.addEventListener('scroll', update, { passive: true });
    }

    // Listen for window resize
    window.addEventListener('resize', update, { passive: true });

    backdropInstances.set(id, {
        backdropElement,
        cutoutElement,
        resizeObserver,
        scrollAncestors,
        update
    });

    return id;
}

/**
 * Updates the cutout element for an existing backdrop instance.
 * Tears down old listeners and sets up new ones for the new element.
 *
 * @param {string} id - The backdrop instance ID.
 * @param {HTMLElement} cutoutElement - The new cutout element.
 */
export function updateBackdropCutout(id, cutoutElement) {
    const instance = backdropInstances.get(id);
    if (!instance) return;

    // Tear down old listeners
    instance.resizeObserver.disconnect();
    for (const ancestor of instance.scrollAncestors) {
        ancestor.removeEventListener('scroll', instance.update);
    }
    window.removeEventListener('resize', instance.update);

    // Set up new listeners
    instance.cutoutElement = cutoutElement;
    instance.update = () => updateBackdropClipPath(instance.backdropElement, cutoutElement);

    updateBackdropClipPath(instance.backdropElement, cutoutElement);

    instance.resizeObserver = new ResizeObserver(instance.update);
    instance.resizeObserver.observe(cutoutElement);

    instance.scrollAncestors = getScrollAncestors(cutoutElement);
    for (const ancestor of instance.scrollAncestors) {
        ancestor.addEventListener('scroll', instance.update, { passive: true });
    }
    window.addEventListener('resize', instance.update, { passive: true });
}

/**
 * Disposes a backdrop instance and cleans up all listeners.
 *
 * @param {string} id - The backdrop instance ID.
 */
export function disposeBackdrop(id) {
    const instance = backdropInstances.get(id);
    if (!instance) return;

    instance.resizeObserver.disconnect();
    for (const ancestor of instance.scrollAncestors) {
        ancestor.removeEventListener('scroll', instance.update);
    }
    window.removeEventListener('resize', instance.update);

    // Clear clip-path
    if (instance.backdropElement) {
        instance.backdropElement.style.clipPath = '';
    }

    backdropInstances.delete(id);
}

// ============================================================================
// FloatingPortal — portal-level focus management
// ============================================================================

const portalInstances = new Map();
let portalCounter = 0;

/**
 * Initializes focus management for a FloatingPortal instance.
 * Attaches focusin/focusout listeners on the portal node for non-modal tabbability
 * and sets up outside focus guard handlers.
 *
 * @param {object} options
 * @param {HTMLElement} options.portalNode - The portal container element.
 * @param {HTMLElement} [options.beforeOutsideGuard] - The before-outside focus guard element.
 * @param {HTMLElement} [options.afterOutsideGuard] - The after-outside focus guard element.
 * @param {HTMLElement} [options.beforeInsideGuard] - The before-inside focus guard element.
 * @param {HTMLElement} [options.afterInsideGuard] - The after-inside focus guard element.
 * @param {DotNetObjectReference} [options.dotNetRef] - .NET reference for close callbacks.
 * @returns {string} The portal instance ID.
 */
export function initializeFloatingPortal(options) {
    const {
        portalNode,
        beforeOutsideGuard = null,
        afterOutsideGuard = null,
        beforeInsideGuard = null,
        afterInsideGuard = null,
        dotNetRef = null
    } = options;

    const portalId = `portal-${++portalCounter}`;

    const instance = {
        portalNode,
        beforeOutsideGuard,
        afterOutsideGuard,
        beforeInsideGuard,
        afterInsideGuard,
        dotNetRef,
        focusManagerState: null,
        cleanup: null
    };

    attachPortalFocusListeners(instance);
    portalInstances.set(portalId, instance);
    return portalId;
}

/**
 * Updates the focus manager state for a portal instance.
 * Called by FloatingFocusManager when it mounts/unmounts within the portal.
 *
 * @param {string} portalId - The portal instance ID.
 * @param {object|null} focusManagerState - The focus state, or null to clear.
 * @param {boolean} focusManagerState.modal - Whether focus is trapped.
 * @param {boolean} focusManagerState.open - Whether the floating element is open.
 * @param {HTMLElement} [focusManagerState.domReference] - The trigger element.
 * @param {boolean} focusManagerState.closeOnFocusOut - Whether to close on focus-out.
 */
export function updatePortalFocusManagerState(portalId, focusManagerState) {
    const instance = portalInstances.get(portalId);
    if (!instance) return;

    instance.focusManagerState = focusManagerState;

    // Re-attach listeners since behavior depends on modal/open state
    detachPortalFocusListeners(instance);
    attachPortalFocusListeners(instance);
}

/**
 * Disposes a FloatingPortal instance and cleans up all listeners.
 *
 * @param {string} portalId - The portal instance ID.
 */
export function disposeFloatingPortal(portalId) {
    const instance = portalInstances.get(portalId);
    if (!instance) return;

    detachPortalFocusListeners(instance);
    portalInstances.delete(portalId);
}

/**
 * Attaches focusin/focusout listeners on the portal node for non-modal tabbability,
 * and outside focus guard focus handlers.
 */
function attachPortalFocusListeners(instance) {
    const { portalNode, focusManagerState } = instance;
    if (!portalNode) return;

    const modal = focusManagerState?.modal ?? false;
    const open = focusManagerState?.open ?? false;
    const cleanups = [];

    // Non-modal focus management: enable/disable focus inside the portal
    // when focus enters/leaves, so portal contents are only tabbable when
    // the portal itself has been focused.
    if (!modal && portalNode) {
        function onFocusChange(event) {
            if (event.relatedTarget && isOutsideEvent(event, portalNode)) {
                const focusing = event.type === 'focusin';
                const manageFocus = focusing ? enableFocusInside : disableFocusInside;
                manageFocus(portalNode);
            }
        }

        portalNode.addEventListener('focusin', onFocusChange, true);
        portalNode.addEventListener('focusout', onFocusChange, true);
        cleanups.push(() => {
            portalNode.removeEventListener('focusin', onFocusChange, true);
            portalNode.removeEventListener('focusout', onFocusChange, true);
        });
    }

    // When not open, re-enable focus inside the portal
    if (!open && portalNode) {
        enableFocusInside(portalNode);
    }

    // Outside focus guard handlers
    if (instance.beforeOutsideGuard) {
        function handleBeforeOutsideFocus(event) {
            const fms = instance.focusManagerState;
            if (isOutsideEvent(event, portalNode)) {
                // Focus came from outside — redirect to inside before guard
                instance.beforeInsideGuard?.focus();
            } else {
                // Focus came from inside (Shift+Tab past first element) — move to previous tabbable in page
                const domReference = fms?.domReference ?? null;
                const prevTabbable = getPreviousTabbable(domReference, document.body, document.body);
                prevTabbable?.focus();
            }
        }

        instance.beforeOutsideGuard.addEventListener('focus', handleBeforeOutsideFocus);
        cleanups.push(() => {
            instance.beforeOutsideGuard?.removeEventListener('focus', handleBeforeOutsideFocus);
        });
    }

    if (instance.afterOutsideGuard) {
        function handleAfterOutsideFocus(event) {
            const fms = instance.focusManagerState;
            if (isOutsideEvent(event, portalNode)) {
                // Focus came from outside — redirect to inside after guard
                instance.afterInsideGuard?.focus();
            } else {
                // Focus came from inside (Tab past last element) — move to next tabbable in page
                const domReference = fms?.domReference ?? null;
                const nextTabbable = getNextTabbable(domReference, document.body, document.body);
                nextTabbable?.focus();

                if (fms?.closeOnFocusOut && instance.dotNetRef) {
                    instance.dotNetRef.invokeMethodAsync('OnPortalFocusOut');
                }
            }
        }

        instance.afterOutsideGuard.addEventListener('focus', handleAfterOutsideFocus);
        cleanups.push(() => {
            instance.afterOutsideGuard?.removeEventListener('focus', handleAfterOutsideFocus);
        });
    }

    instance.cleanup = () => {
        for (const fn of cleanups) fn();
    };
}

/**
 * Detaches all focus listeners from a portal instance.
 */
function detachPortalFocusListeners(instance) {
    instance.cleanup?.();
    instance.cleanup = null;
}

// ============================================================================
// Export state for debugging
// ============================================================================

export function getFloatingState() {
    return state;
}

/**
 * Removes the listed inline-style properties from an element. Mirrors the
 * React `clearStyles` helper used by the Select popup to reset height/max-height
 * inline styles applied during align-item-with-trigger sizing.
 *
 * @param {Element | null | undefined} element
 * @param {string[]} styleNames
 */
export function clearStyles(element, styleNames) {
    if (!element || !styleNames) {
        return;
    }

    for (const name of styleNames) {
        element.style.removeProperty(name);
    }
}
