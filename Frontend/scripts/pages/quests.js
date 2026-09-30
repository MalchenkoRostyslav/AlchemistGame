import { api } from '../core/api.js';
import { state, refreshPlayer, subscribe } from '../core/state.js';
import { register } from '../core/router.js';
import { el, toast } from '../core/ui.js';

register('quests', {
  async render(view) {
    const pid = state.playerId;
    const S = { loaded: false, tab: 'active', board: { available: [], active: [], completed: [], locked: [] }, lock: false };
    const root = el('section', { class: 'panel market' });
    view.append(root);

    const guard = (fn) => async (...a) => {
      if (S.lock) return;
      S.lock = true;
      try { await fn(...a); } catch (e) { toast(e.message, 'error'); } finally { S.lock = false; }
    };

    async function load() {
      S.board = await api.get(`/quest/board/${pid}`);
      if (!S.loaded) S.tab = S.board.active.length ? 'active' : (S.board.available.length ? 'available' : 'active');
      S.loaded = true;
      draw();
    }

    const accept = guard(async (q) => {
      const r = await api.post(`/quest/accept?playerId=${pid}&questId=${q.questId}`);
      toast(r.message, 'success');
      S.tab = 'active';
      await load();
    });

    const turnIn = guard(async (q) => {
      const r = await api.post(`/quest/turnin?playerId=${pid}&questId=${q.questId}`);
      toast(r.message, 'success');
      (r.unlockedRecipes || []).forEach((n) => toast(`Новий рецепт: ${n}`, 'success'));
      await refreshPlayer();
      await load();
    });

    function card(q, kind) {
      const reqs = q.requirements.map((r) => {
        const ok = r.have >= r.quantityNeeded;
        const pct = Math.min(100, (r.have / r.quantityNeeded) * 100);
        return el('div', { class: `req${ok ? ' ok' : ''}` },
          el('div', { class: 'req-top' }, el('span', {}, r.itemName), el('b', {}, `${Math.min(r.have, r.quantityNeeded)}/${r.quantityNeeded}`)),
          el('div', { class: 'bar' }, el('div', { class: 'bar-fill', style: `width:${pct}%` })));
      });
      let action;
      if (kind === 'available') action = el('button', { class: 'btn btn-brass', onclick: () => accept(q) }, 'Взяти квест');
      else if (kind === 'active') action = el('button', { class: `btn btn-green${q.canTurnIn ? ' pulse' : ''}`, disabled: !q.canTurnIn, onclick: () => turnIn(q) }, q.canTurnIn ? 'Здати квест' : 'Ще не все зібрано');
      else action = el('div', { class: 'done' }, '✓ Виконано');

      return el('article', { class: `quest-card ${kind}` },
        el('div', { class: 'quest-head' }, el('h3', {}, q.title),
          el('div', { class: 'tags' }, q.isRepeatable ? el('span', { class: 'tag' }, '↻ повторюваний') : null, el('span', { class: 'tag' }, `Рів. ${q.requiredLevel}`))),
        el('p', { class: 'quest-desc' }, q.description || ''),
        el('div', { class: 'reqs' }, reqs),
        el('div', { class: 'rewards' }, el('span', { class: 'reward gold' }, `🪙 ${q.rewardGold}`), el('span', { class: 'reward xp' }, `✨ ${q.rewardXp} XP`)),
        action);
    }

    function draw() {
      const B = S.board;
      const tabs = el('div', { class: 'tabs' },
        [['available', 'Доступні', B.available.length], ['active', 'Активні', B.active.length], ['completed', 'Виконані', B.completed.length]].map(([k, t, n]) =>
          el('button', { class: `tab${S.tab === k ? ' active' : ''}`, onclick: () => { S.tab = k; draw(); } }, `${t} (${n})`)));

      const list = B[S.tab];
      const empty = { available: 'Нових квестів поки немає. Підвищуйте рівень!', active: 'Немає активних квестів. Візьміть щось у «Доступних».', completed: 'Ви ще не виконали жодного квесту.' }[S.tab];
      const locked = S.tab === 'available' && B.locked.length
        ? el('div', { class: 'locked-box' }, el('h3', {}, '🔒 З’являться пізніше'),
            B.locked.map((l) => el('div', { class: 'locked-row' }, el('span', {}, l.title), el('b', {}, `Рівень ${l.requiredLevel}`))))
        : null;

      root.replaceChildren(
        el('div', { class: 'market-head' }, el('h1', { class: 'page-title' }, 'Дошка оголошень')),
        el('div', { class: 'market-bar' }, tabs),
        list.length ? el('div', { class: 'quest-grid' }, list.map((q) => card(q, S.tab))) : el('p', { class: 'muted' }, empty),
        locked);
    }

    // Прогрес квестів залежить від сумки: оновлюємо дані при зміні гравця
    const unsub = subscribe(() => { if (!root.isConnected) return unsub(); if (S.loaded && !S.lock) load().catch(() => {}); });
    await load();
  }
});
