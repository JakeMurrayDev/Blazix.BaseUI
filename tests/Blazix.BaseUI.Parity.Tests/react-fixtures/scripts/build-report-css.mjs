import { execFileSync } from 'node:child_process';
import { readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const fixtureRoot = resolve(scriptDirectory, '..');
const reportRoot = resolve(fixtureRoot, '..', 'Blazix.BaseUI.Parity.Tests', 'Report');
const input = join(reportRoot, 'report.source.css');
const committed = join(reportRoot, 'report.css');
const check = process.argv.includes('--check');
const output = check
  ? join(tmpdir(), `blazix-report-${process.pid}-${Date.now()}.css`)
  : committed;
const executable = resolve(
  fixtureRoot,
  'node_modules',
  '.bin',
  process.platform === 'win32' ? 'tailwindcss.cmd' : 'tailwindcss',
);

try {
  execFileSync(executable, ['-i', input, '-o', output, '--minify'], {
    cwd: fixtureRoot,
    stdio: 'inherit',
  });

  if (check && !readFileSync(output).equals(readFileSync(committed))) {
    throw new Error('Committed report.css is stale. Run `pnpm parity:report-css`.');
  }
} finally {
  if (check) {
    rmSync(output, { force: true });
  }
}
