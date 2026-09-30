// Малюнок котла (SVG), ефекти бризок і перетягування інгредієнтів (миша + дотик).

const flame = (x, w, h) =>
  `<path class="fl" d="M${x} 238 C${x - 4} ${238 - h * 0.4} ${x + w * 0.2} ${238 - h * 0.6} ${x + w * 0.5} ${238 - h} ` +
  `C${x + w * 0.8} ${238 - h * 0.55} ${x + w + 4} ${238 - h * 0.4} ${x + w} 238 Z"/>`;
const FLAMES = [[72, 22, 30], [88, 26, 40], [106, 28, 46], [126, 30, 50], [146, 28, 44], [164, 26, 38], [180, 22, 28]]
  .map((a) => flame(...a)).join('');
const BUBBLES = [[88, 98, 4, -0.2], [112, 101, 5, -1.1], [134, 97, 3.5, -0.6], [156, 101, 5, -1.8], [176, 98, 4, -0.9], [122, 95, 3, -2.2]]
  .map(([x, y, r, d]) => `<circle class="bb" cx="${x}" cy="${y}" r="${r}" style="animation-delay:${d}s"/>`).join('');
const SPARKS = [[12, 30, 0], [82, 24, 0.4], [20, 62, 0.8], [76, 66, 1.2], [48, 8, 0.6], [50, 40, 1.0]]
  .map(([l, t, d]) => `<i class="spark" style="left:${l}%;top:${t}%;animation-delay:${d}s"></i>`).join('');

const SVG = `
<div class="cauldron-glow"></div>
<svg viewBox="0 0 260 250" class="cauldron-svg" aria-hidden="true">
  <defs>
    <radialGradient id="ironG" cx="35%" cy="30%" r="75%"><stop offset="0" stop-color="#8d8d99"/><stop offset=".45" stop-color="#4a4a55"/><stop offset="1" stop-color="#20202a"/></radialGradient>
    <linearGradient id="rimG" x1="0" x2="1" y1="0" y2="0"><stop offset="0" stop-color="#666672"/><stop offset=".5" stop-color="#a4a4b0"/><stop offset="1" stop-color="#4a4a55"/></linearGradient>
    <linearGradient id="liqG" x1="0" y1="0" x2="0" y2="1"><stop offset="0" class="lq-a"/><stop offset="1" class="lq-b"/></linearGradient>
    <linearGradient id="flameG" x1="0" y1="1" x2="0" y2="0"><stop offset="0" stop-color="#ff4d1a"/><stop offset=".55" stop-color="#ffb02e"/><stop offset="1" stop-color="#fff3a6"/></linearGradient>
    <filter id="blur6" x="-20%" y="-50%" width="140%" height="200%"><feGaussianBlur stdDeviation="6"/></filter>
  </defs>
  <ellipse class="coals" cx="130" cy="240" rx="82" ry="9"/>
  <path d="M72 188 L56 234 L80 234 L96 200 Z" fill="#23232a"/>
  <path d="M188 188 L204 234 L180 234 L164 200 Z" fill="#23232a"/>
  <path d="M34 92 C22 150 52 214 130 214 C208 214 238 150 226 92 Z" fill="url(#ironG)" stroke="#16161c" stroke-width="3"/>
  <path d="M38 128 Q130 150 222 128" stroke="#101015" stroke-width="3" fill="none" opacity=".45"/>
  <path d="M52 118 C48 160 68 190 98 202" stroke="#fff" stroke-opacity=".22" stroke-width="7" fill="none" stroke-linecap="round"/>
  <ellipse cx="130" cy="92" rx="100" ry="22" fill="#121218" stroke="url(#rimG)" stroke-width="8"/>
  <ellipse class="liquid" cx="130" cy="96" rx="86" ry="15" fill="url(#liqG)"/>
  <ellipse class="shine" cx="120" cy="92" rx="40" ry="4"/>
  ${BUBBLES}
  <ellipse class="ripple" cx="130" cy="96" rx="24" ry="5"/>
  <path d="M32 104 C6 104 6 134 34 130" fill="none" stroke="#2c2c34" stroke-width="7" stroke-linecap="round"/>
  <path d="M228 104 C254 104 254 134 226 130" fill="none" stroke="#2c2c34" stroke-width="7" stroke-linecap="round"/>
  <g fill="#a9a9b5"><circle cx="46" cy="112" r="2.6"/><circle cx="88" cy="119" r="2.6"/><circle cx="130" cy="121" r="2.6"/><circle cx="172" cy="119" r="2.6"/><circle cx="214" cy="112" r="2.6"/></g>
  <g class="fire">${FLAMES}</g>
</svg>
<i class="wisp w1"></i><i class="wisp w2"></i><i class="wisp w3"></i>
${SPARKS}`;

const BASE = [74, 163, 155]; // колір води за замовчуванням
const hexToRgb = (c) => {
  const m = /^#?([0-9a-f]{6})$/i.exec(String(c || '').trim());
  return m ? [0, 2, 4].map((i) => parseInt(m[1].slice(i, i + 2), 16)) : null;
};
const toHex = (rgb) => '#' + rgb.map((v) => Math.max(0, Math.min(255, Math.round(v))).toString(16).padStart(2, '0')).join('');
const shade = (rgb, k) => rgb.map((v) => (k >= 0 ? v + (255 - v) * k : v * (1 + k)));

// st: 'idle' | 'brewing' | 'ready'; colors: масив кольорів інгредієнтів (тінт рідини) або null
export function cauldronEl(st, colors) {
  const wrap = document.createElement('div');
  wrap.className = `cauldron-wrap ${st}`;
  wrap.innerHTML = SVG;
  const list = (colors || []).map(hexToRgb).filter(Boolean);
  if (list.length) {
    list.push(BASE);
    const avg = [0, 1, 2].map((i) => list.reduce((s, c) => s + c[i], 0) / list.length);
    wrap.style.setProperty('--lq1', toHex(shade(avg, 0.3)));
    wrap.style.setProperty('--lq2', toHex(shade(avg, -0.3)));
  }
  return wrap;
}

// Бризки й кола на воді при падінні інгредієнта
export function splash(wrap, color) {
  if (!wrap) return;
  wrap.classList.remove('drop');
  void wrap.offsetWidth;
  wrap.classList.add('drop');
  for (let i = 0; i < 9; i++) {
    const p = document.createElement('i');
    p.className = 'drip';
    const a = (30 + Math.random() * 120) * (Math.PI / 180);
    const d = 30 + Math.random() * 40;
    p.style.setProperty('--dx', Math.cos(a) * d * (Math.random() < 0.5 ? -1 : 1) + 'px');
    p.style.setProperty('--dy', -Math.sin(a) * d - 20 + 'px');
    p.style.background = color || '#9fe6dd';
    wrap.append(p);
    setTimeout(() => p.remove(), 750);
  }
}

// Перетягування картки (pointer events: працює мишею й пальцем). Клік без руху = onClick.
export function enableDrag(node, { canDrag, onClick, onDrop, dropSelector = '.cauldron-wrap' }) {
  node.addEventListener('pointerdown', (e) => {
    if (e.pointerType === 'mouse' && e.button !== 0) return;
    const sx = e.clientX, sy = e.clientY;
    let ghost = null, dragging = false;
    const zone = () => document.querySelector(dropSelector);
    const over = (x, y) => { const t = document.elementFromPoint(x, y); return !!(t && t.closest(dropSelector)); };
    const place = (x, y) => { ghost.style.left = x + 'px'; ghost.style.top = y + 'px'; };

    const move = (ev) => {
      if (!dragging) {
        if (Math.hypot(ev.clientX - sx, ev.clientY - sy) < 6) return;
        if (canDrag && !canDrag()) { off(); return; }
        dragging = true;
        ghost = node.cloneNode(true);
        ghost.classList.add('drag-ghost');
        document.body.append(ghost);
        node.classList.add('dragging');
      }
      place(ev.clientX, ev.clientY);
      const z = zone();
      if (z) z.classList.toggle('drop-hover', over(ev.clientX, ev.clientY));
    };

    const up = (ev) => {
      off();
      const z = zone();
      if (z) z.classList.remove('drop-hover');
      if (!dragging) { if (onClick) onClick(); return; }
      node.classList.remove('dragging');
      if (z && over(ev.clientX, ev.clientY)) {
        const r = z.getBoundingClientRect();
        ghost.classList.add('falling'); // летить у котел і зменшується
        place(r.left + r.width / 2, r.top + r.height * 0.38);
        setTimeout(() => { ghost.remove(); onDrop(); }, 260);
      } else {
        ghost.classList.add('cancel');
        setTimeout(() => ghost.remove(), 200);
      }
    };

    const off = () => {
      window.removeEventListener('pointermove', move);
      window.removeEventListener('pointerup', up);
      window.removeEventListener('pointercancel', up);
    };
    window.addEventListener('pointermove', move);
    window.addEventListener('pointerup', up);
    window.addEventListener('pointercancel', up);
  });
}
