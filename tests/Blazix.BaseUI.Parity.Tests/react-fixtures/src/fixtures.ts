import type { ComponentType } from 'react';

export interface Fixture {
  id: string;
  Component: ComponentType<unknown>;
}

// `base-ui-demos` is the alias vite.config.mts points at the checkout's demo
// directory. The specifier is deliberately bare: Vite joins a glob beginning
// with `/` onto the project root without consulting resolve.alias, so a rooted
// spelling silently matches nothing.
const modules = import.meta.glob<{ default: ComponentType<unknown> }>(
  'base-ui-demos/*/demos/*/tailwind/index.tsx',
  { eager: true },
);

// Keys arrive relative to the Vite root, so they carry a `../` prefix whose
// depth depends on where the checkout sits. Only the trailing segments —
// `<component>/demos/<demo>/tailwind/index.tsx` — are stable, so match on those.
const idPattern = /([^/]+)\/demos\/([^/]+)\/tailwind\/index\.tsx$/;

export const fixtures: Fixture[] = Object.entries(modules)
  .map(([path, mod]) => {
    const match = idPattern.exec(path);
    if (!match) {
      throw new Error(`Unexpected demo path: ${path}`);
    }
    return { id: `${match[1]}/${match[2]}`, Component: mod.default };
  })
  .sort((a, b) => a.id.localeCompare(b.id));

export const fixtureById = new Map(fixtures.map((f) => [f.id, f]));
