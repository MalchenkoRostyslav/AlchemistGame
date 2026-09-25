using System;
using System.Collections.Generic;

namespace AlchemistApi.Models;

public partial class PlayerQuest
{
    public int PlayerId { get; set; }

    public int QuestId { get; set; }

    public string Status { get; set; } = null!;

    public virtual Player Player { get; set; } = null!;

    public virtual Quest Quest { get; set; } = null!;
}
