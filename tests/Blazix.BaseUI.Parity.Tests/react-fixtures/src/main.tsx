import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { fixtureById, fixtures } from './fixtures';

function currentId(): string | null {
  const match = /^#\/fixture\/(.+)$/.exec(window.location.hash);
  return match ? match[1] : null;
}

function App() {
  const id = currentId();

  if (id === null) {
    return (
      <ul id="fixtures">
        {fixtures.map((f) => (
          <li key={f.id}>
            <a href={`#/fixture/${f.id}`}>{f.id}</a>
          </li>
        ))}
      </ul>
    );
  }

  const fixture = fixtureById.get(id);
  if (fixture === undefined) {
    return <p data-parity-error>Unknown fixture: {id}</p>;
  }

  const { Component } = fixture;
  return <Component />;
}

const host = document.createElement('div');
host.setAttribute('data-parity-root', '');
// The Blazor side reports interactivity through this attribute; React is
// interactive as soon as it mounts, so the settle protocol is symmetric.
host.setAttribute('data-interactive', 'true');
document.body.append(host);

createRoot(host).render(
  <StrictMode>
    <App />
  </StrictMode>,
);

window.addEventListener('hashchange', () => window.location.reload());
