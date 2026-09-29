using System;
using System.Collections.Generic;

namespace AlchemistApi.Models;

public partial class Recipe
{
    public int Id { get; set; }

    public int? ResultItemId { get; set; }

    public int? RequiredXp { get; set; }

    /// <summary>Рецепт відкривається автоматично на цьому рівні гравця (якщо RequiredSkillId == null).</summary>
    public int RequiredLevel { get; set; } = 1;

    /// <summary>Якщо задано, рецепт відкривається лише вивченням навички (етап "Прокачка").</summary>
    public int? RequiredSkillId { get; set; }

    public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

    public virtual ICollection<PlayerRecipe> PlayerRecipes { get; set; } = new List<PlayerRecipe>();

    public virtual Item? ResultItem { get; set; }
}