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
    // Animation -> { time, playState } as it was before the first seek of this step.
    seeked: null,
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

    /** Pauses every running animation on the page and seeks to `fraction` of its duration.
     *
     * Each animation's state is recorded the first time it is seen, because the page
     * outlives the step: one runner drives every step of a fixture on the same page, and an
     * animation left paused at the fraction the last frame asked for would hold the popup
     * in that pose for every step that follows — the DOM, the styles, the geometry, and the
     * screenshots of all of them. resumeAnimations() undoes it, and the recording is what
     * lets it undo rather than guess. New animations that appear between two seeks of the
     * same step are recorded when they are first seen, so they are restored too. */
    seekAnimations(fraction) {
      const animations = document.getAnimations();

      // Gated on there being something to seek, not merely on this being the first seek of
      // the step. A step whose animation has already finished by the time the settle
      // protocol lets go — a short transition, which is most of them — reaches here with an
      // empty list, and a caller that finds nothing to seek has no frames to take and so
      // never reaches resumeAnimations(), the only thing that clears state.seeked. Arming
      // it here would leave it armed for the life of the page, and the NEXT animation
      // step's first seek would take the `state.seeked` branch, skip the teardown below,
      // and record its own seeking into that step's timeline.
      if (animations.length > 0 && !state.seeked) {
        state.seeked = new Map();

        // The step's timeline has already been read by capture(), so everything from here
        // on is harness time and belongs in no step's record. It has to be silenced by
        // detaching rather than by a flag: seeking an animation to its end and then back
        // to where it was crosses two phase boundaries, the browser reports each as an
        // animationend / animationstart, and the one caused by the resume is dispatched a
        // frame AFTER resumeAnimations() has returned — past any flag the resume could
        // clear. The next step's startTimeline() re-arms the recording.
        teardownTimeline();
      }
      for (const a of animations) {
        if (!state.seeked.has(a)) {
          state.seeked.set(a, { time: a.currentTime, playState: a.playState });
        }
        const timing = a.effect?.getComputedTiming?.();
        const duration = typeof timing?.duration === 'number' ? timing.duration : 0;
        a.pause();
        a.currentTime = duration * fraction;
      }
      return animations.length;
    },

    /** Puts every animation seekAnimations() touched back where it was.
     *
     * The recorded time is restored rather than the animation being finished or replayed.
     * Finishing would end a transition the component has not ended, which is a state the
     * page would never have reached on its own; play() alone would rewind, because the spec
     * makes play() on an animation sitting at its end seek back to zero, and the whole
     * transition would then run again in the middle of the next step. Returns the number
     * of animations restored, which is zero when the step never seeked. */
    resumeAnimations() {
      const seeked = state.seeked;
      state.seeked = null;
      if (!seeked) return 0;

      for (const [a, before] of seeked) {
        try {
          if (before.playState === 'finished') {
            a.finish();
          } else if (before.playState === 'idle') {
            // It was not running and had no time to restore; pausing it gave it one.
            a.cancel();
          } else {
            if (typeof before.time === 'number') a.currentTime = before.time;
            if (before.playState !== 'paused') a.play();
          }
        } catch {
          // Cancelled, replaced, or detached while it was held. Nothing to put back, and
          // throwing here would abandon every animation after it in the map.
        }
      }

      return seeked.size;
    },

    settled() {
      const root = document.querySelector('[data-parity-root]');
      return root !== null && root.getAttribute('data-interactive') === 'true';
    },
  };
})();
