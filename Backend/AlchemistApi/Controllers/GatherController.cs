using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlchemistApi.Models;
using System;

namespace AlchemistApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GatherController : ControllerBase
    {
        private readonly AlchemistGameContext _context;

        public GatherController(AlchemistGameContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> GoToForest(int playerId)
        {
            // 1. Шукаємо гравця та перевіряємо енергію
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound("Гравця не знайдено");

            if (player.Energy < 5)
            {
                return BadRequest("Недостатньо енергії для походу в ліс! (Потрібно 5 ⚡)");
            }

            // Віднімаємо енергію та оновлюємо таймер
            player.Energy -= 5;
            player.LastEnergyUpdate = DateTime.Now;

            // 2. Генерація випадкового луту (70% шанс на Воду, 30% на Мандрагору)
            var rnd = new Random();
            int dropItemId = rnd.Next(1, 100) > 70 ? 2 : 1; // 1 - Вода, 2 - Мандрагора
            int amount = rnd.Next(1, 4); // Випаде від 1 до 3 штук

            // 3. Отримуємо інформацію про предмет для гарного повідомлення
            var item = await _context.Items.FindAsync(dropItemId);
            if (item == null) return NotFound("Предмет не знайдено в базі даних");

            // 4. Шукаємо цей предмет в інвентарі
            var inventorySlot = await _context.PlayerInventories
                .FirstOrDefaultAsync(pi => pi.PlayerId == playerId && pi.ItemId == dropItemId);

            if (inventorySlot != null)
            {
                inventorySlot.Quantity += amount;
            }
            else
            {
                _context.PlayerInventories.Add(new PlayerInventory
                {
                    PlayerId = playerId,
                    ItemId = dropItemId,
                    Quantity = amount
                });
            }

            // 5. Зберігаємо всі зміни (енергію та інвентар) однією транзакцією
            await _context.SaveChangesAsync();

            // Повертаємо інформацію про лут та залишок енергії
            return Ok(new
            {
                message = $"Ви знайшли у лісі: {item.Name} (x{amount})",
                energyRemaining = player.Energy
            });
        }
    }
}