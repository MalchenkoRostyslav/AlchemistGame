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

        private static int Seconds(int baseSeconds, decimal modifier) =>
            Math.Max(1, (int)Math.Round(baseSeconds * modifier));

        // ПОЧАТИ ВАРІННЯ: списує інгредієнти й енергію, запускає таймер. Результат приховано до завершення.
        // POST /api/craft/start  { "playerId": 1, "ingredients": [ { "itemId": 1, "quantity": 2 } ] }
        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] BrewRequestDto request)
        {
            var mix = new Dictionary<int, int>();
            foreach (var ing in request.Ingredients ?? new List<BrewIngredientDto>())
            {
                if (ing.Quantity <= 0 || ing.Quantity > GameRules.MaxQuantityPerIngredient)
                    return BadRequest(new { message = $"Кількість кожного інгредієнта має бути від 1 до {GameRules.MaxQuantityPerIngredient}." });
                mix[ing.ItemId] = (mix.TryGetValue(ing.ItemId, out var existing) ? existing : 0) + ing.Quantity;
            }
            if (mix.Count == 0) return BadRequest(new { message = "Котел порожній: додайте інгредієнти." });

            int playerId = request.PlayerId;
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { message = "Гравця не знайдено." });

            if (await _context.PlayerBrews.AnyAsync(b => b.PlayerId == playerId))
                return BadRequest(new { message = "Казан зайнятий: заберіть зілля або дочекайтесь завершення." });

            // Місткість і швидкість казана
            int cauldronLevel = player.CauldronLevel ?? 1;
            var cauldron = await _context.CauldronUpgrades.FirstOrDefaultAsync(c => c.Level == cauldronLevel);
            int slots = Math.Max(1, cauldron?.MaxIngredients ?? 2);
            decimal timeModifier = cauldron?.CraftTimeModifier ?? 1.0m;
            timeModifier *= PlayerService.BrewTimeFactor(await _players.GetEffectsAsync(playerId));
            if (mix.Count > slots)
                return BadRequest(new { message = $"Ваш казан вміщує не більше {slots} різних інгредієнтів." });

            // Предмети
            var itemIds = mix.Keys.ToList();
            var items = await _context.Items.Where(i => itemIds.Contains(i.Id)).ToListAsync();
            if (items.Count != itemIds.Count) return BadRequest(new { message = "Один з інгредієнтів не існує." });
            if (items.Any(i => i.ItemType != GameRules.TypeIngredient))
                return BadRequest(new { message = "У котел можна класти лише інгредієнти." });

            // Наявність у сумці
            var inventory = await _context.PlayerInventories.Where(pi => pi.PlayerId == playerId).ToListAsync();
            foreach (var (itemId, qty) in mix)
            {
                var slot = inventory.FirstOrDefault(pi => pi.ItemId == itemId);
                if (slot == null || slot.Quantity < qty)
                    return BadRequest(new { message = $"Недостатньо інгредієнта: {items.First(i => i.Id == itemId).Name}." });
            }

            // Результат (визначаємо до списання ресурсів)
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
                if (resultItem == null) return StatusCode(500, new { message = "Результат рецепта не існує в базі даних." });
            }
            else
            {
                outcome = matched != null ? "Unknown" : "Poison";
                string failType = matched != null ? GameRules.TypeUnknown : GameRules.TypePoison;
                resultItem = await _context.Items.FirstOrDefaultAsync(i => i.ItemType == failType);
                if (resultItem == null)
                    return StatusCode(500, new { message = $"У базі немає предмета типу '{failType}'. Запустіть SQL-скрипт міграції." });
            }

            if (!_players.TrySpendEnergy(player, GameRules.BrewEnergyCost))
                return BadRequest(new { message = $"Недостатньо енергії для варіння! (Потрібно {GameRules.BrewEnergyCost} ⚡)" });

            foreach (var (itemId, qty) in mix)
            {
                var slot = inventory.First(pi => pi.ItemId == itemId);
                slot.Quantity -= qty;
                if (slot.Quantity == 0) _context.PlayerInventories.Remove(slot);
            }

            int baseSeconds = outcome == "Success" ? matched!.BrewTimeSeconds : GameRules.FailBrewSeconds;
            int seconds = Seconds(baseSeconds, timeModifier);
            var now = DateTime.UtcNow;
            _context.PlayerBrews.Add(new PlayerBrew
            {
                PlayerId = playerId,
                Outcome = outcome,
                ResultItemId = resultItem.Id,
                StartedAt = now,
                ReadyAt = now.AddSeconds(seconds)
            });

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Не вдалося почати варіння (можливо, казан вже зайнятий)." });
            }

            return Ok(new
            {
                message = "Суміш закипає... Зачекайте, поки зілля звариться.",
                totalSeconds = seconds,
                readyAt = GameRules.AsUtc(now.AddSeconds(seconds)),
                energy = player.Energy
            });
        }

        // СТАТУС казана (результат не розкривається)
        [HttpGet("status/{playerId:int}")]
        public async Task<IActionResult> Status(int playerId)
        {
            var brew = await _context.PlayerBrews.FirstOrDefaultAsync(b => b.PlayerId == playerId);
            if (brew == null) return Ok(new { active = false });

            int left = (int)Math.Max(0d, Math.Ceiling((brew.ReadyAt - DateTime.UtcNow).TotalSeconds));
            int total = (int)Math.Round((brew.ReadyAt - brew.StartedAt).TotalSeconds);
            return Ok(new
            {
                active = true,
                ready = left == 0,
                secondsLeft = left,
                totalSeconds = total,
                readyAt = GameRules.AsUtc(brew.ReadyAt)
            });
        }

        // ЗАБРАТИ готове зілля
        [HttpPost("collect")]
        public async Task<IActionResult> Collect(int playerId)
        {
            var brew = await _context.PlayerBrews.FirstOrDefaultAsync(b => b.PlayerId == playerId);
            if (brew == null) return BadRequest(new { message = "У казані нічого немає." });

            int left = (int)Math.Max(0d, Math.Ceiling((brew.ReadyAt - DateTime.UtcNow).TotalSeconds));
            if (left > 0) return BadRequest(new { message = $"Зілля ще вариться (залишилось {left} с).", secondsLeft = left });

            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { message = "Гравця не знайдено." });

            var resultItem = await _context.Items.FindAsync(brew.ResultItemId);
            if (resultItem == null) return StatusCode(500, new { message = "Результат варіння не існує в базі даних." });

            var slot = await _context.PlayerInventories
                .FirstOrDefaultAsync(pi => pi.PlayerId == playerId && pi.ItemId == resultItem.Id);
            if (slot != null) slot.Quantity += 1;
            else _context.PlayerInventories.Add(new PlayerInventory { PlayerId = playerId, ItemId = resultItem.Id, Quantity = 1 });

            string outcome = brew.Outcome;
            _context.PlayerBrews.Remove(brew);

            int xpGained = outcome == "Success" ? GameRules.BrewSuccessXp : GameRules.BrewFailXp;
            var levelUp = _players.AddExperience(player, xpGained);
            var unlocked = levelUp.LeveledUp ? await _players.GrantAutoRecipesAsync(player) : new List<string>();

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "Зілля вже забрано." });
            }

            string message = outcome switch
            {
                "Success" => $"Зварено: {resultItem.Name}!",
                "Unknown" => "Суміш булькає дивним кольором... Вийшло невідоме зілля.",
                _ => "Суміш закипіла й димить. Вийшла отрута!"
            };

            return Ok(new
            {
                outcome,
                message,
                result = new { itemId = resultItem.Id, name = resultItem.Name, icon = resultItem.IconPath, type = resultItem.ItemType },
                xpGained,
                xp = player.Experience ?? 0,
                xpToNext = GameRules.XpForLevel(player.Level),
                level = player.Level,
                leveledUp = levelUp.LeveledUp,
                skillPoints = player.SkillPoints,
                unlockedRecipes = unlocked,
                energy = player.Energy
            });
        }
    }
}
