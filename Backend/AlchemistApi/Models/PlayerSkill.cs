namespace AlchemistApi.Models;

public partial class PlayerSkill
{
    public int PlayerId { get; set; }
    public int SkillId { get; set; }
    public int Rank { get; set; } = 1;

    public virtual Player Player { get; set; } = null!;
    public virtual Skill Skill { get; set; } = null!;
}
