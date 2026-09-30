namespace AlchemistApi.Models;

/// <summary>Активне варіння гравця. Результат визначається на старті, але показується лише після завершення.</summary>
public partial class PlayerBrew
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public string Outcome { get; set; } = null!;   // Success | Unknown | Poison
    public int ResultItemId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime ReadyAt { get; set; }

    public virtual Player Player { get; set; } = null!;
    public virtual Item ResultItem { get; set; } = null!;
}
