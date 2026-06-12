const SPY_KEY = Symbol.for('BlazixDocs.QuickNav.Spy');

export function copyText(text) {
  return navigator.clipboard.writeText(text);
}

export function setRenderMode(mode) {
  const normalized = mode === 'wasm' ? 'wasm' : 'server';
  document.cookie = `blazix-docs-render-mode=${normalized}; path=/; max-age=31536000; samesite=lax`;
  location.reload();
}

export function highlightElement(element) {
  if (element && window.hljs) {
    delete element.dataset.highlighted;
    window.hljs.highlightElement(element);
  }
}

export function observeHeadings(dotnet, ids) {
  disconnectHeadings();

  const headings = ids
    .map((id) => document.getElementById(id))
    .filter(Boolean);

  if (headings.length === 0) {
    return;
  }

  const observer = new IntersectionObserver((entries) => {
    const visible = entries.filter((entry) => entry.isIntersecting);
    if (visible.length > 0) {
      dotnet.invokeMethodAsync('SetActiveHeading', visible[0].target.id);
    }
  }, { rootMargin: '-90px 0px -65% 0px', threshold: 0 });

  headings.forEach((heading) => observer.observe(heading));
  window[SPY_KEY] = observer;
}

export function disconnectHeadings() {
  const existing = window[SPY_KEY];
  if (existing) {
    existing.disconnect();
    delete window[SPY_KEY];
  }
}
