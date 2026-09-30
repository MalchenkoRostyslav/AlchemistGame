import { api } from '../core/api.js';
import { state, refreshPlayer } from '../core/state.js';
import { register } from '../core/router.js';
import { el, toast } from '../core/ui.js';

const BRANCHES = { Herbalism: ['🌿', 'Травництво'], Alchemy: ['⚗️', 'Алхімія'], Trade: ['🪙', 'Торгівля'] };
const ICON = { MaxEnergy: '⚡', BrewTime: '⏳', SellBonus: '💰', BuyDiscount: '🏷️', GatherBonus: '🌲', UnlockRecipe: '📖' };
const EFFECT = {
  MaxEnergy: (v) => `+${v} макс. енергії`,
  BrewTime: (v) => `−${v}% часу варіння`,
  SellBonus: (v) => `+${v}% до ціни продажу`,
  BuyDiscount: (v) => `−${v}% ціни купівлі`,
  GatherBonus: (v) => `+${v} до видобутку в лісі`,
  UnlockRecipe: () => 'Відкриває нові рецепти'
};

register('skills', {
  async render(view) {
    const pid = state.playerId;
    const S = { loaded: false, data: { skillPoints: 0, effects: [], skills: [] }, lock: false };
    const root = el('section', { class: 'panel market' });
    view.append(root);

    const guard = (fn) => async (...a) => {
      if (S.lock) return;
      S.lock = true;
      try { await fn(...a); } catch (e) { toast(e.message, 'error'); } finally { S.lock = false; }
    };

    async function load() {
      S.data = await api.get(`/skills/${pid}`);
      S.loaded = true;
      draw();
    }

    const learn = guard(async (s) => {
      const r = await api.post(`/skills/learn?playerId=${pid}&skillId=${s.id}`);
      toast(r.message, 'success');
      (r.unlockedRecipes || []).forEach((n) => toast(`Новий рецепт: ${n}`, 'success'));
      await refreshPlayer();
      await load();
    });

    function node(s) {
      const pips = Array.from({ length: s.maxRank }, (_, i) => el('i', { class: `pip${i < s.rank ? ' on' : ''}` }));
      const eff = (EFFECT[s.effectType] || (() => ''))(s.effectValue);
      return el('div', { class: `skill ${s.status}${s.canLearn ? ' can' : ''}` },
        el('div', { class: 'skill-ico' }, s.status === 'locked' ? '🔒' : (ICON[s.effectType] || '✦')),
        el('div', { class: 'skill-body' },
          el('h3', {}, s.name),
          el('p', { class: 'skill-desc' }, s.description || ''),
          el('div', { class: 'skill-eff' }, eff, s.effectType !== 'UnlockRecipe' && s.maxRank > 1 ? ' за ранг' : ''),
          s.unlocks.length ? el('div', { class: 'skill-unl' }, 'Відкриває: ' + s.unlocks.join(', ')) : null,
          s.status === 'locked' ? el('div', { class: 'need' }, `Потрібно: ${s.prerequisiteName}`) : null,
          el('div', { class: 'skill-foot' },
            el('div', { class: 'pips' }, pips),
            s.status === 'maxed'
              ? el('span', { class: 'maxed-tag' }, '★ Макс.')
              : el('button', { class: 'btn btn-brass btn-sm', disabled: !s.canLearn, onclick: () => learn(s) }, `Вивчити · ${s.cost} ✦`))));
    }

    function draw() {
      const D = S.data;
      const chips = D.effects.filter((e) => e.value > 0 && EFFECT[e.type])
        .map((e) => el('span', { class: 'chip' }, `${ICON[e.type]} ${EFFECT[e.type](e.value)}`));
      const cols = Object.entries(BRANCHES).map(([b, [ico, title]]) => {
        const list = D.skills.filter((s) => s.branch === b).sort((x, y) => x.tier - y.tier);
        return el('div', { class: 'branch' },
          el('h2', { class: 'branch-title' }, `${ico} ${title}`),
          el('div', { class: 'chain' }, list.map(node)));
      });
      root.replaceChildren(
        el('div', { class: 'market-head' },
          el('h1', { class: 'page-title' }, 'Прокачка'),
          el('div', { class: 'hud-badge gold-big' }, `✦ Очки навичок: ${D.skillPoints}`)),
        el('p', { class: 'muted small left' }, '1 очко навичок дається за кожен новий рівень.'),
        chips.length ? el('div', { class: 'chips' }, chips) : null,
        el('div', { class: 'skill-tree' }, cols));
    }

    await load();
  }
});
