import { api } from '../core/api.js';
import { state, refreshPlayer, subscribe } from '../core/state.js';
import { register } from '../core/router.js';
import { el, toast, itemCard } from '../core/ui.js';

const MODES = [[1, '×1'], [5, '×5'], [10, '×10'], ['max', 'Макс']];

register('shop', {
  async render(view) {
    const pid = state.playerId;
    const S = { loaded: false, tab: 'buy', mode: 1, offers: [], inv: [], lock: false };
    const root = el('section', { class: 'panel market' });
    view.append(root);

    const guard = (fn) => async (...a) => {
      if (S.lock) return;
      S.lock = true;
      try { await fn(...a); } catch (e) { toast(e.message, 'error'); } finally { S.lock = false; }
    };
    // Скільки штук торгувати: за режимом, але не більше за можливий максимум
    const qtyFor = (cap) => (S.mode === 'max' ? cap : Math.min(S.mode, cap));

    async function load() {
      const [offers, inv] = await Promise.all([api.get(`/shop/assortment/${pid}`), api.get(`/inventory/${pid}`)]);
      S.offers = offers; S.inv = inv; S.loaded = true;
      draw();
    }

    const buy = guard(async (o) => {
      const cap = Math.min(999, Math.floor(state.player.gold / o.price));
      const q = qtyFor(cap);
      if (q < 1) return toast('Недостатньо золота', 'error');
      const r = await api.post(`/shop/buy?playerId=${pid}&itemId=${o.itemId}&quantity=${q}`);
      toast(r.message, 'success');
      await refreshPlayer();
      await load();
    });

    const sell = guard(async (i) => {
      const q = qtyFor(Math.min(999, i.quantity));
      if (q < 1) return;
      const r = await api.post(`/shop/sell?playerId=${pid}&itemId=${i.itemId}&quantity=${q}`);
      toast(r.message, 'success');
      await refreshPlayer();
      await load();
    });

    function draw() {
      const gold = state.player.gold;
      const tabs = el('div', { class: 'tabs' },
        [['buy', '🛒 Купити'], ['sell', '💰 Продати']].map(([k, t]) =>
          el('button', { class: `tab${S.tab === k ? ' active' : ''}`, onclick: () => { S.tab = k; draw(); } }, t)));
      const seg = el('div', { class: 'seg' },
        MODES.map(([m, t]) => el('button', { class: `seg-btn${S.mode === m ? ' active' : ''}`, onclick: () => { S.mode = m; draw(); } }, t)));

      let grid;
      if (S.tab === 'buy') {
        grid = S.offers.length
          ? el('div', { class: 'shop-grid' }, S.offers.map((o) =>
              el('div', { class: `shop-item${o.locked ? ' locked' : ''}` },
                itemCard(o),
                el('div', { class: 'price' }, `🪙 ${o.price}`),
                o.locked
                  ? el('div', { class: 'lock' }, `🔒 Рівень ${o.requiredLevel}`)
                  : el('button', { class: 'btn btn-brass btn-sm', disabled: gold < o.price, onclick: () => buy(o) }, 'Купити'))))
          : el('p', { class: 'muted' }, 'Торговець поки нічого не продає.');
      } else {
        const sellable = S.inv.filter((i) => i.sellable);
        grid = sellable.length
          ? el('div', { class: 'shop-grid' }, sellable.map((i) =>
              el('div', { class: 'shop-item' },
                itemCard(i),
                el('div', { class: 'price' }, `🪙 ${i.price} / шт.`),
                el('button', { class: 'btn btn-green btn-sm', onclick: () => sell(i) },
                  `Продати ×${qtyFor(Math.min(999, i.quantity))}`))))
          : el('p', { class: 'muted' }, 'Немає зілля на продаж. Звари щось у котлі!');
      }

      root.replaceChildren(
        el('div', { class: 'market-head' },
          el('h1', { class: 'page-title' }, 'Торговець'),
          el('div', { class: 'hud-badge gold-big' }, `🪙 ${gold}`)),
        el('div', { class: 'market-bar' }, tabs, el('div', { class: 'seg-wrap' }, el('span', { class: 'muted small' }, 'Кількість:'), seg)),
        grid);
    }

    const unsub = subscribe(() => { if (!root.isConnected) return unsub(); if (S.loaded && !S.lock) draw(); });
    await load();
  }
});
