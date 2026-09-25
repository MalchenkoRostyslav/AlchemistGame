using System;
using System.Collections.Generic;

namespace AlchemistApi.Models;

public partial class QuestRequirement
{
    public int QuestId { get; set; }

    public int ItemId { get; set; }

    public int QuantityNeeded { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual Quest Quest { get; set; } = null!;
}
