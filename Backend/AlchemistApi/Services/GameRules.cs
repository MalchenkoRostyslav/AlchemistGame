namespace AlchemistApi.Services
{
    /// <summary>Усі константи й формули гри в одному місці.</summary>
    public static class GameRules
    {
        // Енергія
        public const int EnergyRegenSeconds = 60;   // 1 енергія за хвилину
        public const int BrewEnergyCost = 10;
        public const int GatherEnergyCost = 5;

        // Досвід за варіння
        public const int BrewSuccessXp = 35;
        public const int BrewFailXp = 5;
        public const int FailBrewSeconds = 20;   // час варіння невдалої суміші (фіксований, щоб не видавати результат)

        /// <summary>Позначає DateTime з БД як UTC, щоб у JSON він мав суфікс Z.</summary>
        public static DateTime AsUtc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

        // Ліміти
        public const int MaxQuantityPerIngredient = 99;
        public const int MaxTradeQuantity = 999;

        // Типи предметів (Items.ItemType)
        public const string TypeIngredient = "Ingredient";
        public const string TypePotion = "Potion";
        public const string TypePoison = "Poison";
        public const string TypeUnknown = "UnknownPotion";

        // Статуси квестів (PlayerQuests.Status)
        public const string QuestActive = "Active";
        public const string QuestCompleted = "Completed";

        // Типи ефектів навичок (Skills.EffectType)
        public const string EffMaxEnergy = "MaxEnergy";       // +X макс. енергії за ранг (застосовується при вивченні)
        public const string EffBrewTime = "BrewTime";         // -X% часу варіння за ранг (максимум -80%)
        public const string EffSellBonus = "SellBonus";       // +X% до ціни продажу за ранг
        public const string EffBuyDiscount = "BuyDiscount";   // -X% до ціни купівлі за ранг (максимум -50%)
        public const string EffGatherBonus = "GatherBonus";   // +X предметів за похід у ліс за ранг
        public const string EffUnlockRecipe = "UnlockRecipe"; // відкриває рецепти з RequiredSkillId

        /// <summary>Скільки XP потрібно, щоб піднятися з рівня level на level+1.</summary>
        public static int XpForLevel(int level) => Math.Max(1, level) * 100;

        /// <summary>Що можна продавати торговцю.</summary>
        public static bool IsSellable(string? itemType) =>
            itemType is TypePotion or TypePoison or TypeUnknown;
    }
}
