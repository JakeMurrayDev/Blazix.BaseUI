const SPY_KEY = Symbol.for('BlazixDocs.QuickNav.Spy');
const ACTIVE_OFFSET = 96;
const BOTTOM_THRESHOLD = 2;

export function observeHeadings(dotnet, ids) {
  disconnectHeadings();

  const headings = ids
    .map((id) => document.getElementById(id))
    .filter(Boolean);

  if (headings.length === 0) {
    return;
  }

  let disposed = false;
  let animationFrame = 0;

  const updateActiveHeading = () => {
    animationFrame = 0;

    if (disposed) {
      return;
    }

    const documentElement = document.documentElement;
    const scrollTop = window.scrollY || documentElement.scrollTop;
    const viewportHeight = window.innerHeight || documentElement.clientHeight;
    const documentHeight = Math.max(documentElement.scrollHeight, document.body.scrollHeight);

    if (scrollTop + viewportHeight >= documentHeight - BOTTOM_THRESHOLD) {
      invokeSetActiveHeading(dotnet, headings[headings.length - 1].id);
      return;
    }

    let activeHeading = headings[0];
    for (const heading of headings) {
      if (heading.getBoundingClientRect().top > ACTIVE_OFFSET) {
        break;
      }

      activeHeading = heading;
    }

    invokeSetActiveHeading(dotnet, activeHeading.id);
  };

  const scheduleUpdate = () => {
    if (disposed || animationFrame) {
      return;
    }

    animationFrame = requestAnimationFrame(updateActiveHeading);
  };

  const observer = new IntersectionObserver(scheduleUpdate);

  headings.forEach((heading) => observer.observe(heading));
  window.addEventListener('scroll', scheduleUpdate, { passive: true });
  window.addEventListener('resize', scheduleUpdate);
  window.addEventListener('hashchange', scheduleUpdate);
  scheduleUpdate();

  window[SPY_KEY] = {
    dispose() {
      disposed = true;
      if (animationFrame) {
        cancelAnimationFrame(animationFrame);
      }

      observer.disconnect();
      window.removeEventListener('scroll', scheduleUpdate);
      window.removeEventListener('resize', scheduleUpdate);
      window.removeEventListener('hashchange', scheduleUpdate);
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

function invokeSetActiveHeading(dotnet, id) {
  dotnet.invokeMethodAsync('SetActiveHeading', id).catch(() => {});
}
