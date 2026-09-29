using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlchemistApi.Models;
using AlchemistApi.Services;

namespace AlchemistApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayerController : ControllerBase
    {
        private readonly AlchemistGameContext _context;
        private readonly PlayerService _players;

        public PlayerController(AlchemistGameContext context, PlayerService players)
        {
            _context = context;
            _players = players;
        }

        // Інформація про казан гравця (поточний і наступний рівні)
        private async Task<object> BuildCauldronInfo(Player player)
        {
            int cl = player.CauldronLevel ?? 1;
            var rows = await _context.CauldronUpgrades
                .Where(u => u.Level == cl || u.Level == cl + 1)
                .ToListAsync();
            var current = rows.FirstOrDefault(u => u.Level == cl);
            var next = rows.FirstOrDefault(u => u.Level == cl + 1);

            return new
            {
                level = cl,
                maxIngredients = current?.MaxIngredients ?? 2,
                craftTimeModifier = current?.CraftTimeModifier ?? 1.0m,
                nextUpgradeCost = next?.UpgradeCostGold,
                nextMaxIngredients = next?.MaxIngredients
            };
        }

        // 1. Інформація про гравця (з регенерацією енергії)
        [HttpGet("{playerId:int}")]
        public async Task<IActionResult> GetPlayerInfo(int playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { message = "Гравця не знайдено" });

            _players.RegenerateEnergy(player);
            var cauldron = await BuildCauldronInfo(player);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = player.Id,
                name = player.Nickname,
                gold = player.Gold ?? 0,
                level = player.Level,
                xp = player.Experience ?? 0,
                xpToNext = GameRules.XpForLevel(player.Level),
                skillPoints = player.SkillPoints,
                energy = player.Energy,
                maxEnergy = player.MaxEnergy,
                energyRegenSeconds = GameRules.EnergyRegenSeconds,
                secondsToNextEnergy = _players.SecondsToNextEnergy(player),
                cauldron
            });
        }

        // 2. Список профілів для головного меню
        [HttpGet("profiles")]
        public async Task<IActionResult> GetAllProfiles()
        {
            var profiles = await _context.Players
                .OrderBy(p => p.Id)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Nickname,
                    level = p.Level
                })
                .ToListAsync();
            return Ok(profiles);
        }

        // 3. Створення нового профілю
        [HttpPost("new")]
        public async Task<IActionResult> CreateProfile([FromQuery] string? nickname = null)
        {
            nickname = nickname?.Trim() ?? "";
            if (nickname.Length == 0) return BadRequest(new { message = "Ім'я не може бути порожнім!" });
            if (nickname.Length > 50) return BadRequest(new { message = "Ім'я занадто довге (макс. 50 символів)." });

            if (await _context.Players.AnyAsync(p => p.Nickname == nickname))
                return Conflict(new { message = "Профіль з таким ім'ям уже існує." });

            var player = new Player
            {
                Nickname = nickname,
                Gold = 100,
                Experience = 0,
                Level = 1,
                SkillPoints = 0,
                CauldronLevel = 1,
                Energy = 50,
                MaxEnergy = 50,
                LastEnergyUpdate = DateTime.UtcNow
            };

            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            // Стартові рецепти
            await _players.GrantAutoRecipesAsync(player);
            await _context.SaveChangesAsync();

            return Ok(new { id = player.Id, name = player.Nickname });
        }

        // 4. Видалення профілю
        [HttpDelete("delete/{id:int}")]
        public async Task<IActionResult> DeleteProfile(int id)
        {
            var player = await _context.Players
                .Include(p => p.PlayerInventories)
                .Include(p => p.PlayerQuests)
                .Include(p => p.PlayerRecipes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (player == null) return NotFound(new { message = "Профіль не знайдено" });

            _context.PlayerInventories.RemoveRange(player.PlayerInventories);
            _context.PlayerQuests.RemoveRange(player.PlayerQuests);
            _context.PlayerRecipes.RemoveRange(player.PlayerRecipes);
            _context.Players.Remove(player);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Профіль успішно видалено" });
        }

        // 5. Покращення казана (за золото)
        [HttpPost("upgrade-cauldron/{playerId:int}")]
        public async Task<IActionResult> UpgradeCauldron(int playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { message = "Гравця не знайдено" });

            int currentLevel = player.CauldronLevel ?? 1;
            var nextUpgrade = await _context.CauldronUpgrades.FirstOrDefaultAsync(u => u.Level == currentLevel + 1);

            if (nextUpgrade == null) return BadRequest(new { message = "Ваш казан вже максимального рівня!" });
            if ((player.Gold ?? 0) < nextUpgrade.UpgradeCostGold)
                return BadRequest(new { message = $"Недостатньо золота! Потрібно {nextUpgrade.UpgradeCostGold} 🪙" });

            player.Gold = (player.Gold ?? 0) - nextUpgrade.UpgradeCostGold;
            player.CauldronLevel = currentLevel + 1;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Казан покращено до {currentLevel + 1} рівня!",
                cauldronLevel = currentLevel + 1,
                maxIngredients = nextUpgrade.MaxIngredients,
                craftTimeModifier = nextUpgrade.CraftTimeModifier,
                newBalance = player.Gold
            });
        }
    }
}