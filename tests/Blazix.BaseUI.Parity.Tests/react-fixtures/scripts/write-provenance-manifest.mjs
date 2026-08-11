import { execFileSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { existsSync, readFileSync, readdirSync, statSync, writeFileSync } from 'node:fs';
import { dirname, join, relative, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const project = resolve(here, '..');
const dist = join(project, 'dist');
const manifestName = 'parity-provenance.json';

function locateBaseUi() {
  const override = process.env.PARITY_BASE_UI_PATH;
  if (override) return resolve(override);

  let directory = here;
  for (let index = 0; index < 12; index += 1) {
    const candidate = join(directory, '.base-ui');
    if (existsSync(join(candidate, 'packages/react'))) return candidate;
    const parent = dirname(directory);
    if (parent === directory) break;
    directory = parent;
  }

  throw new Error('Could not locate .base-ui. Set PARITY_BASE_UI_PATH.');
}

function sha256File(path) {
  return createHash('sha256').update(readFileSync(path)).digest('hex').toUpperCase();
}

function files(directory) {
  return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const path = join(directory, entry.name);
    return entry.isDirectory() ? files(path) : [path];
  });
}

function posix(path) {
  return path.split(sep).join('/');
}

function distFingerprint() {
  const canonical = files(dist)
    .map((path) => ({ path, relativePath: posix(relative(dist, path)) }))
    .filter((entry) => entry.relativePath !== manifestName)
    .map((entry) => `${entry.relativePath}:${sha256File(entry.path)}`)
    .sort()
    .join('\n') + '\n';
  return createHash('sha256').update(canonical).digest('hex').toUpperCase();
}

const baseUi = locateBaseUi();
const fixtureManifest = JSON.parse(
  readFileSync(resolve(project, '..', 'manifest', 'fixtures.json'), 'utf8'),
);
const entries = [
  ...fixtureManifest.map((fixture) => ({
    fixture: fixture.id,
    sourcePath: `docs/src/app/(docs)/react/components/${fixture.react}`,
    absolutePath: resolve(baseUi, 'docs/src/app/(docs)/react/components', fixture.react),
  })),
  {
    fixture: 'harness/canary',
    sourcePath: 'react-fixtures/src/canary.tsx',
    absolutePath: resolve(project, 'src', 'canary.tsx'),
  },
];

for (const entry of entries) {
  if (!existsSync(entry.absolutePath) || !statSync(entry.absolutePath).isFile()) {
    throw new Error(`React provenance source is missing for ${entry.fixture}: ${entry.absolutePath}`);
  }
}

const provenance = {
  schemaVersion: 2,
  upstreamSha: execFileSync('git', ['rev-parse', 'HEAD'], {
    cwd: baseUi,
    encoding: 'utf8',
  }).trim(),
  distFingerprint: distFingerprint(),
  generatedAtUtc: new Date().toISOString(),
  sources: entries.map((entry) => ({
    fixture: entry.fixture,
    sourcePath: entry.sourcePath,
    sourceHash: sha256File(entry.absolutePath),
  })),
};

writeFileSync(join(dist, manifestName), `${JSON.stringify(provenance, null, 2)}\n`);
console.log(`[parity] wrote ${manifestName} for ${provenance.upstreamSha}`);
