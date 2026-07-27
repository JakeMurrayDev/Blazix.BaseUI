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

export default defineConfig({
  base: '/react/',
  plugins: [react()],
  resolve: {
    // The demos and packages/react resolve `react` from the checkout's own
    // node_modules, which carries a different patch release than this project
    // pins. Without deduping, both copies are bundled and every demo's hooks
    // run against a React that never rendered them.
    dedupe: ['react', 'react-dom'],
    alias: {
      // Bare, not `/base-ui-demos`: import.meta.glob only routes a specifier
      // through resolve.alias when it is neither rooted nor relative.
      'base-ui-demos': resolve(baseUi, DEMOS_SUBPATH),
      '@base-ui/react': resolve(baseUi, 'packages/react/src'),
      docs: resolve(baseUi, 'docs'),
    },
  },
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    sourcemap: false,
  },
});
