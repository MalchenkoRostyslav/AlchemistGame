using System;
using System.Collections.Generic;

namespace AlchemistApi.Models;

public partial class PlayerInventory
{
    public int PlayerId { get; set; }

    public int ItemId { get; set; }

    public int Quantity { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual Player Player { get; set; } = null!;
}
