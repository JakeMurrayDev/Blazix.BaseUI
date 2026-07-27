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

  /** Assigns stable #idN symbols in document order across every root.
   *
   * One table spanning all roots, never one per root: a trigger inside the fixture
   * root routinely points at a popup portalled to <body>, and a per-root table
   * cannot see across that boundary — it would leave aria-controls holding a raw
   * Blazor GUID or React useId, which differs on every run and between legs. The
   * counter has to span the roots too, or #id1 would name a different node in each
   * tree. */
  function buildIdTable(rootList) {
    const table = new Map();
    let n = 0;
    const walk = (el) => {
      if (el.id && !table.has(el.id)) table.set(el.id, `#id${++n}`);
      for (const child of el.children) walk(child);
    };
    for (const { el } of rootList) walk(el);
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

  /** Builds a node's path within `root`, prefixed with that root's label.
   *
   * The label is a segment of its own rather than a separate field, so the root
   * element itself gets a non-empty path instead of ''. Without it every root
   * writes its own geometry to the same key, and the moment a popup portals to
   * <body> the fixture tree's boxes — the data popup positioning is compared on —
   * are overwritten by the portal's. */
  function nodePath(el, root, label) {
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
    segments.unshift(label);
    return segments.join(' > ');
  }

  function readAttributes(el, idTable) {
    const out = {};
    for (const attr of el.attributes) {
      let name = attr.name;

      // class and style are covered by `classes` and by the computed-style
      // comparator respectively; diffing them textually produces false positives.
      if (name === 'class' || name === 'style') continue;

      // Blazor render-tree bookkeeping, never present on the React side. `b-*` is the
      // scoped-CSS marker; `_bl_<referenceCaptureId>` is what the browser renderer
      // stamps on for every AddElementReferenceCapture, which RenderElement issues on
      // EVERY element the library renders. The id is regenerated per run, so leaving
      // these in makes every Blazix element diff, and diff differently each time.
      if (name.startsWith('b-') || name.startsWith('_bl_')) continue;

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
    //
    // `--tw-*` is excluded deliberately. Tailwind v4 registers dozens of custom
    // properties via @property, so every element inherits a large map of engine
    // internals. Capturing them would bloat every baseline and bury the
    // properties this harness actually compares — the ones base-ui exposes
    // (--anchor-width, --available-width/height, --transform-origin,
    // --positioner-*) — under Tailwind's bookkeeping. Both sides load the same
    // stylesheet, so the excluded values are identical by construction anyway.
    for (let i = 0; i < cs.length; i++) {
      const prop = cs.item(i);
      if (prop.startsWith('--') && !prop.startsWith('--tw-')) {
        out[prop] = cs.getPropertyValue(prop).trim();
      }
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

  /** The trees a capture spans, each paired with the label that namespaces its paths.
   *
   * The labels have to mean the same thing on both legs, whose <body> contents
   * otherwise differ. `root` is resolved by attribute, so it never depends on where
   * the framework puts the fixture host. Portal labels number only the elements that
   * survive the SCRIPT / data-parity-ignore filter — framework chrome (Blazor's
   * blazor.web.js tag, Vite's module tag) is excluded — leaving just the containers a
   * fixture portals out, which both legs append to <body> in the order the fixture
   * opens them. portal(1) is therefore the same logical portal on both sides. */
  function roots() {
    const primary = document.querySelector('[data-parity-root]');
    const list = primary ? [{ label: 'root', el: primary }] : [];
    // Portalled content mounts outside the fixture root.
    let portals = 0;
    for (const el of document.body.children) {
      if (el !== primary && !el.hasAttribute('data-parity-ignore') && el.tagName !== 'SCRIPT') {
        list.push({ label: `portal(${++portals})`, el });
      }
    }
    return list;
  }

  /** Resolves a labelled path for an element reached outside a snapshot walk — focus
   * and the timeline observe the whole document, not one root. */
  function pathIn(rootList, el) {
    const owner = rootList.find((r) => r.el.contains(el));
    return owner
      ? nodePath(el, owner.el, owner.label)
      : nodePath(el, document.body, 'body');
  }

  function snapshot(root, idTable, styles, customProps, geometry) {
    const build = (el) => {
      const path = nodePath(el, root.el, root.label);
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
    return build(root.el);
  }

  const state = {
    timeline: [],
    observer: null,
    startedAt: 0,
    listeners: [],
  };

  function teardownTimeline() {
    state.observer?.disconnect();
    state.observer = null;
    for (const [type, handler] of state.listeners) {
      document.removeEventListener(type, handler, true);
    }
    state.listeners = [];
  }

  window[KEY] = {
    capture(stepName) {
      const styles = {};
      const customProps = {};
      const geometry = {};
      const rootList = roots();
      const idTable = buildIdTable(rootList);
      const trees = rootList.map((root) => snapshot(root, idTable, styles, customProps, geometry));

      const active = document.activeElement;
      const activeRoot = rootList.find((r) => r.el.contains(active));

      return {
        step: stepName,
        dom: trees.length === 1 ? trees[0] : { tag: '#roots', path: '', attributes: {}, classes: [], text: '', children: trees },
        styles,
        customProps,
        geometry,
        focus: active && activeRoot ? nodePath(active, activeRoot.el, activeRoot.label) : null,
        timeline: state.timeline.slice(),
      };
    },

    startTimeline() {
      // capture() returns the timeline without stopping it, so a per-step runner
      // naturally does start -> capture -> start. Without this teardown the previous
      // observer stays connected and a second copy of all six listeners is registered,
      // so every event lands in state.timeline once per live registration.
      teardownTimeline();
      state.timeline = [];
      state.startedAt = performance.now();
      const at = () => Math.round(performance.now() - state.startedAt);

      state.observer = new MutationObserver((records) => {
        for (const r of records) {
          if (r.type === 'attributes') {
            const el = r.target;
            state.timeline.push({
              t: at(),
              kind: 'attribute',
              path: pathIn(roots(), el),
              attr: r.attributeName,
              from: r.oldValue,
              to: el.getAttribute(r.attributeName),
            });
          } else if (r.type === 'childList') {
            for (const n of r.addedNodes) {
              if (n.nodeType === Node.ELEMENT_NODE) {
                state.timeline.push({ t: at(), kind: 'added', path: pathIn(roots(), n), attr: null, from: null, to: n.tagName.toLowerCase() });
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
          state.timeline.push({
            t: at(), kind: type, path: pathIn(roots(), e.target),
            attr: e.propertyName ?? e.animationName ?? null, from: null, to: null,
          });
        };
        document.addEventListener(type, handler, true);
        state.listeners.push([type, handler]);
      }
    },

    stopTimeline() {
      teardownTimeline();
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
