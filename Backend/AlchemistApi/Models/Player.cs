using System;
using System.Collections.Generic;

namespace AlchemistApi.Models;

public partial class Player
{
    public int Id { get; set; }

    public string Nickname { get; set; } = null!;

    public int? Gold { get; set; }

    public int? Experience { get; set; }

    public int? CauldronLevel { get; set; }
    public int Energy { get; set; } = 50;
    public int MaxEnergy { get; set; } = 50;
    public DateTime LastEnergyUpdate { get; set; } = DateTime.Now;

    public virtual CauldronUpgrade? CauldronLevelNavigation { get; set; }

    public virtual ICollection<PlayerInventory> PlayerInventories { get; set; } = new List<PlayerInventory>();

    public virtual ICollection<PlayerQuest> PlayerQuests { get; set; } = new List<PlayerQuest>();
}
