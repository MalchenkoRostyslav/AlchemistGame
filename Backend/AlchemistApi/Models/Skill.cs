namespace AlchemistApi.Models;

public partial class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? IconPath { get; set; }
    public string Branch { get; set; } = null!;
    public int Tier { get; set; } = 1;
    public int Cost { get; set; } = 1;
    public int MaxRank { get; set; } = 1;
    public int? PrerequisiteSkillId { get; set; }
    public string EffectType { get; set; } = null!;
    public decimal EffectValue { get; set; }
}
