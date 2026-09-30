using AlchemistApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AlchemistApi.Services
{
    public class LevelUpResult
    {
        public int LevelsGained { get; set; }
        public bool LeveledUp => LevelsGained > 0;
    }

    /// <summary>Логіка гравця: енергія, досвід, рівні, розблокування рецептів.</summary>
    public class PlayerService
    {
        private readonly AlchemistGameContext _context;

        public PlayerService(AlchemistGameContext context)
        {
            _context = context;
        }

        // ---------- ЕНЕРГІЯ ----------

        /// <summary>Нараховує відновлену енергію. Дробові хвилини НЕ втрачаються.</summary>
        public void RegenerateEnergy(Player player)
        {
            var now = DateTime.UtcNow;

            if (player.Energy >= player.MaxEnergy)
            {
                player.Energy = player.MaxEnergy;
                return; // якір оновиться в момент першої витрати
            }

            // Захист від некоректного/старого значення в БД
            if (player.LastEnergyUpdate > now || player.LastEnergyUpdate.Year < 2000)
            {
                player.LastEnergyUpdate = now;
                return;
            }

            int gained = (int)((now - player.LastEnergyUpdate).TotalSeconds / GameRules.EnergyRegenSeconds);
            if (gained <= 0) return;

            player.Energy = Math.Min(player.MaxEnergy, player.Energy + gained);
            player.LastEnergyUpdate = player.Energy >= player.MaxEnergy
                ? now
                : player.LastEnergyUpdate.AddSeconds((double)gained * GameRules.EnergyRegenSeconds);
        }

        /// <summary>Спершу відновлює енергію, потім списує. false, якщо не вистачає.</summary>
        public bool TrySpendEnergy(Player player, int cost)
        {
            RegenerateEnergy(player);
            if (player.Energy < cost) return false;

            // Якщо енергія була повною, таймер відновлення стартує саме зараз
            if (player.Energy >= player.MaxEnergy) player.LastEnergyUpdate = DateTime.UtcNow;

            player.Energy -= cost;
            return true;
        }

        public int SecondsToNextEnergy(Player player)
        {
            if (player.Energy >= player.MaxEnergy) return 0;
            int elapsed = (int)(DateTime.UtcNow - player.LastEnergyUpdate).TotalSeconds;
            return Math.Max(0, GameRules.EnergyRegenSeconds - elapsed);
        }

        // ---------- ДОСВІД І РІВНІ ----------

        /// <summary>Додає XP; підвищує рівень стільки разів, скільки потрібно. +1 очко навичок за рівень.</summary>
        public LevelUpResult AddExperience(Player player, int amount)
        {
            var result = new LevelUpResult();
            if (player.Level < 1) player.Level = 1;
            if (amount <= 0) return result;

            int xp = (player.Experience ?? 0) + amount;
            while (xp >= GameRules.XpForLevel(player.Level))
            {
                xp -= GameRules.XpForLevel(player.Level);
                player.Level++;
                player.SkillPoints++;
                result.LevelsGained++;
            }
            player.Experience = xp;
            return result;
        }

        // ---------- НАВИЧКИ ----------

        /// <summary>Сумарні бонуси навичок гравця за типом ефекту (значення × ранг).</summary>
        public async Task<Dictionary<string, decimal>> GetEffectsAsync(int playerId)
        {
            var rows = await _context.PlayerSkills
                .Where(ps => ps.PlayerId == playerId)
                .Select(ps => new { Type = ps.Skill.EffectType, Value = ps.Skill.EffectValue, Rank = ps.Rank })
                .ToListAsync();
            return rows.GroupBy(r => r.Type).ToDictionary(g => g.Key, g => g.Sum(r => r.Value * r.Rank));
        }

        public static decimal Effect(Dictionary<string, decimal> effects, string type) =>
            effects.TryGetValue(type, out var v) ? v : 0m;

        /// <summary>Множник часу варіння від навичок (1.0 = без змін).</summary>
        public static decimal BrewTimeFactor(Dictionary<string, decimal> effects) =>
            1m - Math.Min(0.8m, Effect(effects, GameRules.EffBrewTime) / 100m);

        // ---------- РЕЦЕПТИ ----------

        /// <summary>
        /// Відкриває гравцю всі рецепти, доступні за рівнем (без прив'язки до навички).
        /// Повертає назви щойно відкритих рецептів. SaveChanges викликає той, хто викликав метод.
        /// </summary>
        public async Task<List<string>> GrantAutoRecipesAsync(Player player)
        {
            var known = await _context.PlayerRecipes
                .Where(pr => pr.PlayerId == player.Id)
                .Select(pr => pr.RecipeId)
                .ToListAsync();

            var toGrant = await _context.Recipes
                .Include(r => r.ResultItem)
                .Where(r => r.RequiredSkillId == null && r.RequiredLevel <= player.Level && !known.Contains(r.Id))
                .ToListAsync();

            var names = new List<string>();
            foreach (var recipe in toGrant)
            {
                _context.PlayerRecipes.Add(new PlayerRecipe
                {
                    PlayerId = player.Id,
                    RecipeId = recipe.Id,
                    UnlockedAt = DateTime.UtcNow
                });
                names.Add(recipe.ResultItem?.Name ?? $"Рецепт #{recipe.Id}");
            }
            return names;
        }
    }
}
