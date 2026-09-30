// Заготовка під звуки. Щоб додати звук: audio.register('brew', 'assets/audio/brew.mp3'), потім audio.play('brew').
const sounds = {};
let enabled = true;
let musicEl = null;

export const audio = {
  register(name, src) { sounds[name] = new Audio(src); },
  play(name) {
    const s = sounds[name];
    if (!enabled || !s) return;
    const c = s.cloneNode();
    c.play().catch(() => {});
  },
  music(name) {
    if (musicEl) { musicEl.pause(); musicEl = null; }
    const s = sounds[name];
    if (!enabled || !s) return;
    musicEl = s; musicEl.loop = true; musicEl.play().catch(() => {});
  },
  setEnabled(v) { enabled = v; if (!v && musicEl) musicEl.pause(); }
};
