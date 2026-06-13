export function setRenderMode(mode) {
  const normalized = mode === 'wasm' ? 'wasm' : 'server';
  const secure = location.protocol === 'https:' ? '; secure' : '';
  document.cookie = `blazix-docs-render-mode=${normalized}; path=/; max-age=31536000; samesite=lax${secure}`;
  location.reload();
}
