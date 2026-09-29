using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlchemistApi.Models;
using AlchemistApi.Services;

namespace AlchemistApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestController : ControllerBase
    {
        private readonly AlchemistGameContext _context;
        private readonly PlayerService _players;

        public QuestController(AlchemistGameContext context, PlayerService players)
        {
            _context = context;
            _players = players;
        }

        private Task<Dictionary<int, int>> GetInventoryMap(int playerId) =>
            _context.PlayerInventories
                .Where(pi => pi.PlayerId == playerId)
                .ToDictionaryAsync(pi => pi.ItemId, pi => pi.Quantity);

        // Квест у форматі для фронтенду, з прогресом (є / потрібно)
        private static object BuildDto(Quest q, Dictionary<int, int> inv, string status)
        {
            var reqs = q.QuestRequirements.Select(r => new
            {
                itemId = r.ItemId,
                itemName = r.Item.Name,
                icon = r.Item.IconPath,
                quantityNeeded = r.QuantityNeeded,
                have = inv.TryGetValue(r.ItemId, out var h) ? h : 0
            }).ToList();

            return new
            {
                questId = q.Id,
                title = q.Title,
                description = q.Description,
                rewardGold = q.RewardGold,
                rewardXp = q.RewardXp,
                requiredLevel = q.RequiredLevel,
                isRepeatable = q.IsRepeatable,
                status,
                requirements = reqs,
                canTurnIn = reqs.All(r => r.have >= r.quantityNeeded)
            };
        }

        // СУМІСНІСТЬ: список активних квестів (масив)
        [HttpGet("{playerId:int}")]
        public async Task<IActionResult> GetActiveQuests(int playerId)
        {
            var inv = await GetInventoryMap(playerId);
            var quests = await _context.Quests
                .Include(q => q.QuestRequirements).ThenInclude(r => r.Item)
                .Where(q => _context.PlayerQuests.Any(pq =>
                    pq.PlayerId == playerId && pq.QuestId == q.Id && pq.Status == GameRules.QuestActive))
                .ToListAsync();

            return Ok(quests.Select(q => BuildDto(q, inv, GameRules.QuestActive)));
        }

        // Уся дошка оголошень: доступні / активні / виконані / заблоковані за рівнем
        [HttpGet("board/{playerId:int}")]
        public async Task<IActionResult> GetBoard(int playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { message = "Гравця не знайдено" });

            var inv = await GetInventoryMap(playerId);
            var mine = await _context.PlayerQuests
                .Where(pq => pq.PlayerId == playerId)
                .ToDictionaryAsync(pq => pq.QuestId, pq => pq.Status);
            var quests = await _context.Quests
                .Include(q => q.QuestRequirements).ThenInclude(r => r.Item)
                .OrderBy(q => q.RequiredLevel).ThenBy(q => q.Id)
                .ToListAsync();

            var available = new List<object>();
            var active = new List<object>();
            var completed = new List<object>();
            var locked = new List<object>();

            foreach (var q in quests)
            {
                mine.TryGetValue(q.Id, out var status);

                if (status == GameRules.QuestActive)
                {
                    active.Add(BuildDto(q, inv, GameRules.QuestActive));
                    continue;
                }

                if (status == GameRules.QuestCompleted)
                    completed.Add(BuildDto(q, inv, GameRules.QuestCompleted));

                if (q.RequiredLevel > player.Level)
                {
                    if (status == null) locked.Add(new { questId = q.Id, title = q.Title, requiredLevel = q.RequiredLevel });
                }
                else if (status == null || (status == GameRules.QuestCompleted && q.IsRepeatable))
                {
                    available.Add(BuildDto(q, inv, "Available"));
                }
            }

            return Ok(new { available, active, completed, locked });
        }

        // Взяти квест
        [HttpPost("accept")]
        public async Task<IActionResult> AcceptQuest(int playerId, int questId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { message = "Гравця не знайдено" });

            var quest = await _context.Quests.FindAsync(questId);
            if (quest == null) return NotFound(new { message = "Квест не знайдено" });

            if (player.Level < quest.RequiredLevel)
                return BadRequest(new { message = $"Потрібен {quest.RequiredLevel} рівень!" });

            var pq = await _context.PlayerQuests.FirstOrDefaultAsync(x => x.PlayerId == playerId && x.QuestId == questId);
            if (pq == null)
            {
                _context.PlayerQuests.Add(new PlayerQuest { PlayerId = playerId, QuestId = questId, Status = GameRules.QuestActive });
            }
            else if (pq.Status == GameRules.QuestActive)
            {
                return BadRequest(new { message = "Ви вже взяли цей квест." });
            }
            else if (quest.IsRepeatable)
            {
                pq.Status = GameRules.QuestActive;
            }
            else
            {
                return BadRequest(new { message = "Цей квест вже виконано." });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Квест «{quest.Title}» прийнято!" });
        }

        // Здача квесту
        [HttpPost("turnin")]
        public async Task<IActionResult> TurnInQuest(int playerId, int questId)
        {
            var playerQuest = await _context.PlayerQuests
                .Include(pq => pq.Quest).ThenInclude(q => q.QuestRequirements)
                .FirstOrDefaultAsync(pq => pq.PlayerId == playerId && pq.QuestId == questId && pq.Status == GameRules.QuestActive);
            if (playerQuest == null) return NotFound(new { message = "Активний квест не знайдено." });

            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { message = "Гравця не знайдено" });

            var quest = playerQuest.Quest;
            var inventory = await _context.PlayerInventories.Where(pi => pi.PlayerId == playerId).ToListAsync();

            // 1. Перевірка предметів
            foreach (var req in quest.QuestRequirements)
            {
                var slot = inventory.FirstOrDefault(pi => pi.ItemId == req.ItemId);
                if (slot == null || slot.Quantity < req.QuantityNeeded)
                    return BadRequest(new { message = "У вас недостатньо предметів для виконання квесту!" });
            }

            // 2. Списання
            foreach (var req in quest.QuestRequirements)
            {
                var slot = inventory.First(pi => pi.ItemId == req.ItemId);
                slot.Quantity -= req.QuantityNeeded;
                if (slot.Quantity == 0) _context.PlayerInventories.Remove(slot);
            }

            // 3. Нагорода
            player.Gold = (player.Gold ?? 0) + quest.RewardGold;
            var levelUp = _players.AddExperience(player, quest.RewardXp);
            playerQuest.Status = GameRules.QuestCompleted;

            var unlocked = levelUp.LeveledUp
                ? await _players.GrantAutoRecipesAsync(player)
                : new List<string>();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Квест виконано! Отримано {quest.RewardGold} 🪙 та {quest.RewardXp} XP.",
                rewardGold = quest.RewardGold,
                rewardXp = quest.RewardXp,
                newBalance = player.Gold,
                level = player.Level,
                xp = player.Experience ?? 0,
                xpToNext = GameRules.XpForLevel(player.Level),
                leveledUp = levelUp.LeveledUp,
                skillPoints = player.SkillPoints,
                unlockedRecipes = unlocked
            });
        }
    }
}