using System;
using System.Collections.Generic;

namespace AlchemistApi.Models;

public partial class Rarity
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string ColorHex { get; set; } = null!;

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
