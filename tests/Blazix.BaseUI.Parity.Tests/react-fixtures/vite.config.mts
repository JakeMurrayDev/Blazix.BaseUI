import { existsSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

const here = dirname(fileURLToPath(import.meta.url));

const DEMOS_SUBPATH = 'docs/src/app/(docs)/react/components';

function locateBaseUi(): string {
  const override = process.env.PARITY_BASE_UI_PATH;
  if (override) return override;

  let dir = here;
  for (let i = 0; i < 12; i++) {
    const candidate = join(dir, '.base-ui');
    if (existsSync(join(candidate, 'packages/react'))) return candidate;
    const parent = dirname(dir);
    if (parent === dir) break;
    dir = parent;
  }

  throw new Error('Could not locate .base-ui. Set PARITY_BASE_UI_PATH.');
}

const baseUi = locateBaseUi();

// Validate the alias target, not just the checkout root — the override branch
// above returns without any check. An alias pointing at a nonexistent directory
// makes import.meta.glob match zero demos, which builds a bundle with no
// fixtures instead of failing.
if (!existsSync(resolve(baseUi, DEMOS_SUBPATH))) {
  throw new Error(
    `base-ui checkout at ${baseUi} has no demo directory at ${DEMOS_SUBPATH}. ` +
      'Check PARITY_BASE_UI_PATH, or the upstream docs layout may have moved.',
  );
}

// The React leg is only a reference implementation if it runs the React release
// upstream pins. That is the checkout's own copy, not this harness's.
const baseUiReact = resolve(baseUi, 'node_modules/react');
const baseUiReactDom = resolve(baseUi, 'node_modules/react-dom');

if (!existsSync(baseUiReact) || !existsSync(baseUiReactDom)) {
  throw new Error(
    `base-ui checkout at ${baseUi} has no installed react/react-dom under node_modules. ` +
      'Run `pnpm install` in the checkout so the React side runs the version upstream pins.',
  );
}

export default defineConfig({
  base: '/react/',
  plugins: [react()],
  resolve: {
    // The demos and packages/react resolve `react` from the checkout's own
    // node_modules, which carries a different patch release than this project
    // pins. Without deduping, both copies are bundled and every demo's hooks
    // run against a React that never rendered them. This collapses the two
    // copies; the aliases below decide *which* copy survives.
    dedupe: ['react', 'react-dom'],
    alias: {
      // Bare, not `/base-ui-demos`: import.meta.glob only routes a specifier
      // through resolve.alias when it is neither rooted nor relative.
      'base-ui-demos': resolve(baseUi, DEMOS_SUBPATH),
      '@base-ui/react': resolve(baseUi, 'packages/react/src'),
      docs: resolve(baseUi, 'docs'),
      // Pin every React specifier to the checkout's copy. dedupe alone
      // collapses toward this harness's node_modules (19.2.0), so base-ui's
      // own source and every demo would render on a React release upstream
      // does not pin. Alias and dedupe address different mechanisms — alias
      // chooses the copy, dedupe stops a second one slipping in — so both stay.
      // Subpaths ride the prefix rule (`react/jsx-runtime`, `react-dom/client`)
      // but `react-dom/client` is spelled out ahead of `react-dom` so the more
      // specific entry always wins regardless of match order.
      'react-dom/client': resolve(baseUiReactDom, 'client.js'),
      'react-dom': baseUiReactDom,
      react: baseUiReact,
    },
  },
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    sourcemap: false,
  },
});
