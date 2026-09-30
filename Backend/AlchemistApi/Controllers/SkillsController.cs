using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlchemistApi.Models;
using AlchemistApi.Services;

namespace AlchemistApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SkillsController : ControllerBase
    {
        private readonly AlchemistGameContext _context;
        private readonly PlayerService _players;

        public SkillsController(AlchemistGameContext context, PlayerService players)
        {
            _context = context;
            _players = players;
        }

        // Дерево навичок гравця
        [HttpGet("{playerId:int}")]
        public async Task<IActionResult> GetSkills(int playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { message = "Гравця не знайдено" });

            var skills = await _context.Skills.OrderBy(s => s.Branch).ThenBy(s => s.Tier).ThenBy(s => s.Id).ToListAsync();
            var ranks = await _context.PlayerSkills
                .Where(ps => ps.PlayerId == playerId)
                .ToDictionaryAsync(ps => ps.SkillId, ps => ps.Rank);
            var unlocks = await _context.Recipes
                .Where(r => r.RequiredSkillId != null)
                .Select(r => new { SkillId = r.RequiredSkillId, Name = r.ResultItem!.Name })
                .ToListAsync();
            var effects = await _players.GetEffectsAsync(playerId);
            var byId = skills.ToDictionary(s => s.Id);

            var list = skills.Select(s =>
            {
                int rank = ranks.TryGetValue(s.Id, out var rk) ? rk : 0;
                bool locked = s.PrerequisiteSkillId != null &&
                    (!ranks.TryGetValue(s.PrerequisiteSkillId.Value, out var pr) || pr < 1);
                bool maxed = rank >= s.MaxRank;
                string? prereqName = s.PrerequisiteSkillId != null && byId.TryGetValue(s.PrerequisiteSkillId.Value, out var p)
                    ? p.Name : null;

                return new
                {
                    id = s.Id,
                    name = s.Name,
                    description = s.Description,
                    icon = s.IconPath,
                    branch = s.Branch,
                    tier = s.Tier,
                    cost = s.Cost,
                    maxRank = s.MaxRank,
                    rank,
                    effectType = s.EffectType,
                    effectValue = s.EffectValue,
                    status = maxed ? "maxed" : (locked ? "locked" : "available"),
                    canLearn = !maxed && !locked && player.SkillPoints >= s.Cost,
                    prerequisiteName = prereqName,
                    unlocks = unlocks.Where(u => u.SkillId == s.Id).Select(u => u.Name).ToList()
                };
            }).ToList();

            return Ok(new
            {
                skillPoints = player.SkillPoints,
                effects = effects.Select(kv => new { type = kv.Key, value = kv.Value }).ToList(),
                skills = list
            });
        }

        // Вивчити / покращити навичку
        [HttpPost("learn")]
        public async Task<IActionResult> Learn(int playerId, int skillId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { message = "Гравця не знайдено" });

            var skill = await _context.Skills.FindAsync(skillId);
            if (skill == null) return NotFound(new { message = "Навичку не знайдено" });

            var owned = await _context.PlayerSkills.FirstOrDefaultAsync(x => x.PlayerId == playerId && x.SkillId == skillId);
            int rank = owned?.Rank ?? 0;

            if (rank >= skill.MaxRank) return BadRequest(new { message = "Навичка вже на максимальному рівні." });

            if (skill.PrerequisiteSkillId != null)
            {
                var pre = await _context.PlayerSkills.FirstOrDefaultAsync(x => x.PlayerId == playerId && x.SkillId == skill.PrerequisiteSkillId);
                if (pre == null || pre.Rank < 1)
                    return BadRequest(new { message = "Спершу вивчіть попередню навичку." });
            }

            if (player.SkillPoints < skill.Cost)
                return BadRequest(new { message = $"Недостатньо очок навичок (потрібно {skill.Cost} ✦)." });

            player.SkillPoints -= skill.Cost;
            if (owned == null) _context.PlayerSkills.Add(new PlayerSkill { PlayerId = playerId, SkillId = skillId, Rank = 1 });
            else owned.Rank += 1;

            // Максимальна енергія змінюється одразу
            if (skill.EffectType == GameRules.EffMaxEnergy)
            {
                _players.RegenerateEnergy(player);
                int add = (int)skill.EffectValue;
                player.MaxEnergy += add;
                player.Energy += add;
            }

            // Перше вивчення відкриває рецепти, прив'язані до навички
            var unlocked = new List<string>();
            if (rank == 0)
            {
                var recipes = await _context.Recipes.Include(r => r.ResultItem)
                    .Where(r => r.RequiredSkillId == skillId).ToListAsync();
                var known = await _context.PlayerRecipes.Where(pr => pr.PlayerId == playerId)
                    .Select(pr => pr.RecipeId).ToListAsync();
                foreach (var recipe in recipes.Where(r => !known.Contains(r.Id)))
                {
                    _context.PlayerRecipes.Add(new PlayerRecipe { PlayerId = playerId, RecipeId = recipe.Id, UnlockedAt = DateTime.UtcNow });
                    unlocked.Add(recipe.ResultItem?.Name ?? $"Рецепт #{recipe.Id}");
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = $"Вивчено: {skill.Name} (ранг {rank + 1}/{skill.MaxRank})",
                rank = rank + 1,
                skillPoints = player.SkillPoints,
                maxEnergy = player.MaxEnergy,
                unlockedRecipes = unlocked
            });
        }
    }
}
