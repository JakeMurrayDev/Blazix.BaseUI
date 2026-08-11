/* Shared parity capture script. Injected verbatim into BOTH the React and the
 * Blazor page, so capture logic cannot drift between the two sides. */
(() => {
  const KEY = Symbol.for('Blazix.Parity.Capture');
  if (window[KEY]) return;

  // Harness deadlines remain live while target-action fallback timers are held for
  // deterministic frame screenshots.
  const nativeSetTimeout = window.setTimeout;
  const nativeClearTimeout = window.clearTimeout;

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
    'transition-delay', 'animation-name', 'animation-duration', 'animation-delay',
    'animation-iteration-count', 'animation-timing-function', 'animation-fill-mode',
  ];

  const BLAZIX_PREFIX = 'data-blazix-base-ui-';
  const UPSTREAM_PREFIX = 'data-base-ui-';
  // Covers a renderer/interop turn whose early ARIA consequence can precede later root
  // motion. Observed short animations are persisted by the probe during this window.
  const NON_TERMINAL_REGISTRATION_HOLD_MS = 200;
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

      // Popover needs these attributes internally for wiring and FOUC prevention,
      // but React's public DOM has no equivalent. Keep the exclusion exact so
      // semantic Base UI markers continue through the normal prefix mapping.
      if (name === 'data-blazix-base-ui-positioner' ||
          name === 'data-blazix-base-ui-popover-arrow' ||
          (name === 'data-positioned' && el.hasAttribute('data-blazix-base-ui-positioner'))) {
        continue;
      }

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
    // `--blazor-load-*` belongs to the WebAssembly boot progress UI. It is inherited
    // by every fixture node after WASM starts and has no React/Server counterpart; it
    // describes the harness host, not the component being compared.
    for (let i = 0; i < cs.length; i++) {
      const prop = cs.item(i);
      if (prop.startsWith('--') &&
          !prop.startsWith('--tw-') &&
          !prop.startsWith('--blazor-load-')) {
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

  /** Returns the exact labelled root order used by snapshots together with the viewport
   * intersection of every rendered box in that root. Portal containers commonly have no
   * box of their own while absolutely positioned descendants do, so the container rectangle
   * is not the screenshot contract. Opacity is deliberately ignored: an opacity transition's
   * zero endpoint is still a photographable frame. */
  function screenshotRoots() {
    const isRendered = (el) => {
      if (!el.isConnected || el.getClientRects().length === 0) return false;
      if (typeof el.checkVisibility === 'function') {
        return el.checkVisibility({ checkOpacity: false, checkVisibilityCSS: true });
      }
      const style = getComputedStyle(el);
      return style.display !== 'none' && style.visibility !== 'hidden' &&
        style.visibility !== 'collapse' && style.contentVisibility !== 'hidden';
    };

    return roots().map(({ label, el }) => {
      const elements = [el, ...el.querySelectorAll('*')];
      let left = Infinity;
      let top = Infinity;
      let right = -Infinity;
      let bottom = -Infinity;

      for (const candidate of elements) {
        if (!isRendered(candidate)) continue;
        for (const rect of candidate.getClientRects()) {
          if (rect.width <= 0 || rect.height <= 0) continue;
          left = Math.min(left, rect.left + window.scrollX);
          top = Math.min(top, rect.top + window.scrollY);
          right = Math.max(right, rect.right + window.scrollX);
          bottom = Math.max(bottom, rect.bottom + window.scrollY);
        }
      }

      if (!Number.isFinite(left) || right <= left || bottom <= top) {
        return { label, state: 'NotVisible', clip: null };
      }

      // Playwright's page clip is expressed in document coordinates, while a translated
      // animation endpoint can be wholly beyond the current viewport and therefore outside
      // the resulting image. Preserve the root's labelled slot but expose no clip until it
      // has photographable viewport pixels. A partially visible root is clipped to exactly
      // the intersection so screenshot capture cannot fail at the same boundary.
      left = Math.max(left, window.scrollX);
      top = Math.max(top, window.scrollY);
      right = Math.min(right, window.scrollX + window.innerWidth);
      bottom = Math.min(bottom, window.scrollY + window.innerHeight);

      if (right <= left || bottom <= top) {
        return { label, state: 'NotVisible', clip: null };
      }

      return {
        label,
        state: 'Captured',
        clip: { x: left, y: top, width: right - left, height: bottom - top },
      };
    });
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

  function completionVisible(el) {
    if (!el || !el.isConnected) return false;
    const style = getComputedStyle(el);
    const rect = el.getBoundingClientRect();
    return style.visibility !== 'hidden' && style.display !== 'none' &&
      rect.width > 0 && rect.height > 0;
  }

  /** Reads the complete all-of predicate set in one synchronous browser task.
   *
   * No promise, event callback, or framework round trip occurs inside this map. The DOM
   * therefore cannot advance between predicate observations, which is the distinction
   * between an all-of snapshot and several individually successful waits. */
  function completionSnapshot(predicates) {
    return predicates.map((predicate) => {
      const matches = document.querySelectorAll(predicate.selector.css);
      const el = matches[predicate.selector.index] ?? null;
      let actual = null;
      let complete = false;

      switch (predicate.kind) {
        case 'attached':
          actual = el !== null;
          complete = actual;
          break;
        case 'detached':
          actual = el === null;
          complete = actual;
          break;
        case 'visible':
          actual = completionVisible(el);
          complete = actual;
          break;
        case 'hidden':
          actual = el === null || !completionVisible(el);
          complete = actual;
          break;
        case 'attribute':
          actual = el?.getAttribute(predicate.name) ?? null;
          complete = actual === predicate.expected;
          break;
        case 'property':
          actual = el && predicate.name in el ? el[predicate.name] : null;
          complete = actual === predicate.expected;
          break;
        case 'input-value':
          actual = el && 'value' in el ? String(el.value) : null;
          complete = actual === predicate.expected;
          break;
        case 'focus-equals':
          actual = el !== null && document.activeElement === el;
          complete = actual;
          break;
        case 'focus-not-equals':
          actual = el !== null && document.activeElement !== el;
          // Absence is not "focus moved elsewhere": the declared focus target must
          // resolve so a missing alias or fixture node becomes typed completion evidence.
          complete = actual;
          break;
        default:
          throw new Error(`Unknown completion predicate kind '${predicate.kind}'.`);
      }

      return {
        complete,
        observed: JSON.stringify({
          matches: matches.length,
          actual: actual === null ? null : String(actual),
          activeTag: document.activeElement?.tagName?.toLowerCase() ?? null,
          element: el?.outerHTML ?? null,
        }).slice(0, 500),
      };
    });
  }

  const state = {
    timeline: [],
    observer: null,
    startedAt: 0,
    listeners: [],
    animationProbe: null,
    frameAnimations: null,
    // Original component animation -> clock/effect state captured when registration freezes
    // it. A temporary positive end delay keeps fraction 1 inside the animation's unfinished
    // timeline, so component-owned `animation.finished` promises cannot unmount the subtree.
    frameOriginals: null,
    frameLifecycleListeners: [],
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

  function captureAnimations(activeOnly = true) {
    const captureRoots = roots().map((root) => root.el);
    return document.getAnimations().filter((animation) => {
      const target = animation.effect?.target;
      if (!(target instanceof Element) || !captureRoots.some(
        (root) => root === target || root.contains(target))) {
        return false;
      }
      return !activeOnly || animation.pending || animation.playState === 'running';
    });
  }

  function captureHasStartingStyle() {
    return roots().some(({ el }) =>
      el.hasAttribute('data-starting-style') ||
      el.querySelector('[data-starting-style]') !== null);
  }

  function stopAnimationProbe() {
    state.animationProbe?.stop();
    state.animationProbe = null;
  }

  function freezeProbeTimers() {
    const timerPatch = state.animationProbe?.timerPatch;
    if (!timerPatch) return;
    for (const id of timerPatch.scheduled) {
      nativeClearTimeout.call(window, id);
    }
    timerPatch.scheduled.clear();
    timerPatch.restore();
  }

  function restoreAnimation(animation, before) {
    try {
      if (before.timing) animation.effect?.updateTiming(before.timing);
      if (before.playState === 'finished') {
        animation.finish();
      } else if (before.playState === 'idle') {
        animation.cancel();
      } else {
        if (typeof before.time === 'number') animation.currentTime = before.time;
        if (before.playState !== 'paused') animation.play();
      }
    } catch {
      // Cancelled, replaced, or detached while it was held. Nothing to restore.
    }
  }

  function blockFrameLifecycleEvents() {
    if (state.frameLifecycleListeners.length > 0) return;
    const stop = (event) => event.stopImmediatePropagation();
    for (const type of [
      'transitionend', 'transitioncancel', 'animationend', 'animationcancel',
    ]) {
      document.addEventListener(type, stop, true);
      state.frameLifecycleListeners.push([type, stop]);
    }
  }

  function releaseFrameLifecycleEvents() {
    for (const [type, listener] of state.frameLifecycleListeners) {
      document.removeEventListener(type, listener, true);
    }
    state.frameLifecycleListeners = [];
  }

  /** Freezes the component-owned clocks at the registration fixed point. The temporary
   * end delay leaves the original CSS transition in its native composite order — a script
   * clone is lower priority than a CSS transition — while keeping its finished promise
   * pending when the visual effect is seeked to its exact active-duration endpoint. */
  function freezeFrameAnimations(animations) {
    const originals = new Map();
    if (animations.length > 0) {
      freezeProbeTimers();
      blockFrameLifecycleEvents();
    }

    for (const animation of animations) {
      const timing = animation.effect?.getTiming?.();
      originals.set(animation, {
        time: animation.currentTime,
        playState: animation.playState,
        timing,
      });
      try { animation.pause(); } catch { /* a finished persisted animation can be immutable */ }
      if (timing) {
        const duration = typeof timing.duration === 'number' ? timing.duration : 0;
        animation.effect.updateTiming({
          endDelay: Math.max(1000, duration + Math.max(0, timing.endDelay ?? 0)),
          fill: 'both',
        });
      }
    }

    state.frameOriginals = originals;
    state.frameAnimations = animations;
    return animations;
  }

  function beginAnimationProbe(trackTimers = true) {
    stopAnimationProbe();
    state.frameAnimations = null;
    state.frameOriginals = null;

    const baseline = new Set(document.getAnimations());
    const observed = new Set();
    let frame = 0;
    let stopped = false;
    let timerPatch = null;

    if (trackTimers) {
      const scheduled = new Set();
      const patchedSetTimeout = (handler, timeout, ...args) => {
        const id = nativeSetTimeout.call(window, handler, timeout, ...args);
        scheduled.add(id);
        return id;
      };
      const patchedClearTimeout = (id) => {
        scheduled.delete(id);
        nativeClearTimeout.call(window, id);
      };
      const restore = () => {
        if (window.setTimeout === patchedSetTimeout) window.setTimeout = nativeSetTimeout;
        if (window.clearTimeout === patchedClearTimeout) window.clearTimeout = nativeClearTimeout;
      };
      window.setTimeout = patchedSetTimeout;
      window.clearTimeout = patchedClearTimeout;
      timerPatch = { scheduled, restore };
    }

    const scan = () => {
      if (stopped) return;
      for (const animation of captureAnimations(false)) {
        if (baseline.has(animation) || observed.has(animation)) continue;
        // Retain an observed short animation after it finishes so registration can still
        // select and seek it without pausing the live action while it is running.
        try { animation.persist?.(); } catch { /* optional browser optimization */ }
        observed.add(animation);
      }
    };

    const tick = () => {
      scan();
      if (!stopped) frame = requestAnimationFrame(tick);
    };

    const observer = new MutationObserver(scan);
    observer.observe(document.body, {
      attributes: true,
      characterData: true,
      childList: true,
      subtree: true,
    });
    const animationEvent = () => scan();
    document.addEventListener('transitionrun', animationEvent, true);
    document.addEventListener('animationstart', animationEvent, true);
    frame = requestAnimationFrame(tick);
    scan();

    state.animationProbe = {
      observed,
      timerPatch,
      scan,
      stop: () => {
        if (stopped) return;
        stopped = true;
        timerPatch?.restore();
        observer.disconnect();
        document.removeEventListener('transitionrun', animationEvent, true);
        document.removeEventListener('animationstart', animationEvent, true);
        if (frame) cancelAnimationFrame(frame);
      },
    };
  }

  window[KEY] = {
    /** Supplies screenshot capture with the same root labels and order as snapshots. */
    screenshotRoots() {
      return screenshotRoots();
    },

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
        screenshotObservations: [],
        actions: [],
        animationFrameCaptureFailures: [],
      };
    },

    startTimeline() {
      // The production capturer does start -> actions/completion/quiescence -> stop ->
      // capture for every step. Keep the teardown here as a defensive boundary too: an
      // interrupted step must not leave its observer and six listeners registered when
      // the next diagnostic attempt starts a fresh timeline.
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

    timelineActive() {
      return state.observer !== null || state.listeners.length > 0;
    },

    /** Waits for one atomic all-of snapshot under the action's shared deadline. */
    awaitCompletion(predicates, timeoutMs) {
      if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) {
        throw new Error(`Completion timeout must be positive; received ${timeoutMs}.`);
      }
      return new Promise((resolve) => {
        let deadline = 0;
        let frame = 0;
        let finished = false;
        const eventTypes = ['input', 'change', 'focusin', 'focusout'];

        const stop = () => {
          observer.disconnect();
          for (const type of eventTypes) document.removeEventListener(type, check, true);
          if (frame) cancelAnimationFrame(frame);
          nativeClearTimeout.call(window, deadline);
        };

        const finish = (value) => {
          if (finished) return;
          finished = true;
          stop();
          resolve(value);
        };

        const check = () => {
          if (finished) return;
          const snapshot = completionSnapshot(predicates);
          if (snapshot.every((item) => item.complete)) {
            finish({ completed: true, unmetIndex: -1, observed: '' });
            return;
          }
          if (frame) cancelAnimationFrame(frame);
          frame = requestAnimationFrame(check);
        };

        const observer = new MutationObserver(check);
        observer.observe(document.body, {
          attributes: true,
          characterData: true,
          childList: true,
          subtree: true,
        });
        for (const type of eventTypes) document.addEventListener(type, check, true);

        deadline = nativeSetTimeout.call(window, () => {
          const snapshot = completionSnapshot(predicates);
          const unmetIndex = snapshot.findIndex((item) => !item.complete);
          if (unmetIndex < 0) {
            finish({ completed: true, unmetIndex: -1, observed: '' });
            return;
          }
          finish({
            completed: false,
            unmetIndex,
            observed: snapshot[unmetIndex]?.observed ?? '{}',
          });
        }, timeoutMs);

        check();
      });
    },

    /** Waits for every finite animation under a captured root to reach a terminal state.
     *
     * The caller has already run the ordinary portal/mutation quiescence, which gives CSS
     * transitions a chance to register. A completed animation can synchronously cause a
     * component to replace it with another animation, so this is a fixed-point scan rather
     * than a one-time Promise.all. Infinite animations have no canonical terminal and are
     * deliberately excluded; the deterministic frame pass can still seek them. */
    awaitFiniteAnimations(timeoutMs, deadlineMs = timeoutMs) {
      if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) {
        throw new Error(`Animation timeout must be positive; received ${timeoutMs}.`);
      }

      return new Promise((resolve, reject) => {
        let finished = false;
        let waited = 0;
        let infinite = 0;
        const seen = new WeakSet();
        const captureRoots = roots().map((root) => root.el);

        const belongsToCapture = (animation) => {
          const target = animation.effect?.target;
          return target instanceof Element && captureRoots.some(
            (root) => root === target || root.contains(target));
        };

        const classify = () => {
          const finite = [];
          infinite = 0;

          for (const animation of document.getAnimations()) {
            if (!belongsToCapture(animation)) continue;
            const timing = animation.effect?.getComputedTiming?.();
            if (!Number.isFinite(timing?.endTime)) {
              infinite += 1;
              continue;
            }
            if (animation.pending || animation.playState === 'running' ||
                animation.playState === 'paused') {
              finite.push(animation);
            }
          }

          return finite;
        };

        const deadline = nativeSetTimeout.call(window, () => {
          if (finished) return;
          finished = true;
          const pending = classify().length;
          reject(new Error(
            `Timed out after ${deadlineMs}ms waiting for finite animations ` +
            `(pending: ${pending}, observed: ${waited}, infinite ignored: ${infinite}).`));
        }, timeoutMs);

        const scan = async () => {
          if (finished) return;
          const animations = classify();
          if (animations.length === 0) {
            finished = true;
            nativeClearTimeout.call(window, deadline);
            resolve({ waited, infinite });
            return;
          }

          for (const animation of animations) {
            if (!seen.has(animation)) {
              seen.add(animation);
              waited += 1;
            }
          }

          await Promise.allSettled(animations.map((animation) => animation.finished));
          queueMicrotask(scan);
        };

        scan();
      });
    },

    /** Waits until a replay action has registered motion or reached its declared consequence.
     *
     * The authoritative pass waits the full all-of consequence. A disposable frame replay
     * must stop earlier when that consequence is terminal (for example, a closing panel is
     * detached), otherwise there is no live animation left to seek. */
    awaitAnimationRegistration(predicates, timeoutMs) {
      if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) {
        throw new Error(`Animation registration timeout must be positive; received ${timeoutMs}.`);
      }

      return new Promise((resolve) => {
        let deadline = 0;
        let frame = 0;
        let finished = false;
        let completedFrames = 0;
        let activeFrames = 0;
        let previousActive = [];
        let completionReachedAt = null;
        if (!state.animationProbe) beginAnimationProbe();
        const hasTerminalCompletion = predicates.some(
          (predicate) => predicate.kind === 'detached' || predicate.kind === 'hidden');

        const activeAnimations = () => {
          const probe = state.animationProbe;
          probe?.scan();
          return probe ? [...probe.observed] : [];
        };

        const stop = () => {
          observer.disconnect();
          if (frame) cancelAnimationFrame(frame);
          nativeClearTimeout.call(window, deadline);
          stopAnimationProbe();
        };

        const finish = (animations) => {
          if (finished) return;
          finished = true;
          const drivers = freezeFrameAnimations(animations);
          stop();
          resolve(drivers.length);
        };

        const check = (fromFrame = false) => {
          if (finished) return;
          const animations = activeAnimations();
          const startingStylePending = captureHasStartingStyle();
          const completionReached = predicates.length === 0 ||
            completionSnapshot(predicates).every((item) => item.complete);
          if (completionReached && completionReachedAt === null) {
            completionReachedAt = performance.now();
          }
          // Completion is a consequence milestone, not necessarily a terminal state. A
          // replacement animation can legitimately remove the first attached node after
          // satisfying that milestone; do not make the fixed point impossible by revoking it.
          const completionWasReached = completionReachedAt !== null;
          const nonTerminalFixedPointReached = predicates.length === 0 ||
            hasTerminalCompletion ||
            (completionWasReached &&
             performance.now() - completionReachedAt >= NON_TERMINAL_REGISTRATION_HOLD_MS);
          if (animations.length > 0) {
            const sameSet = animations.length === previousActive.length &&
              animations.every((animation, index) => animation === previousActive[index]);
            previousActive = animations;
            completedFrames = 0;

            // Non-terminal consequences (checked state, expanded state, visible popup)
            // are part of the same action and can register later root motion after an
            // earlier descendant animation. Terminal consequences cannot be awaited here:
            // a detached closing popup would leave nothing to photograph.
            if (!startingStylePending &&
                (hasTerminalCompletion ||
                 (completionWasReached && nonTerminalFixedPointReached)) &&
                fromFrame && sameSet && ++activeFrames >= 2) {
              finish(animations);
              return;
            }
            if (startingStylePending || !sameSet ||
                (!hasTerminalCompletion &&
                 (!completionWasReached || !nonTerminalFixedPointReached))) activeFrames = 0;
          } else {
            previousActive = [];
            activeFrames = 0;
          }

          if (!startingStylePending && animations.length === 0 && predicates.length > 0 &&
              completionWasReached && nonTerminalFixedPointReached) {
            // An attribute can be published one rendering turn before its CSS transition
            // is registered. Four quiet frames keep an empty result from winning that race
            // while remaining far below the action deadline.
            if (fromFrame && ++completedFrames >= 4) {
              finish([]);
              return;
            }
          } else {
            completedFrames = 0;
          }

          if (frame) cancelAnimationFrame(frame);
          frame = requestAnimationFrame(() => check(true));
        };

        const observer = new MutationObserver(check);
        observer.observe(document.body, {
          attributes: true,
          characterData: true,
          childList: true,
          subtree: true,
        });

        deadline = nativeSetTimeout.call(window, () => finish([]), timeoutMs);
        check();
      });
    },

    /** Starts observing animations created by the next action before it is dispatched. */
    beginAnimationProbe(trackTimers = true) {
      beginAnimationProbe(trackTimers);
    },

    /** Selects currently active capture-root motion for an actionless animation step. */
    selectCurrentAnimations() {
      stopAnimationProbe();
      return freezeFrameAnimations(captureAnimations(true)).length;
    },

    /** Pauses the selected capture-root animations and seeks to `fraction` of their duration.
     *
     * A replay action establishes the selection before dispatch, excluding already-running
     * page motion and animations outside the captured trees. An actionless animation step
     * explicitly selects the currently active capture-root set. Direct callers that do
     * neither get the same active capture-root fallback. The selected set is retained across
     * all five fractions even though the first seek pauses it. */
    seekAnimations(fraction) {
      const animations = state.seeked
        ? [...state.seeked.keys()]
        : state.frameAnimations ?? captureAnimations(true);

      // Gated on there being something to seek, not merely on this being the first seek.
      // An empty selected set intentionally produces no frame files; arming seek state for
      // it would make a later direct call inherit an empty operation it never selected.
      if (animations.length > 0 && !state.seeked) {
        state.seeked = new Map();

        // The step's timeline has already been read by capture(), so everything from here
        // on is harness time and belongs in no step's record. It has to be silenced by
        // detaching rather than by a flag: seeking an animation to its end and then back
        // to where it was crosses two phase boundaries, the browser reports each as an
        // animationend / animationstart, and the one caused by the resume is dispatched a
        // frame AFTER resumeAnimations() has restored the clocks — past any flag the resume
        // could clear.
        //
        // Detaching alone does not finish the job, because the recording does not stay
        // detached: the next step's startTimeline() re-arms it, and the capturer reaches
        // that after one aria-snapshot round trip — shorter than the frame the resume's
        // event waits for. The event was therefore recorded after all, one step downstream
        // rather than in place, which two consecutive animation steps (a popup that opens
        // and then closes) produce as a matter of course. What actually closes it is
        // resumeAnimations() holding its promise open for two animation frames, so the
        // event is dispatched while the recording is still detached and lands nowhere.
        teardownTimeline();
      }
      for (const a of animations) {
        // Armed here as well as above, so that the null check below rests on this loop and
        // not on a condition thirteen lines away. Nothing can reach it unarmed today — the
        // loop body runs only when animations.length > 0, which is half of the arming
        // condition — but that couples the two halves, and an edit to either would break
        // the other silently.
        state.seeked ??= new Map();
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
     * transition would then run again in the middle of the next step. Resolves to the number
     * of animations restored, which is zero when the step never seeked. */
    async resumeAnimations() {
      const seeked = state.seeked;
      const originals = state.frameOriginals;
      state.seeked = null;
      state.frameAnimations = null;
      state.frameOriginals = null;
      releaseFrameLifecycleEvents();
      if (!seeked && !originals) return 0;

      if (originals) {
        for (const [animation, before] of originals) {
          restoreAnimation(animation, before);
        }
      } else {
        for (const [animation, before] of seeked) {
          restoreAnimation(animation, before);
        }
      }

      // Held open past the restoration rather than returning as soon as the clocks are set.
      // The phase crossing the restoration causes is reported a frame later, and the caller
      // re-arms the recording after one round trip, which is shorter than that frame — so
      // returning here would file the harness's own animationstart in the next step's
      // timeline, on the one comparator whose entire subject is animation. Awaited, the
      // event is dispatched while the recording is still detached and is recorded nowhere.
      //
      // The timer is the guard SettleProtocol's quiesce loop carries and for the same
      // reason: a throttled or backgrounded page stops servicing animation frames, and a
      // promise only a frame callback can settle would hang the awaiting evaluate with no
      // diagnostic.
      await new Promise((resolve) => {
        const done = () => { nativeClearTimeout.call(window, deadline); resolve(); };
        const deadline = nativeSetTimeout.call(window, done, 250);
        requestAnimationFrame(() => requestAnimationFrame(done));
      });

      return originals?.size ?? seeked.size;
    },

    settled() {
      const root = document.querySelector('[data-parity-root]');
      return root !== null && root.getAttribute('data-interactive') === 'true';
    },
  };
})();
