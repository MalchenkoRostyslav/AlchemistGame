using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlchemistApi.Models;
using AlchemistApi.Services;

namespace AlchemistApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly AlchemistGameContext _context;
        private readonly PlayerService _players;

        public InventoryController(AlchemistGameContext context, PlayerService players)
        {
            _context = context;
            _players = players;
        }

        // GET /api/inventory/1  (порожня сумка = порожній масив; price = ціна продажу з бонусом навичок)
        [HttpGet("{playerId:int}")]
        public async Task<IActionResult> GetPlayerInventory(int playerId)
        {
            if (!await _context.Players.AnyAsync(p => p.Id == playerId))
                return NotFound(new { message = "Гравця не знайдено" });

            var effects = await _players.GetEffectsAsync(playerId);
            decimal bonus = 1m + PlayerService.Effect(effects, GameRules.EffSellBonus) / 100m;

            var raw = await _context.PlayerInventories
                .Where(pi => pi.PlayerId == playerId)
                .OrderBy(pi => pi.Item.ItemType).ThenBy(pi => pi.Item.Name)
                .Select(pi => new
                {
                    ItemId = pi.ItemId,
                    Name = pi.Item.Name,
                    Quantity = pi.Quantity,
                    Type = pi.Item.ItemType,
                    Rarity = pi.Item.Rarity!.Name,
                    Color = pi.Item.Rarity!.ColorHex,
                    Icon = pi.Item.IconPath,
                    Price = pi.Item.BasePrice
                })
                .ToListAsync();

            var inventory = raw.Select(x => new
            {
                x.ItemId,
                x.Name,
                x.Quantity,
                x.Type,
                x.Rarity,
                x.Color,
                x.Icon,
                Price = (int)Math.Round(x.Price * bonus),
                Sellable = GameRules.IsSellable(x.Type)
            });

            return Ok(inventory);
        }
    }
}
