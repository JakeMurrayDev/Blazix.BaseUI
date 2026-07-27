/* Shared parity capture script. Injected verbatim into BOTH the React and the
 * Blazor page, so capture logic cannot drift between the two sides. */
(() => {
  const KEY = Symbol.for('Blazix.Parity.Capture');
  if (window[KEY]) return;

  const STYLE_PROPS = [
    'display', 'position', 'top', 'right', 'bottom', 'left', 'z-index', 'isolation',
    'width', 'height', 'min-width', 'min-height', 'max-width', 'max-height',
    'margin-top', 'margin-right', 'margin-bottom', 'margin-left',
    'padding-top', 'padding-right', 'padding-bottom', 'padding-left',
    'box-sizing', 'overflow-x', 'overflow-y', 'visibility', 'opacity',
    'flex-direction', 'flex-wrap', 'justify-content', 'align-items', 'align-self',
    'flex-grow', 'flex-shrink', 'flex-basis', 'gap', 'order',
    'grid-template-columns', 'grid-template-rows', 'grid-column', 'grid-row',
    'font-family', 'font-size', 'font-weight', 'font-style', 'line-height',
    'letter-spacing', 'text-align', 'text-decoration-line', 'text-transform',
    'white-space', 'color', 'background-color', 'background-image',
    'border-top-width', 'border-right-width', 'border-bottom-width', 'border-left-width',
    'border-top-color', 'border-top-style', 'border-radius',
    'box-shadow', 'outline-width', 'outline-style', 'outline-color', 'outline-offset',
    'transform', 'transform-origin', 'pointer-events', 'cursor',
    'transition-property', 'transition-duration', 'transition-timing-function',
    'transition-delay', 'animation-name', 'animation-duration',
    'animation-timing-function', 'animation-fill-mode',
  ];

  const BLAZIX_PREFIX = 'data-blazix-base-ui-';
  const UPSTREAM_PREFIX = 'data-base-ui-';
  const ID_REF_ATTRS = [
    'aria-labelledby', 'aria-controls', 'aria-describedby',
    'aria-activedescendant', 'aria-details', 'aria-owns', 'for', 'headers',
  ];

  /** Assigns stable #idN symbols in document order. */
  function buildIdTable(root) {
    const table = new Map();
    let n = 0;
    const walk = (el) => {
      if (el.id && !table.has(el.id)) table.set(el.id, `#id${++n}`);
      for (const child of el.children) walk(child);
    };
    walk(root);
    return table;
  }

  function symbolize(table, value) {
    // id-reference attributes are space-separated token lists.
    return value
      .split(/\s+/)
      .filter((t) => t.length > 0)
      .map((t) => table.get(t) ?? t)
      .join(' ');
  }

  function nodePath(el, root) {
    const segments = [];
    let node = el;
    while (node && node !== root) {
      const parent = node.parentElement;
      if (!parent) break;
      const tag = node.tagName.toLowerCase();
      const role = node.getAttribute('role');
      const siblings = Array.from(parent.children).filter(
        (c) => c.tagName === node.tagName && c.getAttribute('role') === role,
      );
      const idx = siblings.indexOf(node);
      const rolePart = role ? `[role=${role}]` : '';
      const idxPart = siblings.length > 1 ? `:nth(${idx})` : '';
      segments.unshift(`${tag}${rolePart}${idxPart}`);
      node = parent;
    }
    return segments.join(' > ');
  }

  function readAttributes(el, idTable) {
    const out = {};
    for (const attr of el.attributes) {
      let name = attr.name;

      // class and style are covered by `classes` and by the computed-style
      // comparator respectively; diffing them textually produces false positives.
      if (name === 'class' || name === 'style') continue;

      // Blazor render-tree bookkeeping, never present on the React side.
      if (name.startsWith('b-') || name === 'blazor:elementreference') continue;

      // Prefixed Blazix markers are renamed to their upstream spelling. The rule
      // is idempotent: already-unprefixed markers pass through untouched.
      if (name.startsWith(BLAZIX_PREFIX)) {
        name = UPSTREAM_PREFIX + name.slice(BLAZIX_PREFIX.length);
      }

      let value = attr.value;
      if (name === 'id') value = idTable.get(attr.value) ?? attr.value;
      else if (ID_REF_ATTRS.includes(name)) value = symbolize(idTable, attr.value);

      out[name] = value;
    }
    return out;
  }

  function readStyles(el) {
    const cs = getComputedStyle(el);
    const out = {};
    for (const prop of STYLE_PROPS) out[prop] = cs.getPropertyValue(prop).trim();
    return out;
  }

  function readCustomProps(el) {
    const cs = getComputedStyle(el);
    const out = {};
    // computedStyleMap is not available for custom properties in all engines;
    // enumerate the declared set from the element's own cascade instead.
    for (let i = 0; i < cs.length; i++) {
      const prop = cs.item(i);
      if (prop.startsWith('--')) out[prop] = cs.getPropertyValue(prop).trim();
    }
    return out;
  }

  function readGeometry(el) {
    const r = el.getBoundingClientRect();
    return {
      x: Math.round(r.x * 100) / 100,
      y: Math.round(r.y * 100) / 100,
      width: Math.round(r.width * 100) / 100,
      height: Math.round(r.height * 100) / 100,
    };
  }

  function roots() {
    const primary = document.querySelector('[data-parity-root]');
    const list = primary ? [primary] : [];
    // Portalled content mounts outside the fixture root.
    for (const el of document.body.children) {
      if (el !== primary && !el.hasAttribute('data-parity-ignore') && el.tagName !== 'SCRIPT') {
        list.push(el);
      }
    }
    return list;
  }

  function snapshot(root, idTable, styles, customProps, geometry) {
    const build = (el) => {
      const path = nodePath(el, root);
      styles[path] = readStyles(el);
      customProps[path] = readCustomProps(el);
      geometry[path] = readGeometry(el);

      const text = Array.from(el.childNodes)
        .filter((n) => n.nodeType === Node.TEXT_NODE)
        .map((n) => n.textContent.replace(/\s+/g, ' ').trim())
        .filter((t) => t.length > 0)
        .join(' ');

      return {
        tag: el.tagName.toLowerCase(),
        path,
        attributes: readAttributes(el, idTable),
        classes: Array.from(el.classList).sort(),
        text,
        children: Array.from(el.children).map(build),
      };
    };
    return build(root);
  }

  const state = {
    timeline: [],
    observer: null,
    startedAt: 0,
    listeners: [],
  };

  window[KEY] = {
    capture(stepName) {
      const styles = {};
      const customProps = {};
      const geometry = {};
      const trees = roots().map((root) => {
        const idTable = buildIdTable(root);
        return snapshot(root, idTable, styles, customProps, geometry);
      });

      const active = document.activeElement;
      const activeRoot = roots().find((r) => r.contains(active));

      return {
        step: stepName,
        dom: trees.length === 1 ? trees[0] : { tag: '#roots', path: '', attributes: {}, classes: [], text: '', children: trees },
        styles,
        customProps,
        geometry,
        focus: active && activeRoot ? nodePath(active, activeRoot) : null,
        timeline: state.timeline.slice(),
      };
    },

    startTimeline() {
      state.timeline = [];
      state.startedAt = performance.now();
      const at = () => Math.round(performance.now() - state.startedAt);

      state.observer = new MutationObserver((records) => {
        for (const r of records) {
          if (r.type === 'attributes') {
            const el = r.target;
            const root = roots().find((x) => x.contains(el)) ?? document.body;
            state.timeline.push({
              t: at(),
              kind: 'attribute',
              path: nodePath(el, root),
              attr: r.attributeName,
              from: r.oldValue,
              to: el.getAttribute(r.attributeName),
            });
          } else if (r.type === 'childList') {
            for (const n of r.addedNodes) {
              if (n.nodeType === Node.ELEMENT_NODE) {
                state.timeline.push({ t: at(), kind: 'added', path: nodePath(n, document.body), attr: null, from: null, to: n.tagName.toLowerCase() });
              }
            }
            for (const n of r.removedNodes) {
              if (n.nodeType === Node.ELEMENT_NODE) {
                state.timeline.push({ t: at(), kind: 'removed', path: '', attr: null, from: n.tagName.toLowerCase(), to: null });
              }
            }
          }
        }
      });

      state.observer.observe(document.body, {
        attributes: true,
        attributeOldValue: true,
        subtree: true,
        childList: true,
      });

      for (const type of ['transitionstart', 'transitionend', 'transitioncancel',
                          'animationstart', 'animationend', 'animationcancel']) {
        const handler = (e) => {
          const root = roots().find((x) => x.contains(e.target)) ?? document.body;
          state.timeline.push({
            t: at(), kind: type, path: nodePath(e.target, root),
            attr: e.propertyName ?? e.animationName ?? null, from: null, to: null,
          });
        };
        document.addEventListener(type, handler, true);
        state.listeners.push([type, handler]);
      }
    },

    stopTimeline() {
      state.observer?.disconnect();
      state.observer = null;
      for (const [type, handler] of state.listeners) {
        document.removeEventListener(type, handler, true);
      }
      state.listeners = [];
      return state.timeline.slice();
    },

    /** Pauses every running animation on the page and seeks to `fraction` of its duration. */
    seekAnimations(fraction) {
      const animations = document.getAnimations();
      for (const a of animations) {
        const timing = a.effect?.getComputedTiming?.();
        const duration = typeof timing?.duration === 'number' ? timing.duration : 0;
        a.pause();
        a.currentTime = duration * fraction;
      }
      return animations.length;
    },

    settled() {
      const root = document.querySelector('[data-parity-root]');
      return root !== null && root.getAttribute('data-interactive') === 'true';
    },
  };
})();
