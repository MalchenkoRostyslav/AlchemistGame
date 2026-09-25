using System;
using System.Collections.Generic;

namespace AlchemistApi.Models;

public partial class ShopAssortment
{
    public int Id { get; set; }

    public int? ItemId { get; set; }

    public int PurchasePrice { get; set; }

    public int? RequiredPlayerLevel { get; set; }

    public virtual Item? Item { get; set; }
}
