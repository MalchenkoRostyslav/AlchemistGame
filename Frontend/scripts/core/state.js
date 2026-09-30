import { api } from './api.js';

export const state = { playerId: null, player: null, catalog: new Map() };
const listeners = new Set();
export const subscribe = (fn) => { listeners.add(fn); return () => listeners.delete(fn); };
export const emit = () => listeners.forEach((fn) => fn(state));

export function setPlayer(id) {
  state.playerId = id;
  state.player = null;
  emit();
}

export async function refreshPlayer() {
  if (!state.playerId) return null;
  state.player = await api.get(`/player/${state.playerId}`);
  emit();
  return state.player;
}

// Каталог предметів (опис, властивості) для підказок. З'явиться після Етапу 2 бекенду; до того працює без нього.
export async function loadCatalog() {
  try {
    const items = await api.get('/catalog/items');
    state.catalog = new Map(items.map((i) => [i.id, i]));
  } catch { state.catalog = new Map(); }
}
