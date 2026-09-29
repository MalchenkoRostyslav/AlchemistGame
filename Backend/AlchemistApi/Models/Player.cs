using System;
using System.Collections.Generic;

namespace AlchemistApi.Models;

public partial class Player
{
    public int Id { get; set; }

    public string Nickname { get; set; } = null!;

    public int? Gold { get; set; }

    /// <summary>XP у межах поточного рівня (після левелапу залишок переноситься).</summary>
    public int? Experience { get; set; }

    /// <summary>Рівень КАЗАНА (купується за золото). FK на CauldronUpgrades.Level.</summary>
    public int? CauldronLevel { get; set; }

    /// <summary>Рівень ГРАВЦЯ (росте від XP).</summary>
    public int Level { get; set; } = 1;

    public int SkillPoints { get; set; }

    public int Energy { get; set; } = 50;

    public int MaxEnergy { get; set; } = 50;

    /// <summary>Якір відліку відновлення енергії (UTC).</summary>
    public DateTime LastEnergyUpdate { get; set; } = DateTime.UtcNow;

    public virtual CauldronUpgrade? CauldronLevelNavigation { get; set; }

    public virtual ICollection<PlayerInventory> PlayerInventories { get; set; } = new List<PlayerInventory>();

    public virtual ICollection<PlayerQuest> PlayerQuests { get; set; } = new List<PlayerQuest>();

    public virtual ICollection<PlayerRecipe> PlayerRecipes { get; set; } = new List<PlayerRecipe>();
}