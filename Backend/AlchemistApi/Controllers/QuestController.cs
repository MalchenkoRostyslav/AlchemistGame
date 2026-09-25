using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlchemistApi.Models;

namespace AlchemistApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestController : ControllerBase
    {
        private readonly AlchemistGameContext _context;

        public QuestController(AlchemistGameContext context)
        {
            _context = context;
        }

        // Отримання списку активних квестів гравця
        [HttpGet("{playerId}")]
        public async Task<IActionResult> GetActiveQuests(int playerId)
        {
            var quests = await _context.PlayerQuests
                .Include(pq => pq.Quest)
                    .ThenInclude(q => q.QuestRequirements)
                        .ThenInclude(qr => qr.Item)
                .Where(pq => pq.PlayerId == playerId && pq.Status == "Active")
                .Select(pq => new
                {
                    QuestId = pq.QuestId,
                    Title = pq.Quest!.Title,
                    Description = pq.Quest.Description,
                    RewardGold = pq.Quest.RewardGold,
                    RewardXp = pq.Quest.RewardXp,
                    Requirements = pq.Quest.QuestRequirements.Select(qr => new
                    {
                        ItemId = qr.ItemId,
                        ItemName = qr.Item!.Name,
                        QuantityNeeded = qr.QuantityNeeded
                    })
                })
                .ToListAsync();

            return Ok(quests);
        }

        // Здача виконаного квесту
        [HttpPost("turnin")]
        public async Task<IActionResult> TurnInQuest(int playerId, int questId)
        {
            var playerQuest = await _context.PlayerQuests
                .Include(pq => pq.Quest)
                    .ThenInclude(q => q.QuestRequirements)
                .FirstOrDefaultAsync(pq => pq.PlayerId == playerId && pq.QuestId == questId && pq.Status == "Active");

            if (playerQuest == null) return NotFound("Квест не знайдено або вже виконано.");

            var inventory = await _context.PlayerInventories.Where(pi => pi.PlayerId == playerId).ToListAsync();
            var player = await _context.Players.FindAsync(playerId);

            // 1. Перевіряємо, чи є в інвентарі всі потрібні предмети
            foreach (var req in playerQuest.Quest!.QuestRequirements)
            {
                var item = inventory.FirstOrDefault(pi => pi.ItemId == req.ItemId);
                if (item == null || item.Quantity < req.QuantityNeeded)
                {
                    return BadRequest("У вас недостатньо предметів для виконання квесту!");
                }
            }

            // 2. Віднімаємо предмети
            foreach (var req in playerQuest.Quest.QuestRequirements)
            {
                var item = inventory.First(pi => pi.ItemId == req.ItemId);
                item.Quantity -= req.QuantityNeeded;
                if (item.Quantity == 0) _context.PlayerInventories.Remove(item);
            }

            // 3. Видаємо нагороду і змінюємо статус квесту
            player!.Gold += playerQuest.Quest.RewardGold;
            player.Experience = (player.Experience ?? 0) + playerQuest.Quest.RewardXp;
            playerQuest.Status = "Completed";

            // Перевірка на підвищення рівня
            int currentLevel = player.CauldronLevel ?? 1;
            int xpRequired = currentLevel * 100;
            if (player.Experience >= xpRequired)
            {
                player.Experience -= xpRequired;
                player.CauldronLevel = currentLevel + 1;
            }

            await _context.SaveChangesAsync();
            return Ok($"Квест виконано! Отримано {playerQuest.Quest.RewardGold} 🪙 та {playerQuest.Quest.RewardXp} XP.");
        }
    }
}