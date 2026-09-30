using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlchemistApi.Models;
using AlchemistApi.Services;

namespace AlchemistApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GatherController : ControllerBase
    {
        private readonly AlchemistGameContext _context;
        private readonly PlayerService _players;

        public GatherController(AlchemistGameContext context, PlayerService players)
        {
            _context = context;
            _players = players;
        }

        // Тимчасово id предметів усе ще прописані тут; дані-кероване збирання (локації + таблиці лута) додамо разом зі сторінкою котла.
        private const int WaterItemId = 1;
        private const int MandrakeItemId = 2;

        [HttpPost]
        public async Task<IActionResult> GoToForest(int playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { message = "Гравця не знайдено" });

            if (!_players.TrySpendEnergy(player, GameRules.GatherEnergyCost))
                return BadRequest(new { message = $"Недостатньо енергії для походу в ліс! (Потрібно {GameRules.GatherEnergyCost} ⚡)" });

            // 70% вода, 30% мандрагора; кількість 1-3
            int dropItemId = Random.Shared.Next(100) < 30 ? MandrakeItemId : WaterItemId;
            int amount = Random.Shared.Next(1, 4)
                + (int)PlayerService.Effect(await _players.GetEffectsAsync(playerId), GameRules.EffGatherBonus);

            var item = await _context.Items.FindAsync(dropItemId);
            if (item == null) return NotFound(new { message = "Предмет не знайдено в базі даних" });

            var slot = await _context.PlayerInventories
                .FirstOrDefaultAsync(pi => pi.PlayerId == playerId && pi.ItemId == dropItemId);
            if (slot != null) slot.Quantity += amount;
            else _context.PlayerInventories.Add(new PlayerInventory { PlayerId = playerId, ItemId = dropItemId, Quantity = amount });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Ви знайшли у лісі: {item.Name} (x{amount})",
                item = new { itemId = item.Id, name = item.Name, icon = item.IconPath },
                amount,
                energyRemaining = player.Energy
            });
        }
    }
}
