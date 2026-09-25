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
        public RecipeController(AlchemistGameContext context) { _context = context; }

        [HttpGet("available/{playerId}")]
        public async Task<IActionResult> GetAvailableRecipes(int playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound();

            int pLevel = player.CauldronLevel ?? 1;

            var recipes = await _context.Recipes
                .Include(r => r.ResultItem)
                .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.IngredientItem)
                .Where(r => r.RequiredLevel <= pLevel) // Віддаємо тільки доступні за рівнем
                .Select(r => new
                {
                    RecipeId = r.Id,
                    ResultName = r.ResultItem!.Name,
                    Ingredients = r.RecipeIngredients.Select(ri => $"{ri.QuantityNeeded}x {ri.IngredientItem!.Name}")
                })
                .ToListAsync();

            return Ok(recipes);
        }
    }
}