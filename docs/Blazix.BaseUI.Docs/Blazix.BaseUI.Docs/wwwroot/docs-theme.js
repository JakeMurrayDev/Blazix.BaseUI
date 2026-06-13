const storageKey = 'blazix-docs-theme';

export function getDocsTheme() {
  const stored = localStorage.getItem(storageKey);
  const theme = stored === 'dark' || stored === 'light'
    ? stored
    : (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');

  applyTheme(theme);
  return theme;
}

export function setDocsTheme(theme) {
  const normalizedTheme = theme === 'dark' ? 'dark' : 'light';
  localStorage.setItem(storageKey, normalizedTheme);
  applyTheme(normalizedTheme);
}

export function afterPaint() {
  return new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));
}

function applyTheme(theme) {
  document.documentElement.classList.toggle('dark', theme === 'dark');
  document.documentElement.dataset.theme = theme;
}
