using System;
using System.Collections.Generic;

namespace AlchemistApi.Models;

public partial class RecipeIngredient
{
    public int RecipeId { get; set; }

    public int IngredientItemId { get; set; }

    public int QuantityNeeded { get; set; }

    public virtual Item IngredientItem { get; set; } = null!;

    public virtual Recipe Recipe { get; set; } = null!;
}
