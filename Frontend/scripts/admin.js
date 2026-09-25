document.getElementById('add-item-form').addEventListener('submit', async (e) => {
    e.preventDefault(); // Зупиняємо перезавантаження сторінки

    const messageEl = document.getElementById('admin-message');
    messageEl.style.color = 'white';
    messageEl.innerText = 'Збереження...';

    // Збираємо дані з полів вводу
    const newItem = {
        name: document.getElementById('itemName').value,
        itemType: document.getElementById('itemType').value,
        rarityId: parseInt(document.getElementById('itemRarity').value),
        basePrice: parseInt(document.getElementById('itemPrice').value),
        iconPath: document.getElementById('itemIcon').value
    };

    try {
        const response = await fetch('http://localhost:5012/api/admin/add-item', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(newItem)
        });

        if (response.ok) {
            messageEl.style.color = '#00ff00';
            messageEl.innerText = await response.text();
            document.getElementById('add-item-form').reset(); // Очищаємо форму
        } else {
            messageEl.style.color = 'red';
            messageEl.innerText = await response.text();
        }
    } catch (error) {
        messageEl.style.color = 'red';
        messageEl.innerText = 'Помилка з\'єднання із сервером!';
        console.error(error);
    }
});


// Завантаження предметів для випадаючих списків
async function loadItemDropdowns() {
    try {
        const response = await fetch('http://localhost:5012/api/admin/items');
        if (response.ok) {
            const items = await response.json();
            const dropdowns = document.querySelectorAll('.item-dropdown');

            // Генеруємо HTML опцій
            let optionsHtml = '';
            items.forEach(item => {
                optionsHtml += `<option value="${item.id}">[${item.type}] ${item.name}</option>`;
            });

            // Вставляємо опції у всі селекти
            dropdowns.forEach(select => {
                // Зберігаємо першу опцію "Немає", якщо вона є
                const emptyOption = select.querySelector('option[value="0"]');
                select.innerHTML = emptyOption ? emptyOption.outerHTML + optionsHtml : optionsHtml;
            });
        }
    } catch (error) {
        console.error('Помилка завантаження списку предметів:', error);
    }
}

// Запускаємо при завантаженні сторінки
loadItemDropdowns();

// Відправка форми Рецепта
document.getElementById('add-recipe-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const msg = document.getElementById('recipe-message');
    msg.innerText = 'Збереження...';

    const recipeData = {
        resultItemId: parseInt(document.getElementById('recipeResult').value),
        requiredXp: parseInt(document.getElementById('recipeXp').value),
        ingredient1Id: parseInt(document.getElementById('recipeIng1').value),
        ingredient1Qty: parseInt(document.getElementById('recipeQty1').value),
        ingredient2Id: parseInt(document.getElementById('recipeIng2').value),
        ingredient2Qty: parseInt(document.getElementById('recipeQty2').value)
    };

    try {
        const response = await fetch('http://localhost:5012/api/admin/add-recipe', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(recipeData)
        });

        msg.style.color = response.ok ? '#00ff00' : 'red';
        msg.innerText = await response.text();
        if (response.ok) document.getElementById('add-recipe-form').reset();
    } catch (error) {
        msg.style.color = 'red';
        msg.innerText = 'Помилка сервера';
    }
});

// Відправка форми Квесту
document.getElementById('add-quest-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const msg = document.getElementById('quest-message');
    msg.innerText = 'Збереження...';

    const questData = {
        title: document.getElementById('questTitle').value,
        description: document.getElementById('questDesc').value,
        rewardGold: parseInt(document.getElementById('questGold').value),
        rewardXp: parseInt(document.getElementById('questXp').value),
        requiredItemId: parseInt(document.getElementById('questReqItem').value),
        requiredQuantity: parseInt(document.getElementById('questReqQty').value)
    };

    try {
        const response = await fetch('http://localhost:5012/api/admin/add-quest', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(questData)
        });

        msg.style.color = response.ok ? '#00ff00' : 'red';
        msg.innerText = await response.text();
        if (response.ok) document.getElementById('add-quest-form').reset();
    } catch (error) {
        msg.style.color = 'red';
        msg.innerText = 'Помилка сервера';
    }
});