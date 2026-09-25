using System;
using System.Collections.Generic;

namespace AlchemistApi.Models;

public partial class Item
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string ItemType { get; set; } = null!;

    public int? RarityId { get; set; }

    public int BasePrice { get; set; }

    public string? IconPath { get; set; }

    public virtual ICollection<PlayerInventory> PlayerInventories { get; set; } = new List<PlayerInventory>();

    public virtual ICollection<QuestRequirement> QuestRequirements { get; set; } = new List<QuestRequirement>();

    public virtual Rarity? Rarity { get; set; }

    public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

    public virtual ICollection<ShopAssortment> ShopAssortments { get; set; } = new List<ShopAssortment>();
}
