let currentPlayerId = null;
const API_BASE = 'http://localhost:5012/api';

// --- МЕНЮ ТА ПРОФІЛІ ---
async function createNewGame() {
    const nickname = prompt("Введіть ім'я вашого алхіміка:");
    if (!nickname) return;
    try {
        const response = await fetch(`${API_BASE}/player/new?nickname=${nickname}`, { method: 'POST' });
        if (response.ok) {
            const newPlayer = await response.json();
            startGame(newPlayer.id);
        }
    } catch (e) { alert("Помилка з'єднання!"); }
}

async function showProfilesList(action) {
    const container = document.getElementById('profiles-container');
    const list = document.getElementById('profiles-list');
    const title = document.getElementById('profiles-title');

    container.classList.remove('hidden');
    list.innerHTML = 'Завантаження...';
    title.innerText = action === 'load' ? 'Оберіть збереження' : 'Оберіть для видалення';

    try {
        const response = await fetch(`${API_BASE}/player/profiles`);
        if (response.ok) {
            const profiles = await response.json();
            list.innerHTML = '';

            if (profiles.length === 0) {
                list.innerHTML = 'Профілів немає.';
                return;
            }

            profiles.forEach(p => {
                const btn = document.createElement('button');
                btn.className = 'btn';
                btn.innerText = `${p.name} (Рівень: ${p.level})`;

                if (action === 'load') {
                    btn.style.background = '#334155';
                    btn.style.color = 'white';
                    btn.onclick = () => startGame(p.id);
                } else {
                    btn.style.background = '#ef4444';
                    btn.style.color = 'white';
                    btn.onclick = () => deleteProfile(p.id, p.name);
                }
                list.appendChild(btn);
            });
        }
    } catch (e) { list.innerHTML = 'Помилка підключення.'; }
}

async function deleteProfile(id, name) {
    if (!confirm(`Видалити профіль ${name}?`)) return;
    try {
        const response = await fetch(`${API_BASE}/player/delete/${id}`, { method: 'DELETE' });
        if (response.ok) showProfilesList('delete');
    } catch (e) { alert("Помилка видалення"); }
}

function startGame(id) {
    currentPlayerId = id;
    document.getElementById('main-menu').classList.add('hidden');
    document.getElementById('game-ui').classList.remove('hidden');
    document.getElementById('craft-message').innerText = '';

    // Завантажуємо всі дані гри
    loadPlayerStats();
    loadInventory();
    loadRecipes();
    loadQuests();
}

function exitToMenu() {
    currentPlayerId = null;
    document.getElementById('game-ui').classList.add('hidden');
    document.getElementById('main-menu').classList.remove('hidden');
    document.getElementById('profiles-container').classList.add('hidden');
}


// --- ІГРОВА ЛОГІКА ---

async function loadPlayerStats() {
    if (!currentPlayerId) return;
    try {
        const response = await fetch(`${API_BASE}/player/${currentPlayerId}`);
        if (response.ok) {
            const p = await response.json();
            document.getElementById('player-gold').innerText = p.gold;
            document.getElementById('player-level').innerText = p.level;
            document.getElementById('player-xp').innerText = p.xp;

            // Нова механіка енергії
            document.getElementById('player-energy').innerText = p.energy;
            document.getElementById('player-max-energy').innerText = p.maxEnergy;

            // Якщо енергія мала, підсвічуємо червоним
            const engEl = document.getElementById('player-energy');
            engEl.style.color = p.energy < 10 ? '#ef4444' : 'inherit';
        }
    } catch (e) { console.error(e); }
}

async function loadInventory() {
    if (!currentPlayerId) return;
    const grid = document.getElementById('inventory-grid');
    grid.innerHTML = '<p style="color:#94a3b8; font-size: 12px; grid-column: 1/-1; text-align: center;">Завантаження...</p>';

    try {
        const response = await fetch(`${API_BASE}/inventory/${currentPlayerId}`);
        if (!response.ok) throw new Error();

        const items = await response.json();
        grid.innerHTML = '';

        if (items.length === 0) {
            grid.innerHTML = '<p style="color:#94a3b8; font-size: 12px; grid-column: 1/-1; text-align: center;">Сумка порожня</p>';
            return;
        }

        items.forEach(item => {
            const card = document.createElement('div');
            card.className = 'item-card';
            card.style.borderColor = item.color; // Колір рідкісності

            card.innerHTML = `
                <div class="qty-badge">x${item.quantity}</div>
                <img src="${item.icon}" alt="${item.name}" style="width: 40px; height: 40px;" onerror="this.style.display='none'">
                <div style="font-size: 12px; margin: 8px 0;">${item.name}</div>
                ${item.type === 'Potion' ? `<button onclick="sellItem(${item.itemId})" style="background:#f59e0b; color:black; border:none; border-radius:4px; font-size:10px; padding:4px; width:100\%; cursor:pointer;">Продати (${item.price}🪙)</button>` : ''}
            `;
            grid.appendChild(card);
        });
    } catch (e) { grid.innerHTML = 'Помилка'; }
}

// Завантаження динамічних рецептів
async function loadRecipes() {
    if (!currentPlayerId) return;
    const select = document.getElementById('recipe-select');
    select.innerHTML = '<option>Оновлення рецептів...</option>';

    try {
        const response = await fetch(`${API_BASE}/recipe/available/${currentPlayerId}`);
        if (response.ok) {
            const recipes = await response.json();
            select.innerHTML = '';

            if (recipes.length === 0) {
                select.innerHTML = '<option disabled>Немає доступних рецептів</option>';
                return;
            }

            recipes.forEach(r => {
                const opt = document.createElement('option');
                opt.value = r.recipeId;
                opt.text = `${r.resultName} (Потрібно: ${r.ingredients.join(' + ')})`;
                select.appendChild(opt);
            });
        }
    } catch (e) { select.innerHTML = '<option disabled>Помилка бази даних</option>'; }
}


// Крафт (Варіння)
document.getElementById('craft-btn').addEventListener('click', async () => {
    if (!currentPlayerId) return;
    const recipeId = document.getElementById('recipe-select').value;
    if (!recipeId) return;

    const msgEl = document.getElementById('craft-message');
    msgEl.innerText = 'Варимо...';
    msgEl.style.color = 'white';

    try {
        const response = await fetch(`${API_BASE}/craft?playerId=${currentPlayerId}&recipeId=${recipeId}`, { method: 'POST' });

        if (response.ok) {
            const res = await response.json();
            msgEl.style.color = res.leveledUp ? '#10b981' : '#38bdf8';
            msgEl.innerText = res.message;
            if (res.leveledUp) loadRecipes(); // Оновлюємо рецепти, якщо підняли рівень
        } else {
            msgEl.style.color = '#ef4444';
            msgEl.innerText = await response.text(); // Виведе "Недостатньо енергії" або "ресурсів"
        }

        loadPlayerStats(); // Оновлюємо енергію та XP
        loadInventory();   // Оновлюємо предмети
    } catch (e) { msgEl.innerText = 'Помилка сервера'; }
});


// Похід у ліс
document.getElementById('gather-btn').addEventListener('click', async () => {
    if (!currentPlayerId) return;
    const btn = document.getElementById('gather-btn');
    const msgEl = document.getElementById('craft-message');

    btn.disabled = true;

    try {
        const response = await fetch(`${API_BASE}/gather?playerId=${currentPlayerId}`, { method: 'POST' });

        if (response.ok) {
            const data = await response.json();
            msgEl.style.color = '#10b981';
            msgEl.innerText = data.message;
        } else {
            msgEl.style.color = '#ef4444';
            msgEl.innerText = await response.text(); // Повідомлення про брак енергії
        }

        loadPlayerStats(); // Оновлюємо енергію
        loadInventory();   // Оновлюємо лут
    } catch (e) { console.error(e); }
    finally {
        setTimeout(() => { btn.disabled = false; }, 1000); // Кулдаун 1 сек
    }
});


// Покращення казанка
async function upgradeCauldron() {
    if (!currentPlayerId) return;
    try {
        const response = await fetch(`${API_BASE}/player/upgrade-cauldron/${currentPlayerId}`, { method: 'POST' });
        if (response.ok) {
            alert(await response.text());
            loadPlayerStats(); // Оновити золото і рівень
            loadRecipes();     // Завантажити нові рецепти!
        } else {
            alert(await response.text());
        }
    } catch (e) { alert('Помилка з\'єднання'); }
}


// Магазин
async function buyItem(itemId) {
    if (!currentPlayerId) return;
    try {
        const response = await fetch(`${API_BASE}/shop/buy?playerId=${currentPlayerId}&itemId=${itemId}`, { method: 'POST' });
        const msgEl = document.getElementById('craft-message');

        if (response.ok) {
            const res = await response.json();
            msgEl.style.color = '#10b981';
            msgEl.innerText = res.message;
            loadPlayerStats();
            loadInventory();
        } else {
            msgEl.style.color = '#ef4444';
            msgEl.innerText = await response.text();
        }
    } catch (e) { console.error(e); }
}

async function sellItem(itemId) {
    if (!currentPlayerId) return;
    try {
        const response = await fetch(`${API_BASE}/shop/sell?playerId=${currentPlayerId}&itemId=${itemId}`, { method: 'POST' });
        if (response.ok) {
            const res = await response.json();
            document.getElementById('craft-message').innerText = res.message;
            document.getElementById('craft-message').style.color = '#f59e0b';
            loadPlayerStats();
            loadInventory();
        }
    } catch (e) { console.error(e); }
}


// Квести
async function loadQuests() {
    if (!currentPlayerId) return;
    const board = document.getElementById('quest-board');

    try {
        const response = await fetch(`${API_BASE}/quest/${currentPlayerId}`);
        if (response.ok) {
            const quests = await response.json();
            board.innerHTML = '';

            if (quests.length === 0) {
                board.innerHTML = '<p style="color: #94a3b8; font-size: 14px;">Немає активних завдань.</p>';
                return;
            }

            quests.forEach(quest => {
                const reqText = quest.requirements.map(r => `${r.itemName} (x${r.quantityNeeded})`).join(', ');

                board.innerHTML += `
                    <div style="min-width: 300px; background: rgba(0,0,0,0.2); border: 1px solid #334155; border-left: 3px solid #6366f1; border-radius: 8px; padding: 15px; display: flex; flex-direction: column; justify-content: space-between;">
                        <div>
                            <div style="display: flex; justify-content: space-between; margin-bottom: 5px;">
                                <strong style="color: white;">${quest.title}</strong>
                                <span style="color: #f59e0b; font-size: 12px; font-weight: bold;">${quest.rewardGold} 🪙 | ${quest.rewardXp} XP</span>
                            </div>
                            <p style="font-size: 12px; color: #94a3b8; margin-bottom: 10px;">${quest.description}</p>
                            <div style="font-size: 12px; background: #0f172a; padding: 5px; border-radius: 4px; margin-bottom: 10px;">Вимога: ${reqText}</div>
                        </div>
                        <button class="btn" style="background: transparent; border: 1px solid #334155; color: white;" onclick="turnInQuest(${quest.questId})">Здати ресурси</button>
                    </div>
                `;
            });
        }
    } catch (e) { console.error(e); }
}

async function turnInQuest(questId) {
    if (!currentPlayerId) return;
    const msgEl = document.getElementById('craft-message');
    try {
        const response = await fetch(`${API_BASE}/quest/turnin?playerId=${currentPlayerId}&questId=${questId}`, { method: 'POST' });
        if (response.ok) {
            msgEl.style.color = '#6366f1';
            msgEl.innerText = await response.text();
            loadPlayerStats();
            loadInventory();
            loadQuests();
        } else {
            msgEl.style.color = '#ef4444';
            msgEl.innerText = await response.text();
        }
    } catch (e) { console.error(e); }
}