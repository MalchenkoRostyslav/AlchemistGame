using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlchemistApi.Models;

namespace AlchemistApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AlchemistGameContext _context;

        public AdminController(AlchemistGameContext context)
        {
            _context = context;
        }

        // Створюємо DTO (Data Transfer Object) - структуру для прийому даних з HTML
        public class CreateItemDto
        {
            public string Name { get; set; } = string.Empty;
            public string ItemType { get; set; } = string.Empty;
            public int RarityId { get; set; }
            public int BasePrice { get; set; }
            public string IconPath { get; set; } = string.Empty;
        }

        [HttpPost("add-item")]
        public async Task<IActionResult> AddItem([FromBody] CreateItemDto dto)
        {
            // Перевіряємо, чи всі дані прийшли
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.ItemType))
            {
                return BadRequest("Назва та тип предмета обов'язкові!");
            }

            // Створюємо новий об'єкт для бази даних
            var newItem = new Item
            {
                Name = dto.Name,
                ItemType = dto.ItemType,
                RarityId = dto.RarityId,
                BasePrice = dto.BasePrice,
                IconPath = dto.IconPath
            };

            _context.Items.Add(newItem);
            await _context.SaveChangesAsync();

            return Ok($"Предмет '{newItem.Name}' успішно додано до бази з ID: {newItem.Id}!");
        }

        // DTO для рецептів та квестів
        public class CreateRecipeDto
        {
            public int ResultItemId { get; set; }
            public int RequiredXp { get; set; }
            public int Ingredient1Id { get; set; }
            public int Ingredient1Qty { get; set; }
            public int Ingredient2Id { get; set; }
            public int Ingredient2Qty { get; set; }
            public int RequiredLevel { get; set; } = 1;
        }

        public class CreateQuestDto
        {
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int RewardGold { get; set; }
            public int RewardXp { get; set; }
            public int RequiredItemId { get; set; }
            public int RequiredQuantity { get; set; }
        }

        // 1. Отримання списку предметів для випадаючих списків (Dropdowns)
        [HttpGet("items")]
        public async Task<IActionResult> GetItems()
        {
            var items = await _context.Items
                .Select(i => new { id = i.Id, name = i.Name, type = i.ItemType })
                .ToListAsync();
            return Ok(items);
        }

        // 2. Створення нового рецепта
        [HttpPost("add-recipe")]
        public async Task<IActionResult> AddRecipe([FromBody] CreateRecipeDto dto)
        {
            var recipe = new Recipe
            {
                ResultItemId = dto.ResultItemId,
                RequiredXp = dto.RequiredXp,
                RequiredLevel = Math.Max(1, dto.RequiredLevel)
            };
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync(); // Зберігаємо, щоб отримати ID рецепта

            if (dto.Ingredient1Id > 0 && dto.Ingredient1Qty > 0)
            {
                _context.RecipeIngredients.Add(new RecipeIngredient
                { RecipeId = recipe.Id, IngredientItemId = dto.Ingredient1Id, QuantityNeeded = dto.Ingredient1Qty });
            }

            if (dto.Ingredient2Id > 0 && dto.Ingredient2Qty > 0 && dto.Ingredient2Id != dto.Ingredient1Id)
            {
                _context.RecipeIngredients.Add(new RecipeIngredient
                { RecipeId = recipe.Id, IngredientItemId = dto.Ingredient2Id, QuantityNeeded = dto.Ingredient2Qty });
            }

            // Відкриваємо новий рецепт усім гравцям, які вже досягли потрібного рівня
            var playerIds = await _context.Players
                .Where(p => p.Level >= recipe.RequiredLevel)
                .Select(p => p.Id)
                .ToListAsync();
            foreach (var pid in playerIds)
            {
                _context.PlayerRecipes.Add(new PlayerRecipe { PlayerId = pid, RecipeId = recipe.Id, UnlockedAt = DateTime.UtcNow });
            }

            await _context.SaveChangesAsync();
            return Ok($"Рецепт успішно створено (ID: {recipe.Id})!");
        }

        // 3. Створення нового квесту
        [HttpPost("add-quest")]
        public async Task<IActionResult> AddQuest([FromBody] CreateQuestDto dto)
        {
            var quest = new Quest
            {
                Title = dto.Title,
                Description = dto.Description,
                RewardGold = dto.RewardGold,
                RewardXp = dto.RewardXp
            };
            _context.Quests.Add(quest);
            await _context.SaveChangesAsync();

            if (dto.RequiredItemId > 0 && dto.RequiredQuantity > 0)
            {
                _context.QuestRequirements.Add(new QuestRequirement
                { QuestId = quest.Id, ItemId = dto.RequiredItemId, QuantityNeeded = dto.RequiredQuantity });
                await _context.SaveChangesAsync();
            }

            return Ok($"Квест '{quest.Title}' успішно додано!");
        }
    }
}