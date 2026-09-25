using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlchemistApi.Models; // Переконайся, що тут назва твого проєкту

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

        // Запит типу GET /api/inventory/1
        [HttpGet("{playerId}")]
        public async Task<IActionResult> GetPlayerInventory(int playerId)
        {
            // Витягуємо інвентар з бази разом із даними про предмети та їх рідкісність
            var inventory = await _context.PlayerInventories
                .Include(pi => pi.Item)
                    .ThenInclude(i => i.Rarity)
                .Where(pi => pi.PlayerId == playerId)
                .Select(pi => new
                {
                    ItemId = pi.ItemId,
                    Name = pi.Item!.Name, // Додано !
                    Quantity = pi.Quantity,
                    Type = pi.Item.ItemType,
                    Rarity = pi.Item.Rarity!.Name, // Додано !
                    Color = pi.Item.Rarity!.ColorHex, // Додано !
                    Icon = pi.Item.IconPath,
                    Price = pi.Item.BasePrice
                })
                .ToListAsync();

            if (!inventory.Any())
            {
                return NotFound("Інвентар порожній або гравця не існує.");
            }

            return Ok(inventory); // Повертаємо дані у форматі JSON
        }
    }
}