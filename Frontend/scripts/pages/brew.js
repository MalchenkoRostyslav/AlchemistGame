import { api } from '../core/api.js';
import { state, refreshPlayer, subscribe } from '../core/state.js';
import { register } from '../core/router.js';
import { el, toast, modal, itemCard } from '../core/ui.js';

const fmt = (s) => `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
const TITLES = { Success: 'Зілля готове!', Unknown: 'Невідоме зілля', Poison: 'Отрута!' };

register('brew', {
  async render(view) {
    const pid = state.playerId;
    const S = { loaded: false, inv: [], book: { recipes: [], knownCount: 0, totalCount: 0 }, brew: { active: false },
      left: 0, page: 0, mix: new Map(), lock: false, turn: false };
    const root = el('div', { class: 'brew-grid' });
    view.append(root);
    let timerTxt = null, timerBar = null;

    const cap = () => state.player.cauldron.maxIngredients;
    const haveQty = (id) => (S.inv.find((i) => i.itemId === id && i.type === 'Ingredient') || {}).quantity || 0;
    // Обгортка дії: блокує подвійні кліки та показує помилки
    const guard = (fn) => async (...a) => {
      if (S.lock) return;
      S.lock = true;
      try { await fn(...a); } catch (e) { toast(e.message, 'error'); } finally { S.lock = false; }
    };

    async function load() {
      const [inv, book, brew] = await Promise.all([
        api.get(`/inventory/${pid}`), api.get(`/recipe/book/${pid}`), api.get(`/craft/status/${pid}`)]);
      S.inv = inv; S.book = book; S.brew = brew; S.left = brew.secondsLeft || 0;
      for (const [id, q] of S.mix) {
        const h = haveQty(id);
        if (h <= 0) S.mix.delete(id); else if (q > h) S.mix.set(id, h);
      }
      S.loaded = true;
      draw();
    }

    // ---- Дії ----
    function add(item) {
      if (S.brew.active) return toast('Казан зайнятий', 'error');
      const cur = S.mix.get(item.itemId) || 0;
      if (!cur && S.mix.size >= cap()) return toast(`Казан вміщує лише ${cap()} різних інгредієнтів`, 'error');
      if (cur >= item.quantity) return toast('Більше немає в сумці', 'error');
      if (cur >= 99) return;
      S.mix.set(item.itemId, cur + 1);
      draw();
    }
    function change(id, d) {
      const q = (S.mix.get(id) || 0) + d;
      if (q <= 0) S.mix.delete(id);
      else if (q <= haveQty(id) && q <= 99) S.mix.set(id, q);
      draw();
    }
    function fill(r) {
      if (S.brew.active) return toast('Казан зайнятий', 'error');
      if (r.ingredients.length > cap()) return toast(`Потрібен казан більшого рівня (${r.ingredients.length} інгредієнтів)`, 'error');
      S.mix = new Map(r.ingredients.map((i) => [i.itemId, i.quantity]));
      if (!r.canBrew) toast('Не вистачає інгредієнтів для цього рецепта', 'error');
      draw();
    }
    const start = guard(async () => {
      const ingredients = [...S.mix].map(([itemId, quantity]) => ({ itemId, quantity }));
      const r = await api.post('/craft/start', { playerId: pid, ingredients });
      S.mix.clear();
      toast(r.message, 'success');
      await refreshPlayer();
      await load();
    });
    const collect = guard(async () => {
      const r = await api.post(`/craft/collect?playerId=${pid}`);
      await modal({
        title: TITLES[r.outcome] || 'Готово',
        content: el('div', { class: 'result' }, itemCard(r.result, { qty: 1 }), el('p', {}, r.message), el('p', { class: 'muted' }, `+${r.xpGained} XP`))
      });
      (r.unlockedRecipes || []).forEach((n) => toast(`Новий рецепт: ${n}`, 'success'));
      await refreshPlayer();
      await load();
    });
    const gather = guard(async () => {
      const r = await api.post(`/gather?playerId=${pid}`);
      toast(r.message, 'success');
      await refreshPlayer();
      await load();
    });
    const upgrade = guard(async () => {
      const r = await api.post(`/player/upgrade-cauldron/${pid}`);
      toast(r.message, 'success');
      await refreshPlayer();
      draw();
    });

    // ---- Відображення ----
    function paintTimer() {
      if (!timerTxt || !S.brew.active) return;
      const total = S.brew.totalSeconds || 1;
      timerTxt.textContent = S.brew.ready ? 'Готово!' : fmt(S.left);
      timerBar.style.width = Math.min(100, ((total - S.left) / total) * 100) + '%';
    }

    function panelLeft() {
      const p = state.player;
      const ingr = S.inv.filter((i) => i.type === 'Ingredient');
      const other = S.inv.filter((i) => i.type !== 'Ingredient');
      return el('section', { class: 'panel' },
        el('h2', { class: 'panel-h' }, 'Інгредієнти'),
        ingr.length
          ? el('div', { class: 'grid-items sm' }, ingr.map((i) => itemCard(i, { onClick: () => add(i) })))
          : el('p', { class: 'muted' }, 'Сумка порожня: відправтесь у ліс або купіть інгредієнти в магазині.'),
        el('button', { class: 'btn btn-green wide', disabled: p.energy < 5, onclick: gather }, '🌲 Піти в ліс (−5 ⚡)'),
        other.length ? [el('h3', { class: 'panel-h sub' }, 'Готові зілля'),
          el('div', { class: 'grid-items sm' }, other.map((i) => itemCard(i)))] : null);
    }

    function panelCenter() {
      const p = state.player, c = p.cauldron, b = S.brew;
      const st = b.active ? (b.ready ? 'ready' : 'brewing') : 'idle';
      const pot = el('div', { class: `cauldron ${st}` },
        el('div', { class: 'steam' }),
        el('div', { class: 'pot' }, el('div', { class: 'liquid' },
          [1, 2, 3, 4, 5, 6].map((n) => el('i', { class: 'bubble', style: `left:${n * 14}%;animation-delay:${n * -0.4}s` })))));
      let body;
      if (b.active) {
        timerBar = el('div', { class: 'bar-fill' });
        timerTxt = el('div', { class: 'timer' });
        body = el('div', { class: 'stack' }, el('div', { class: 'bar big' }, timerBar), timerTxt,
          b.ready
            ? el('button', { class: 'btn btn-brass btn-lg pulse', onclick: collect }, '✨ Забрати зілля')
            : el('p', { class: 'muted small' }, 'Зілля вариться...'));
        paintTimer();
      } else {
        const rows = [...S.mix].map(([id, q]) => {
          const it = S.inv.find((i) => i.itemId === id);
          return el('div', { class: 'slot-row' },
            el('span', { class: 'slot-name' }, it ? it.name : `#${id}`),
            el('button', { class: 'btn btn-ghost btn-sm', onclick: () => change(id, -1) }, '−'),
            el('b', {}, q),
            el('button', { class: 'btn btn-ghost btn-sm', onclick: () => change(id, 1) }, '+'));
        });
        body = el('div', { class: 'stack' },
          el('div', { class: 'muted small' }, `Слоти: ${S.mix.size}/${c.maxIngredients}`),
          rows.length ? rows : el('p', { class: 'muted' }, 'Клацніть на інгредієнти зліва або оберіть рецепт у книзі.'),
          el('button', { class: 'btn btn-brass btn-lg', disabled: !S.mix.size || p.energy < 10, onclick: start }, 'Варити (−10 ⚡)'),
          S.mix.size ? el('button', { class: 'btn btn-ghost', onclick: () => { S.mix.clear(); draw(); } }, 'Очистити') : null);
      }
      const up = c.nextUpgradeCost != null
        ? el('button', { class: 'btn btn-ghost btn-sm', onclick: upgrade }, `⬆ Покращити казан (${c.nextUpgradeCost} 🪙 → ${c.nextMaxIngredients} слотів)`)
        : el('span', { class: 'muted small' }, 'Казан максимального рівня');
      return el('section', { class: 'panel center' },
        el('h2', { class: 'panel-h' }, 'Алхімічний казан'), pot,
        el('div', { class: 'muted small' }, `Казан ${c.level} рів. · час варіння ×${Number(c.craftTimeModifier).toFixed(2)}`),
        body, up);
    }

    function panelBook() {
      const rs = S.book.recipes;
      if (S.page >= rs.length) S.page = Math.max(0, rs.length - 1);
      const r = rs[S.page];
      const nav = (d) => () => { S.page = (S.page + d + rs.length) % rs.length; S.turn = true; draw(); };
      const page = r
        ? el('div', { class: `book-page${S.turn ? ' turn' : ''}` },
            itemCard({ id: r.resultItemId, name: r.name, icon: r.icon, color: r.color, type: 'Potion' }),
            el('div', { class: 'ing-list' }, r.ingredients.map((i) =>
              el('div', { class: `ing ${i.have >= i.quantity ? 'ok' : 'miss'}` }, i.name, el('b', {}, `${i.have}/${i.quantity}`)))),
            el('button', { class: 'btn btn-green', onclick: () => fill(r) }, 'Заповнити котел'))
        : el('p', { class: 'muted' }, 'Рецептів ще немає. Експериментуйте!');
      S.turn = false;
      return el('section', { class: 'panel' },
        el('h2', { class: 'panel-h' }, '📖 Книга рецептів'),
        el('div', { class: 'muted small' }, `Відкрито ${S.book.knownCount} із ${S.book.totalCount}`),
        page,
        rs.length > 1 ? el('div', { class: 'book-nav' },
          el('button', { class: 'btn btn-ghost btn-sm', onclick: nav(-1) }, '◀'),
          el('span', {}, `${S.page + 1} / ${rs.length}`),
          el('button', { class: 'btn btn-ghost btn-sm', onclick: nav(1) }, '▶')) : null,
        el('p', { class: 'muted small' }, 'Невідома суміш дасть отруту або невідоме зілля.'));
    }

    function draw() { root.replaceChildren(panelLeft(), panelCenter(), panelBook()); }

    // Таймер (клієнтський відлік; істину тримає сервер)
    const iv = setInterval(() => {
      if (!root.isConnected) return clearInterval(iv);
      if (S.brew.active && !S.brew.ready) {
        S.left = Math.max(0, S.left - 1);
        if (S.left === 0) { S.brew.ready = true; draw(); } else paintTimer();
      }
    }, 1000);
    // Перемальовка при оновленні даних гравця (енергія тощо)
    const unsub = subscribe(() => { if (!root.isConnected) return unsub(); if (S.loaded && !S.lock) draw(); });

    await load();
  }
});
