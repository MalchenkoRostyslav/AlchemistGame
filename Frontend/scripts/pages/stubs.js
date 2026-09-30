// Тимчасові сторінки. Кожну замінимо повноцінною на своєму етапі (4: котел, 5: магазин, 6: квести, 7: прокачка).
import { register } from '../core/router.js';
import { el, itemCard } from '../core/ui.js';

const demoItems = () => el('div', { class: 'grid-items' },
  itemCard({ id: -1, name: 'Мандрагора', color: '#b98a3e', rarity: 'Звичайний', type: 'Ingredient', quantity: 5,
    description: 'Корінь із людським обличчям.', properties: [{ name: 'Земля', description: 'Стабілізує суміш', color: '#8a6a3a' }] }),
  itemCard({ id: -2, name: 'Ельфійська роса', color: '#4a7fb0', rarity: 'Рідкісний', type: 'Ingredient', quantity: 2,
    description: 'Збирається на світанку.', properties: [{ name: 'Волога', description: 'Розчиняє інші компоненти', color: '#4a7fb0' }] }),
  itemCard({ id: -3, name: 'Зілля сили', color: '#7f5aa8', rarity: 'Епічний', type: 'Potion', quantity: 1, quality: 2,
    description: 'Наповнює тіло силою.' }));

const stub = (title, stage, extra) => ({
  render(view) {
    view.append(el('section', { class: 'panel page-stub' },
      el('h1', { class: 'page-title' }, title),
      el('p', { class: 'muted' }, `Ця сторінка буде реалізована на Етапі ${stage}.`),
      extra ? extra() : null));
  }
});

