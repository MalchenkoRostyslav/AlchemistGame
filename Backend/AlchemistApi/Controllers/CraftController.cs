using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlchemistApi.Models;
using AlchemistApi.Services;

namespace AlchemistApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CraftController : ControllerBase
    {
        private readonly AlchemistGameContext _context;
        private readonly PlayerService _players;

        public CraftController(AlchemistGameContext context, PlayerService players)
        {
            _context = context;
            _players = players;
        }

        public class BrewIngredientDto
        {
            public int ItemId { get; set; }
            public int Quantity { get; set; }
        }

        public class BrewRequestDto
        {
            public int PlayerId { get; set; }
            public List<BrewIngredientDto> Ingredients { get; set; } = new();
        }

        // ВІЛЬНЕ ЗМІШУВАННЯ: гравець кладе в котел будь-які інгредієнти.
        // POST /api/craft/brew   { "playerId": 1, "ingredients": [ { "itemId": 1, "quantity": 2 }, ... ] }
        [HttpPost("brew")]
        public async Task<IActionResult> Brew([FromBody] BrewRequestDto request)
        {
            var mix = new Dictionary<int, int>();
            foreach (var ing in request.Ingredients ?? new List<BrewIngredientDto>())
            {
                if (ing.Quantity <= 0 || ing.Quantity > GameRules.MaxQuantityPerIngredient)
                    return BadRequest(new { message = $"Кількість кожного інгредієнта має бути від 1 до {GameRules.MaxQuantityPerIngredient}." });

                mix[ing.ItemId] = (mix.TryGetValue(ing.ItemId, out var existing) ? existing : 0) + ing.Quantity;
            }

            return await BrewCore(request.PlayerId, mix);
        }

        // СУМІСНІСТЬ зі старим інтерфейсом: варіння за id рецепта (клік по рецепту в книзі).
        // POST /api/craft?playerId=1&recipeId=3
        [HttpPost]
        public async Task<IActionResult> CraftItem(int playerId, int recipeId)
        {
            var recipe = await _context.Recipes
                .Include(r => r.RecipeIngredients)
                .FirstOrDefaultAsync(r => r.Id == recipeId);
            if (recipe == null) return NotFound(new { message = "Рецепт не знайдено." });

            var mix = recipe.RecipeIngredients.ToDictionary(ri => ri.IngredientItemId, ri => ri.QuantityNeeded);
            return await BrewCore(playerId, mix);
        }

        // ---------- Спільне ядро варіння ----------
        private async Task<IActionResult> BrewCore(int playerId, Dictionary<int, int> mix)
        {
            if (mix.Count == 0)
                return BadRequest(new { message = "Котел порожній: додайте інгредієнти." });

            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { message = "Гравця не знайдено." });

            // 1. Місткість казана
            int cauldronLevel = player.CauldronLevel ?? 1;
            var cauldron = await _context.CauldronUpgrades.FirstOrDefaultAsync(c => c.Level == cauldronLevel);
            int slots = Math.Max(1, cauldron?.MaxIngredients ?? 2);
            decimal timeModifier = cauldron?.CraftTimeModifier ?? 1.0m;

            if (mix.Count > slots)
                return BadRequest(new { message = $"Ваш казан вміщує не більше {slots} різних інгредієнтів." });

            // 2. Перевірка предметів: існують і є інгредієнтами
            var itemIds = mix.Keys.ToList();
            var items = await _context.Items.Where(i => itemIds.Contains(i.Id)).ToListAsync();
            if (items.Count != itemIds.Count)
                return BadRequest(new { message = "Один з інгредієнтів не існує." });
            if (items.Any(i => i.ItemType != GameRules.TypeIngredient))
                return BadRequest(new { message = "У котел можна класти лише інгредієнти." });

            // 3. Перевірка наявності в сумці
            var inventory = await _context.PlayerInventories
                .Where(pi => pi.PlayerId == playerId)
                .ToListAsync();

            foreach (var (itemId, qty) in mix)
            {
                var slot = inventory.FirstOrDefault(pi => pi.ItemId == itemId);
                if (slot == null || slot.Quantity < qty)
                {
                    string name = items.First(i => i.Id == itemId).Name;
                    return BadRequest(new { message = $"Недостатньо інгредієнта: {name}." });
                }
            }

            // 4. Визначаємо результат ДО списання ресурсів
            var recipes = await _context.Recipes.Include(r => r.RecipeIngredients).ToListAsync();
            var matched = recipes.FirstOrDefault(r =>
                r.RecipeIngredients.Count == mix.Count &&
                r.RecipeIngredients.All(ri => mix.TryGetValue(ri.IngredientItemId, out var q) && q == ri.QuantityNeeded));

            int? matchedId = matched?.Id;
            bool known = matchedId != null &&
                await _context.PlayerRecipes.AnyAsync(pr => pr.PlayerId == playerId && pr.RecipeId == matchedId);

            string outcome;
            Item? resultItem;
            if (matched != null && known && matched.ResultItemId != null)
            {
                outcome = "Success";
                resultItem = await _context.Items.FindAsync(matched.ResultItemId.Value);
                if (resultItem == null)
                    return StatusCode(500, new { message = "Результат рецепта не існує в базі даних." });
            }
            else
            {
                // Рецепт існує, але не відкритий гравцем -> Невідоме зілля; випадкова суміш -> Отрута
                outcome = matched != null ? "Unknown" : "Poison";
                string failType = matched != null ? GameRules.TypeUnknown : GameRules.TypePoison;
                resultItem = await _context.Items.FirstOrDefaultAsync(i => i.ItemType == failType);
                if (resultItem == null)
                    return StatusCode(500, new { message = $"У базі немає предмета типу '{failType}'. Запустіть SQL-скрипт міграції." });
            }

            // 5. Енергія
            if (!_players.TrySpendEnergy(player, GameRules.BrewEnergyCost))
                return BadRequest(new { message = $"Недостатньо енергії для варіння! (Потрібно {GameRules.BrewEnergyCost} ⚡)" });

            // 6. Списуємо інгредієнти
            foreach (var (itemId, qty) in mix)
            {
                var slot = inventory.First(pi => pi.ItemId == itemId);
                slot.Quantity -= qty;
                if (slot.Quantity == 0) _context.PlayerInventories.Remove(slot);
            }

            // 7. Видаємо результат
            var resultSlot = inventory.FirstOrDefault(pi =>
                pi.ItemId == resultItem.Id && _context.Entry(pi).State != EntityState.Deleted);
            if (resultSlot != null) resultSlot.Quantity += 1;
            else _context.PlayerInventories.Add(new PlayerInventory { PlayerId = playerId, ItemId = resultItem.Id, Quantity = 1 });

            // 8. Досвід і рівень
            int xpGained = outcome == "Success" ? GameRules.BrewSuccessXp : GameRules.BrewFailXp;
            var levelUp = _players.AddExperience(player, xpGained);
            var unlocked = levelUp.LeveledUp
                ? await _players.GrantAutoRecipesAsync(player)
                : new List<string>();

            await _context.SaveChangesAsync();

            string message = outcome switch
            {
                "Success" => levelUp.LeveledUp ? $"Зілля зварено! Рівень підвищено до {player.Level}!" : $"Зварено: {resultItem.Name}!",
                "Unknown" => "Суміш булькає дивним кольором... Вийшло невідоме зілля.",
                _ => "Суміш закипіла й димить. Вийшла отрута!"
            };

            return Ok(new
            {
                outcome,                      // "Success" | "Unknown" | "Poison"
                message,
                result = new { itemId = resultItem.Id, name = resultItem.Name, icon = resultItem.IconPath, type = resultItem.ItemType },
                xpGained,
                xp = player.Experience ?? 0,
                xpToNext = GameRules.XpForLevel(player.Level),
                level = player.Level,
                leveledUp = levelUp.LeveledUp,
                skillPoints = player.SkillPoints,
                unlockedRecipes = unlocked,
                energy = player.Energy,
                craftTimeModifier = timeModifier
            });
        }
    }
}