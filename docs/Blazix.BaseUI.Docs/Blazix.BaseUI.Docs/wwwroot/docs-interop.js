const SPY_KEY = Symbol.for('BlazixDocs.QuickNav.Spy');

export function copyText(text) {
  if (!navigator.clipboard) {
    return Promise.reject(new Error('Clipboard API unavailable'));
  }
  return navigator.clipboard.writeText(text);
}

export function setRenderMode(mode) {
  const normalized = mode === 'wasm' ? 'wasm' : 'server';
  const secure = location.protocol === 'https:' ? '; secure' : '';
  document.cookie = `blazix-docs-render-mode=${normalized}; path=/; max-age=31536000; samesite=lax${secure}`;
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

  let disposed = false;

  const observer = new IntersectionObserver((entries) => {
    if (disposed) {
      return;
    }
    const visible = entries
      .filter((entry) => entry.isIntersecting)
      .sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top);
    if (visible.length > 0) {
      dotnet.invokeMethodAsync('SetActiveHeading', visible[0].target.id);
    }
  }, { rootMargin: '-90px 0px -65% 0px', threshold: 0 });

  headings.forEach((heading) => observer.observe(heading));
  window[SPY_KEY] = {
    dispose() {
      disposed = true;
      observer.disconnect();
    },
  };
}

export function disconnectHeadings() {
  const existing = window[SPY_KEY];
  if (existing) {
    existing.dispose();
    delete window[SPY_KEY];
  }
}
