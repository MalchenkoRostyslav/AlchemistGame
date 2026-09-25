using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlchemistApi.Models;

namespace AlchemistApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopController : ControllerBase
    {
        private readonly AlchemistGameContext _context;

        public ShopController(AlchemistGameContext context)
        {
            _context = context;
        }

        [HttpPost("sell")]
        public async Task<IActionResult> SellItem(int playerId, int itemId)
        {
            // 1. Знаходимо гравця та предмет в інвентарі
            var player = await _context.Players.FindAsync(playerId);
            var inventorySlot = await _context.PlayerInventories
                .Include(pi => pi.Item)
                .FirstOrDefaultAsync(pi => pi.PlayerId == playerId && pi.ItemId == itemId);

            if (player == null) return NotFound("Гравця не знайдено");
            if (inventorySlot == null || inventorySlot.Quantity <= 0) return BadRequest("У вас немає цього предмета");

            // 2. Нараховуємо золото та віднімаємо предмет
            int earnedGold = inventorySlot.Item.BasePrice;
            player.Gold += earnedGold;
            inventorySlot.Quantity -= 1;

            if (inventorySlot.Quantity == 0)
            {
                _context.PlayerInventories.Remove(inventorySlot);
            }

            await _context.SaveChangesAsync();

            // 3. Повертаємо новий баланс гравця
            return Ok(new { message = $"Продано за {earnedGold} 🪙", newBalance = player.Gold });
        }

        [HttpPost("buy")]
        public async Task<IActionResult> BuyItem(int playerId, int itemId)
        {
            var player = await _context.Players.FindAsync(playerId);
            var item = await _context.Items.FindAsync(itemId);

            if (player == null || item == null) return NotFound("Гравця або предмет не знайдено");

            // Ціна купівлі буде трохи вищою за ціну продажу (BasePrice * 2)
            int price = item.BasePrice * 2;

            if (player.Gold < price) return BadRequest("Недостатньо золота!");

            // Віднімаємо золото
            player.Gold -= price;

            // Додаємо предмет в інвентар
            var inventorySlot = _context.PlayerInventories
                .FirstOrDefault(pi => pi.PlayerId == playerId && pi.ItemId == itemId);

            if (inventorySlot != null) inventorySlot.Quantity += 1;
            else
            {
                _context.PlayerInventories.Add(new PlayerInventory
                {
                    PlayerId = playerId,
                    ItemId = itemId,
                    Quantity = 1
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Куплено {item.Name} за {price} 🪙", newBalance = player.Gold });
        }
    }
}