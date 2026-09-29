using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlchemistApi.Models;

namespace AlchemistApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecipeController : ControllerBase
    {
        private readonly AlchemistGameContext _context;

        public RecipeController(AlchemistGameContext context)
        {
            _context = context;
        }

        // СУМІСНІСТЬ: простий список відкритих рецептів
        [HttpGet("available/{playerId:int}")]
        public async Task<IActionResult> GetAvailableRecipes(int playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { message = "Гравця не знайдено" });

            var raw = await _context.PlayerRecipes
                .Where(pr => pr.PlayerId == playerId)
                .Select(pr => new
                {
                    RecipeId = pr.RecipeId,
                    ResultName = pr.Recipe.ResultItem!.Name,
                    Ingredients = pr.Recipe.RecipeIngredients
                        .Select(ri => new { ri.QuantityNeeded, Name = ri.IngredientItem.Name })
                        .ToList()
                })
                .ToListAsync();

            var recipes = raw.Select(r => new
            {
                r.RecipeId,
                r.ResultName,
                Ingredients = r.Ingredients.Select(i => $"{i.QuantityNeeded}x {i.Name}").ToList()
            });

            return Ok(recipes);
        }

        // КНИГА РЕЦЕПТІВ: відкриті рецепти зі структурованими інгредієнтами та прогресом (є / потрібно)
        [HttpGet("book/{playerId:int}")]
        public async Task<IActionResult> GetBook(int playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { message = "Гравця не знайдено" });

            var inv = await _context.PlayerInventories
                .Where(pi => pi.PlayerId == playerId)
                .ToDictionaryAsync(pi => pi.ItemId, pi => pi.Quantity);

            var raw = await _context.PlayerRecipes
                .Where(pr => pr.PlayerId == playerId)
                .Select(pr => new
                {
                    RecipeId = pr.RecipeId,
                    ResultItemId = pr.Recipe.ResultItemId,
                    Name = pr.Recipe.ResultItem!.Name,
                    Icon = pr.Recipe.ResultItem.IconPath,
                    Color = pr.Recipe.ResultItem.Rarity!.ColorHex,
                    RequiredLevel = pr.Recipe.RequiredLevel,
                    Ingredients = pr.Recipe.RecipeIngredients.Select(ri => new
                    {
                        ItemId = ri.IngredientItemId,
                        Name = ri.IngredientItem.Name,
                        Icon = ri.IngredientItem.IconPath,
                        Quantity = ri.QuantityNeeded
                    }).ToList()
                })
                .ToListAsync();

            var recipes = raw.Select(r =>
            {
                var ings = r.Ingredients.Select(i => new
                {
                    itemId = i.ItemId,
                    name = i.Name,
                    icon = i.Icon,
                    quantity = i.Quantity,
                    have = inv.TryGetValue(i.ItemId, out var h) ? h : 0
                }).ToList();

                return new
                {
                    recipeId = r.RecipeId,
                    resultItemId = r.ResultItemId,
                    name = r.Name,
                    icon = r.Icon,
                    color = r.Color,
                    requiredLevel = r.RequiredLevel,
                    ingredients = ings,
                    canBrew = ings.Count > 0 && ings.All(i => i.have >= i.quantity)
                };
            }).ToList();

            int totalCount = await _context.Recipes.CountAsync();
            return Ok(new { knownCount = recipes.Count, totalCount, recipes });
        }
    }
}