import type { ComponentType } from 'react';
import Canary from './canary';

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

// The deliberately broken canary is reachable by its reserved harness URL but
// stays out of `fixtures`, which is the ordinary fixture list and denominator.
const canary: Fixture = { id: 'harness/canary', Component: Canary };

export const fixtureById = new Map([
  ...fixtures.map((fixture): [string, Fixture] => [fixture.id, fixture]),
  [canary.id, canary] as [string, Fixture],
]);
