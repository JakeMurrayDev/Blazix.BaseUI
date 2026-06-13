export function highlightElement(element) {
  if (element && window.hljs) {
    delete element.dataset.highlighted;
    window.hljs.highlightElement(element);
  }
}
