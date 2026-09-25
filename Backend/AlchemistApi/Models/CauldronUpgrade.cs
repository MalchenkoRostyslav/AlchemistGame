using System;
using System.Collections.Generic;

namespace AlchemistApi.Models;

public partial class CauldronUpgrade
{
    public int Level { get; set; }

    public int UpgradeCostGold { get; set; }

    public int MaxIngredients { get; set; }

    public decimal? CraftTimeModifier { get; set; }

    public virtual ICollection<Player> Players { get; set; } = new List<Player>();
}
