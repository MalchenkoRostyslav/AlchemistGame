using System;

namespace AlchemistApi.Models;

/// <summary>Рецепт, відкритий гравцем (запис у його книзі рецептів).</summary>
public partial class PlayerRecipe
{
    public int PlayerId { get; set; }

    public int RecipeId { get; set; }

    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

    public virtual Player Player { get; set; } = null!;

    public virtual Recipe Recipe { get; set; } = null!;
}