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

        public InventoryController(AlchemistGameContext context)
        {
            _context = context;
        }

        // GET /api/inventory/1  (порожня сумка = порожній масив, а не 404)
        [HttpGet("{playerId:int}")]
        public async Task<IActionResult> GetPlayerInventory(int playerId)
        {
            if (!await _context.Players.AnyAsync(p => p.Id == playerId))
                return NotFound(new { message = "Гравця не знайдено" });

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
                x.Price,
                Sellable = GameRules.IsSellable(x.Type)
            });

            return Ok(inventory);
        }
    }
}