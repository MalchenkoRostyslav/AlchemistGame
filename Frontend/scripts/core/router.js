import { state } from './state.js';
import { el } from './ui.js';

const routes = new Map();
let view, onChange, active = false, token = 0;

// page = { render(container) }
export const register = (path, page) => routes.set(path, page);
export const go = (path) => { location.hash = '#/' + path; };

export function start(viewEl, changeCb) {
  view = viewEl; onChange = changeCb; active = true;
  window.addEventListener('hashchange', render);
  if (!location.hash.startsWith('#/')) location.hash = '#/brew';
  else render();
}

export function stop() {
  active = false;
  window.removeEventListener('hashchange', render);
  if (view) view.replaceChildren();
}

async function render() {
  if (!active || !state.playerId) return;
  const wanted = location.hash.slice(2).split('?')[0];
  const path = routes.has(wanted) ? wanted : 'brew';
  const my = ++token;

  view.classList.add('leaving');
  await new Promise((r) => setTimeout(r, 140));
  if (my !== token) return;

  view.replaceChildren();
  if (onChange) onChange(path);
  try {
    await routes.get(path).render(view);
  } catch (e) {
    view.append(el('div', { class: 'panel' }, el('h2', {}, 'Помилка сторінки'), el('p', { class: 'muted' }, e.message)));
  }
  view.classList.remove('leaving');
  view.classList.add('entering');
  setTimeout(() => view.classList.remove('entering'), 400);
}
