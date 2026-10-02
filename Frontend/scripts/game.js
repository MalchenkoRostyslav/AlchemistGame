// API_BASE береться з scripts/config.js
const TOKEN_KEY = 'alchemist_token';
let token = localStorage.getItem(TOKEN_KEY);

const $ = id => document.getElementById(id);
const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

function setMsg(text, color = 'white') {
    const el = $('craft-message');
    el.innerText = text;
    el.style.color = color;
}

// Усі запити до API йдуть через цю функцію (додає JWT-токен)
async function api(path, options = {}) {
    const headers = { ...(options.headers || {}) };
    if (token) headers['Authorization'] = 'Bearer ' + token;
    if (options.body) headers['Content-Type'] = 'application/json';
    const res = await fetch(API_BASE + path, { ...options, headers });
    if (res.status === 401 && token) { // токен протермінований або недійсний
        logout();
        $('auth-message').style.color = '#ef4444';
        $('auth-message').innerText = 'Сесія закінчилась, увійдіть знову';
        throw new Error('unauthorized');
    }
    return res;
}

// --- АВТОРИЗАЦІЯ ---
async function submitAuth(kind) {
    const nickname = $('auth-nick').value.trim();
    const password = $('auth-pass').value;
    const msg = $('auth-message');
    if (!nickname || !password) {
        msg.style.color = '#ef4444';
        msg.innerText = 'Введіть ім\'я та пароль';
        return;
    }
    msg.style.color = '#94a3b8';
    msg.innerText = 'Підключення до сервера… (перший запит за день може тривати до хвилини)';
    try {
        const res = await api('/auth/' + kind, { method: 'POST', body: JSON.stringify({ nickname, password }) });
        if (res.ok) {
            const data = await res.json();
            token = data.token;
            localStorage.setItem(TOKEN_KEY, token);
            $('auth-pass').value = '';
            msg.innerText = '';
            startGame();
        } else {
            msg.style.color = '#ef4444';
            msg.innerText = await res.text();
        }
    } catch (e) {
        if (e.message !== 'unauthorized') {
            msg.style.color = '#ef4444';
            msg.innerText = 'Немає з\'єднання із сервером';
        }
    }
}

function logout() {
    token = null;
    localStorage.removeItem(TOKEN_KEY);
    $('game-ui').classList.add('hidden');
    $('main-menu').classList.remove('hidden');
}

async function deleteAccount() {
    if (!confirm('Видалити акаунт назавжди разом з усім прогресом?')) return;
    try {
        const res = await api('/player/me', { method: 'DELETE' });
        if (res.ok) { logout(); alert('Акаунт видалено'); }
        else alert(await res.text());
    } catch (e) { /* 401 вже оброблено */ }
}

function startGame() {
    $('main-menu').classList.add('hidden');
    $('game-ui').classList.remove('hidden');
    $('craft-message').innerText = '';
    loadPlayerStats();
    loadInventory();
    loadRecipes();
    loadShop();
    loadQuests();
}

// Відновлення сесії після перезавантаження сторінки
async function init() {
    $('auth-pass').addEventListener('keydown', e => { if (e.key === 'Enter') submitAuth('login'); });
    if (!token) return;
    const msg = $('auth-message');
    msg.style.color = '#94a3b8';
    msg.innerText = 'Підключення до сервера…';
    try {
        const res = await api('/player/me');
        if (res.ok) { msg.innerText = ''; startGame(); }
        else { logout(); msg.innerText = ''; }
    } catch (e) { msg.innerText = ''; }
}

// --- ІГРОВА ЛОГІКА ---
async function loadPlayerStats() {
    try {
        const res = await api('/player/me');
        if (!res.ok) return;
        const p = await res.json();
        $('player-name').innerText = p.name;
        $('player-gold').innerText = p.gold;
        $('player-level').innerText = p.level;
        $('player-xp').innerText = `${p.xp}/${p.xpNeeded}`;
        $('player-energy').innerText = p.energy;
        $('player-max-energy').innerText = p.maxEnergy;
        $('player-energy').style.color = p.energy < 10 ? '#ef4444' : 'inherit';
        $('upgrade-btn').innerText = p.nextUpgradeCost == null
            ? '⭐ Максимальний рівень'
            : `⬆️ Покращити казанок (${p.nextUpgradeCost} 🪙)`;
    } catch (e) { console.error(e); }
}

async function loadInventory() {
    const grid = $('inventory-grid');
    try {
        const res = await api('/inventory');
        if (!res.ok) throw new Error();
        const items = await res.json();
        if (items.length === 0) {
            grid.innerHTML = '<p style="color:#94a3b8;font-size:12px;grid-column:1/-1;text-align:center;">Сумка порожня</p>';
            return;
        }
        grid.innerHTML = items.map(item => `
            <div class="item-card" style="border-color:${esc(item.color)}" title="${esc(item.rarity)}">
                <div class="qty-badge">x${item.quantity}</div>
                <img src="${esc(item.icon)}" alt="" style="width:40px;height:40px;" onerror="this.style.display='none'">
                <div style="font-size:12px;margin:8px 0;">${esc(item.name)}</div>
                ${item.type === 'Potion'
                    ? `<button onclick="sellItem(${item.itemId})" style="background:#f59e0b;color:black;border:none;border-radius:4px;font-size:10px;padding:4px;width:100%;cursor:pointer;">Продати (${item.price}🪙)</button>`
                    : ''}
            </div>`).join('');
    } catch (e) { if (e.message !== 'unauthorized') grid.innerHTML = 'Помилка'; }
}

async function loadRecipes() {
    const select = $('recipe-select');
    try {
        const res = await api('/recipe/available');
        if (!res.ok) return;
        const recipes = await res.json();
        if (recipes.length === 0) {
            select.innerHTML = '<option disabled>Немає доступних рецептів</option>';
            return;
        }
        const previous = select.value;
        select.innerHTML = recipes.map(r =>
            `<option value="${r.recipeId}">${esc(r.resultName)} (Потрібно: ${esc(r.ingredients.join(' + '))})</option>`).join('');
        if (previous) select.value = previous;
    } catch (e) { if (e.message !== 'unauthorized') select.innerHTML = '<option disabled>Помилка бази даних</option>'; }
}

async function loadShop() {
    const list = $('shop-list');
    try {
        const res = await api('/shop');
        if (!res.ok) return;
        const items = await res.json();
        if (items.length === 0) { list.innerHTML = '<p style="color:#94a3b8;font-size:12px;">Торговець поки нічого не продає</p>'; return; }
        list.innerHTML = items.map(i => `
            <div style="display:flex;align-items:center;gap:10px;background:#0f172a;border:1px solid ${esc(i.color)};border-radius:8px;padding:8px;">
                <img src="${esc(i.icon)}" alt="" style="width:32px;height:32px;" onerror="this.style.display='none'">
                <div style="flex:1;font-size:13px;">${esc(i.name)}<div style="color:#f59e0b;font-size:12px;">${i.price} 🪙</div></div>
                <button class="btn" style="width:auto;padding:6px 10px;font-size:12px;background:#334155;color:white;" ${i.locked ? 'disabled' : ''}
                    onclick="buyItem(${i.itemId})">${i.locked ? '🔒 Рів. ' + i.requiredLevel : 'Купити'}</button>
            </div>`).join('');
    } catch (e) { console.error(e); }
}

// Варіння
$('craft-btn').addEventListener('click', async () => {
    const recipeId = $('recipe-select').value;
    if (!recipeId) return;
    setMsg('Варимо...');
    try {
        const res = await api(`/craft?recipeId=${recipeId}`, { method: 'POST' });
        if (res.ok) {
            const data = await res.json();
            setMsg(data.message, data.leveledUp ? '#10b981' : '#38bdf8');
            if (data.leveledUp) { loadRecipes(); loadShop(); }
        } else {
            setMsg(await res.text(), '#ef4444');
        }
        loadPlayerStats();
        loadInventory();
    } catch (e) { if (e.message !== 'unauthorized') setMsg('Помилка сервера', '#ef4444'); }
});

// Похід у ліс
$('gather-btn').addEventListener('click', async () => {
    const btn = $('gather-btn');
    btn.disabled = true;
    try {
        const res = await api('/gather', { method: 'POST' });
        if (res.ok) setMsg((await res.json()).message, '#10b981');
        else setMsg(await res.text(), '#ef4444');
        loadPlayerStats();
        loadInventory();
    } catch (e) { if (e.message !== 'unauthorized') setMsg('Помилка сервера', '#ef4444'); }
    finally { setTimeout(() => { btn.disabled = false; }, 1000); }
});

async function upgradeCauldron() {
    try {
        const res = await api('/player/upgrade-cauldron', { method: 'POST' });
        const text = await res.text();
        setMsg(text, res.ok ? '#10b981' : '#ef4444');
        if (res.ok) { loadPlayerStats(); loadRecipes(); loadShop(); }
    } catch (e) { if (e.message !== 'unauthorized') setMsg('Помилка з\'єднання', '#ef4444'); }
}

async function buyItem(itemId) {
    try {
        const res = await api(`/shop/buy?itemId=${itemId}&quantity=1`, { method: 'POST' });
        if (res.ok) { setMsg((await res.json()).message, '#10b981'); loadPlayerStats(); loadInventory(); }
        else setMsg(await res.text(), '#ef4444');
    } catch (e) { console.error(e); }
}

async function sellItem(itemId) {
    try {
        const res = await api(`/shop/sell?itemId=${itemId}&quantity=1`, { method: 'POST' });
        if (res.ok) { setMsg((await res.json()).message, '#f59e0b'); loadPlayerStats(); loadInventory(); loadQuests(); }
        else setMsg(await res.text(), '#ef4444');
    } catch (e) { console.error(e); }
}

// Квести
async function loadQuests() {
    const board = $('quest-board');
    try {
        const res = await api('/quest');
        if (!res.ok) return;
        const quests = await res.json();
        if (quests.length === 0) {
            board.innerHTML = '<p style="color:#94a3b8;font-size:14px;">Немає активних завдань.</p>';
            return;
        }
        board.innerHTML = quests.map(q => {
            const reqText = q.requirements.map(r =>
                `<span style="color:${r.have >= r.quantityNeeded ? '#10b981' : '#f8fafc'}">${esc(r.itemName)} ${r.have}/${r.quantityNeeded}</span>`).join(', ');
            return `
            <div style="min-width:300px;background:rgba(0,0,0,0.2);border:1px solid #334155;border-left:3px solid #6366f1;border-radius:8px;padding:15px;display:flex;flex-direction:column;justify-content:space-between;">
                <div>
                    <div style="display:flex;justify-content:space-between;margin-bottom:5px;gap:10px;">
                        <strong style="color:white;">${esc(q.title)}</strong>
                        <span style="color:#f59e0b;font-size:12px;font-weight:bold;white-space:nowrap;">${q.rewardGold} 🪙 | ${q.rewardXp} XP</span>
                    </div>
                    <p style="font-size:12px;color:#94a3b8;margin-bottom:10px;">${esc(q.description)}</p>
                    <div style="font-size:12px;background:#0f172a;padding:5px;border-radius:4px;margin-bottom:10px;">Вимога: ${reqText}</div>
                </div>
                <button class="btn" style="background:transparent;border:1px solid #334155;color:white;" onclick="turnInQuest(${q.questId})">Здати ресурси</button>
            </div>`;
        }).join('');
    } catch (e) { console.error(e); }
}

async function turnInQuest(questId) {
    try {
        const res = await api(`/quest/turnin?questId=${questId}`, { method: 'POST' });
        if (res.ok) {
            setMsg(await res.text(), '#6366f1');
            loadPlayerStats(); loadInventory(); loadQuests(); loadRecipes(); loadShop();
        } else setMsg(await res.text(), '#ef4444');
    } catch (e) { console.error(e); }
}

init();
