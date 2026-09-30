import { api } from './core/api.js';
import { state, subscribe, setPlayer, refreshPlayer, loadCatalog } from './core/state.js';
import { toast, confirmDialog, countUp, levelUp, initTooltip, fireflies } from './core/ui.js';
import { audio } from './core/audio.js';
import { start as startRouter, stop as stopRouter } from './core/router.js';
import './pages/stubs.js';
import './pages/brew.js';
import './pages/shop.js';
import './pages/quests.js';
import './pages/skills.js';

const $ = (id) => document.getElementById(id);
let lastLevel = null, energyLeft = 0, timerId = null;

// ---------- Екрани й меню ----------
function showScreen(name) {
  $('screen-menu').classList.toggle('hidden', name !== 'menu');
  $('screen-game').classList.toggle('hidden', name !== 'game');
}
function menuView(name) {
  for (const v of ['main', 'new', 'load']) $('menu-' + v).classList.toggle('hidden', v !== name);
}

async function showProfiles(mode) {
  menuView('load');
  $('list-title').textContent = mode === 'delete' ? 'Оберіть профіль для видалення' : 'Оберіть профіль';
  const list = $('profiles-list');
  list.textContent = 'Завантаження...';
  try {
    const profiles = await api.get('/player/profiles');
    list.replaceChildren();
    if (!profiles.length) { list.textContent = 'Профілів ще немає.'; return; }
    for (const p of profiles) {
      const b = document.createElement('button');
      b.className = `btn ${mode === 'delete' ? 'btn-red' : 'btn-green'}`;
      b.textContent = `${p.name} · Рівень ${p.level}`;
      b.onclick = async () => {
        if (mode === 'load') return startGame(p.id);
        if (!(await confirmDialog(`Видалити профіль «${p.name}»? Це незворотно.`))) return;
        try { await api.del(`/player/delete/${p.id}`); toast('Профіль видалено', 'success'); showProfiles('delete'); }
        catch (e) { toast(e.message, 'error'); }
      };
      list.append(b);
    }
  } catch (e) { list.textContent = e.message; }
}

async function createProfile() {
  const name = $('nickname-input').value.trim();
  const err = $('new-error');
  if (!name) { err.textContent = "Введіть ім'я!"; return; }
  err.textContent = '';
  try {
    const p = await api.post(`/player/new?nickname=${encodeURIComponent(name)}`);
    startGame(p.id);
  } catch (e) { err.textContent = e.message; }
}

// ---------- Гра ----------
async function startGame(id) {
  setPlayer(id);
  try {
    await loadCatalog();
    await refreshPlayer();
  } catch (e) { toast(e.message, 'error'); setPlayer(null); return; }
  showScreen('game');
  startRouter($('view'), (path) =>
    document.querySelectorAll('.nav-link').forEach((a) => a.classList.toggle('active', a.dataset.page === path)));
  clearInterval(timerId);
  timerId = setInterval(energyTick, 1000);
}

function exitToMenu() {
  clearInterval(timerId);
  stopRouter();
  setPlayer(null);
  lastLevel = null;
  showScreen('menu');
  menuView('main');
}

// ---------- Шапка (HUD) ----------
function renderHud(s) {
  const p = s.player;
  if (!p) return;
  countUp($('hud-gold'), p.gold);
  $('hud-energy').textContent = `${p.energy}/${p.maxEnergy}`;
  $('hud-level').textContent = p.level;
  $('hud-xp-fill').style.width = Math.min(100, (p.xp / p.xpToNext) * 100) + '%';
  $('hud-xp-text').textContent = `${p.xp} / ${p.xpToNext} XP`;
  const sp = $('hud-sp');
  sp.textContent = `✦ ${p.skillPoints}`;
  sp.classList.toggle('hidden', p.skillPoints <= 0);
  energyLeft = p.secondsToNextEnergy;
  if (lastLevel !== null && p.level > lastLevel) { levelUp(p.level); audio.play('levelup'); }
  lastLevel = p.level;
  paintEnergyTimer();
}

function paintEnergyTimer() {
  const p = state.player, box = $('hud-energy-timer');
  if (!p || p.energy >= p.maxEnergy) { box.textContent = ''; return; }
  box.textContent = `+1 через ${Math.floor(energyLeft / 60)}:${String(energyLeft % 60).padStart(2, '0')}`;
}

function energyTick() {
  const p = state.player;
  if (!p || p.energy >= p.maxEnergy) return;
  if (energyLeft > 0) energyLeft--;
  paintEnergyTimer();
  if (energyLeft <= 0) refreshPlayer().catch(() => {});
}

// ---------- Ініціалізація ----------
subscribe(renderHud);
initTooltip();
fireflies($('fx'));

$('btn-new').onclick = () => { menuView('new'); $('new-error').textContent = ''; $('nickname-input').value = ''; $('nickname-input').focus(); };
$('btn-load').onclick = () => showProfiles('load');
$('btn-delete').onclick = () => showProfiles('delete');
$('btn-create').onclick = createProfile;
$('nickname-input').addEventListener('keydown', (e) => { if (e.key === 'Enter') createProfile(); });
document.querySelectorAll('[data-back]').forEach((b) => (b.onclick = () => menuView('main')));
$('btn-exit').onclick = exitToMenu;
