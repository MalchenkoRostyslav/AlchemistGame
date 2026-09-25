using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlchemistApi.Models;
using System;

namespace AlchemistApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayerController : ControllerBase
    {
        private readonly AlchemistGameContext _context;

        public PlayerController(AlchemistGameContext context)
        {
            _context = context;
        }

        // 1. Отримання інформації про гравця та регенерація енергії
        [HttpGet("{playerId}")]
        public async Task<IActionResult> GetPlayerInfo(int playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound("Гравця не знайдено");

            // Захист від помилок бази даних (якщо енергія ще не була задана)
            if (player.LastEnergyUpdate == DateTime.MinValue || player.LastEnergyUpdate < new DateTime(2000, 1, 1))
            {
                player.LastEnergyUpdate = DateTime.Now;
                player.Energy = 50;
                player.MaxEnergy = 50;
            }

            // Відновлення енергії (1 одиниця за хвилину)
            var timePassed = DateTime.Now - player.LastEnergyUpdate;
            int energyToAdd = (int)timePassed.TotalMinutes;

            if (energyToAdd > 0 && player.Energy < player.MaxEnergy)
            {
                player.Energy = Math.Min(player.MaxEnergy, player.Energy + energyToAdd);
                player.LastEnergyUpdate = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            // Повертаємо дані (використовуємо ?? 0 для захисту від null)
            return Ok(new
            {
                name = player.Nickname,
                gold = player.Gold ?? 0,
                level = player.CauldronLevel ?? 1,
                xp = player.Experience ?? 0,
                energy = player.Energy,
                maxEnergy = player.MaxEnergy
            });
        }

        // 2. Список всіх профілів для Головного меню
        [HttpGet("profiles")]
        public async Task<IActionResult> GetAllProfiles()
        {
            var profiles = await _context.Players
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Nickname,
                    level = p.CauldronLevel ?? 1
                })
                .ToListAsync();
            return Ok(profiles);
        }

        // 3. Створення нового профілю (ЦЕ ПОЛАГОДИТЬ КНОПКУ "СТВОРИТИ ПРОФІЛЬ")
        [HttpPost("new")]
        public async Task<IActionResult> CreateProfile([FromQuery] string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname)) return BadRequest("Ім'я не може бути порожнім!");

            var player = new Player
            {
                Nickname = nickname,
                Gold = 100,             // Стартове золото
                Experience = 0,
                CauldronLevel = 1,
                Energy = 50,            // Стартова енергія
                MaxEnergy = 50,
                LastEnergyUpdate = DateTime.Now
            };

            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            return Ok(new { id = player.Id, name = player.Nickname });
        }

        // 4. Видалення профілю
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteProfile(int id)
        {
            var player = await _context.Players
                .Include(p => p.PlayerInventories)
                .Include(p => p.PlayerQuests)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (player == null) return NotFound("Профіль не знайдено");

            _context.PlayerInventories.RemoveRange(player.PlayerInventories);
            _context.PlayerQuests.RemoveRange(player.PlayerQuests);
            _context.Players.Remove(player);

            await _context.SaveChangesAsync();
            return Ok("Профіль успішно видалено");
        }

        // 5. Покращення казанка
        [HttpPost("upgrade-cauldron/{playerId}")]
        public async Task<IActionResult> UpgradeCauldron(int playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound("Гравця не знайдено");

            int currentLevel = player.CauldronLevel ?? 1;

            var nextUpgrade = await _context.CauldronUpgrades.FirstOrDefaultAsync(u => u.Level == currentLevel + 1);

            if (nextUpgrade == null) return BadRequest("Ваш казанок вже максимального рівня!");
            if (player.Gold < nextUpgrade.UpgradeCostGold) return BadRequest($"Недостатньо золота! Потрібно {nextUpgrade.UpgradeCostGold} 🪙");

            player.Gold -= nextUpgrade.UpgradeCostGold;
            player.CauldronLevel = currentLevel + 1;
            await _context.SaveChangesAsync();

            return Ok($"Казанок покращено до {currentLevel + 1} рівня!");
        }
    }
}