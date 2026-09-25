using System;
using System.Collections.Generic;

namespace AlchemistApi.Models;

public partial class Recipe
{
    public int Id { get; set; }

    public int? ResultItemId { get; set; }

    public int? RequiredXp { get; set; }
    public int RequiredLevel { get; set; } = 1; 

    public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

    public virtual Item? ResultItem { get; set; }
}
