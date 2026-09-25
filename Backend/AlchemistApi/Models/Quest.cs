using System;
using System.Collections.Generic;

namespace AlchemistApi.Models;

public partial class Quest
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int RewardGold { get; set; }

    public int RewardXp { get; set; }

    public virtual ICollection<PlayerQuest> PlayerQuests { get; set; } = new List<PlayerQuest>();

    public virtual ICollection<QuestRequirement> QuestRequirements { get; set; } = new List<QuestRequirement>();
}
