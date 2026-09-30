import { state } from './state.js';

export const QUALITY = ['Звичайне', 'Добре', 'Відмінне'];
const TYPES = { Ingredient: 'Інгредієнт', Potion: 'Зілля', Poison: 'Отрута', UnknownPotion: 'Невідоме зілля', Misc: 'Різне' };

// Створення DOM-елемента: el('div', {class:'x', onclick: fn}, 'текст', іншийЕлемент)
export function el(tag, attrs = {}, ...kids) {
  const n = document.createElement(tag);
  for (const [k, v] of Object.entries(attrs || {})) {
    if (k === 'class') n.className = v;
    else if (k.startsWith('on')) n.addEventListener(k.slice(2), v);
    else if (v !== false && v != null) n.setAttribute(k, v);
  }
  for (const c of kids.flat()) {
    if (c == null || c === false) continue;
    n.append(c instanceof Node ? c : document.createTextNode(String(c)));
  }
  return n;
}

export function toast(msg, type = 'info') {
  const t = el('div', { class: `toast toast-${type}` }, msg);
  document.getElementById('toasts').append(t);
  setTimeout(() => { t.classList.add('out'); setTimeout(() => t.remove(), 300); }, 3400);
}

export function modal({ title, content, buttons = [{ label: 'Гаразд', value: true, cls: 'btn-brass' }] }) {
  return new Promise((resolve) => {
    const close = (v) => { ov.classList.add('out'); setTimeout(() => ov.remove(), 200); resolve(v); };
    const box = el('div', { class: 'modal panel' },
      el('h2', { class: 'modal-title' }, title),
      el('div', { class: 'modal-body' }, content),
      el('div', { class: 'modal-actions' },
        buttons.map((b) => el('button', { class: `btn ${b.cls || 'btn-ghost'}`, onclick: () => close(b.value) }, b.label))));
    const ov = el('div', { class: 'overlay' }, box);
    document.body.append(ov);
  });
}

export const confirmDialog = (text, title = 'Підтвердження') => modal({
  title, content: text,
  buttons: [{ label: 'Скасувати', value: false, cls: 'btn-ghost' }, { label: 'Так', value: true, cls: 'btn-red' }]
});

export const levelUp = (level) => modal({
  title: 'Новий рівень!',
  content: el('div', {}, el('div', { class: 'levelup-num' }, level), el('p', {}, '+1 очко навичок ✦'))
});

// Плавна зміна числа
export function countUp(node, to, ms = 600) {
  const from = Number(node.dataset.v ?? node.textContent) || 0;
  node.dataset.v = to;
  if (from === to) { node.textContent = to; return; }
  const t0 = performance.now();
  const step = (t) => {
    const p = Math.min(1, (t - t0) / ms);
    node.textContent = Math.round(from + (to - from) * (1 - Math.pow(1 - p, 3)));
    if (p < 1) requestAnimationFrame(step);
  };
  requestAnimationFrame(step);
}

// ---- Картка предмета + підказка ----
const placeholder = (f) => el('div', { class: 'icon-ph' }, (f.name || '?').trim().charAt(0).toUpperCase());

// item: {itemId|id, name, icon, color, rarity, type, quantity, quality, description, properties:[{name,description,color}]}
export function itemCard(item, opts = {}) {
  const full = { ...(state.catalog.get(item.itemId ?? item.id) || {}), ...item };
  const qty = opts.qty ?? full.quantity;
  const icon = full.icon
    ? el('img', { src: full.icon, alt: '', onerror: (e) => e.target.replaceWith(placeholder(full)) })
    : placeholder(full);
  const card = el('div', { class: `item-card tip${opts.onClick ? ' clickable' : ''}`, style: `--rc:${full.color || '#b9a27a'}` },
    qty != null ? el('span', { class: 'qty' }, '×' + qty) : null,
    full.quality > 0 ? el('span', { class: `quality q${full.quality}` }, '★'.repeat(full.quality)) : null,
    el('div', { class: 'item-icon' }, icon),
    el('div', { class: 'item-name' }, full.name));
  card._item = full;
  if (opts.onClick) card.addEventListener('click', opts.onClick);
  return card;
}

const tipContent = (i) => [
  el('div', { class: 'tip-title', style: `color:${i.color || 'inherit'}` }, i.name),
  el('div', { class: 'tip-sub' }, [TYPES[i.type] || i.type, i.rarity, i.quality > 0 ? QUALITY[i.quality] : null].filter(Boolean).join(' · ')),
  i.description ? el('p', { class: 'tip-desc' }, i.description) : null,
  ...(i.properties || []).map((p) => el('div', { class: 'tip-prop' },
    el('span', { class: 'dot', style: `background:${p.color || '#b98a3e'}` }),
    el('b', {}, p.name), p.description ? ` — ${p.description}` : null))
].filter(Boolean);

export function initTooltip() {
  const tip = el('div', { class: 'tooltip hidden' });
  document.body.append(tip);
  document.addEventListener('mousemove', (e) => {
    const t = e.target.closest ? e.target.closest('.tip') : null;
    if (!t || !t._item) { tip.classList.add('hidden'); tip._for = null; return; }
    if (tip._for !== t) { tip._for = t; tip.replaceChildren(...tipContent(t._item)); }
    tip.classList.remove('hidden');
    let x = e.clientX + 16, y = e.clientY + 16;
    if (x + tip.offsetWidth > innerWidth - 8) x = e.clientX - tip.offsetWidth - 16;
    if (y + tip.offsetHeight > innerHeight - 8) y = e.clientY - tip.offsetHeight - 16;
    tip.style.left = Math.max(8, x) + 'px';
    tip.style.top = Math.max(8, y) + 'px';
  });
}

// Світлячки на фоні
export function fireflies(container, n = 16) {
  for (let i = 0; i < n; i++) {
    const f = el('span', { class: 'firefly' });
    f.style.left = Math.random() * 100 + '%';
    f.style.top = 30 + Math.random() * 70 + '%';
    f.style.setProperty('--d', 9 + Math.random() * 10 + 's');
    f.style.setProperty('--delay', -Math.random() * 15 + 's');
    container.append(f);
  }
}
