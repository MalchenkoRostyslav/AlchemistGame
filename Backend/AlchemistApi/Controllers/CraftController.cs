using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlchemistApi.Models;
using System;

namespace AlchemistApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CraftController : ControllerBase
    {
        private readonly AlchemistGameContext _context;

        public CraftController(AlchemistGameContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CraftItem(int playerId, int recipeId)
        {
            // 1. Шукаємо рецепт
            var recipe = await _context.Recipes
                .Include(r => r.RecipeIngredients)
                .FirstOrDefaultAsync(r => r.Id == recipeId);

            if (recipe == null) return NotFound("Рецепт не знайдено.");

            // Шукаємо гравця
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound("Гравця не знайдено.");

            // 2. ПЕРЕВІРКА ЕНЕРГІЇ (Нова механіка)
            if (player.Energy < 10)
            {
                return BadRequest("Недостатньо енергії для варіння! (Потрібно 10 ⚡)");
            }

            // 3. Витягуємо інвентар гравця
            var inventory = await _context.PlayerInventories
                .Where(pi => pi.PlayerId == playerId)
                .ToListAsync();

            // 4. Перевіряємо, чи вистачає ресурсів
            foreach (var req in recipe.RecipeIngredients)
            {
                var item = inventory.FirstOrDefault(pi => pi.ItemId == req.IngredientItemId);
                if (item == null || item.Quantity < req.QuantityNeeded)
                {
                    return BadRequest("Недостатньо інгредієнтів!");
                }
            }

            // 5. Віднімаємо інгредієнти та енергію
            player.Energy -= 10;
            player.LastEnergyUpdate = DateTime.Now;

            foreach (var req in recipe.RecipeIngredients)
            {
                var item = inventory.First(pi => pi.ItemId == req.IngredientItemId);
                item.Quantity -= req.QuantityNeeded;

                if (item.Quantity == 0) _context.PlayerInventories.Remove(item);
            }

            // 6. Додаємо готове зілля
            var craftedItem = inventory.FirstOrDefault(pi => pi.ItemId == recipe.ResultItemId);
            if (craftedItem != null)
            {
                craftedItem.Quantity += 1;
            }
            else
            {
                _context.PlayerInventories.Add(new PlayerInventory
                {
                    PlayerId = playerId,
                    ItemId = recipe.ResultItemId ?? 0,
                    Quantity = 1
                });
            }

            // Нараховуємо 35 XP за кожне зварене зілля
            player.Experience = (player.Experience ?? 0) + 35;
            bool leveledUp = false;

            // Формула рівня: кожен наступний рівень потребує більше XP (Рівень * 100)
            int currentLevel = player.CauldronLevel ?? 1;
            int xpRequired = currentLevel * 100;

            if (player.Experience >= xpRequired)
            {
                player.Experience -= xpRequired;
                player.CauldronLevel = currentLevel + 1;
                leveledUp = true;
            }

            // 7. Зберігаємо всі зміни в базу даних однією транзакцією
            await _context.SaveChangesAsync();

            // Повертаємо об'єкт із новими даними
            return Ok(new
            {
                message = leveledUp ? "Рівень підвищено!" : "Зілля успішно зварено!",
                xp = player.Experience,
                level = player.CauldronLevel,
                leveledUp = leveledUp
            });
        }
    }
}