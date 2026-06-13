export function copyText(text) {
  if (!navigator.clipboard) {
    return Promise.reject(new Error('Clipboard API unavailable'));
  }

  return navigator.clipboard.writeText(text);
}
