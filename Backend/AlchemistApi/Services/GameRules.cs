using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AlchemistApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlchemistApi.Services;

public static class AuthConstants
{
    public const string Issuer = "AlchemistApi";
    public const string Audience = "AlchemistGame";
}

public static class GameRules
{
    // --- ЛОГІКА АВТОРИЗАЦІЇ ТА ГЕЙМПЛЕЮ (З ВАШОГО КОДУ) ---

    // Id гравця з JWT-токена
    public static int GetPlayerId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirst("pid")?.Value, out var id) ? id : 0;

    // Регенерація енергії: 1 од. за хвилину. Викликати перед будь-якою перевіркою енергії.
    public static void RegenEnergy(Player p)
    {
        var now = DateTime.UtcNow;
        if (p.LastEnergyUpdate.Year < 2000) p.LastEnergyUpdate = now;
        if (p.Energy >= p.MaxEnergy)
        {
            p.Energy = p.MaxEnergy;
            p.LastEnergyUpdate = now;
            return;
        }
        int add = (int)(now - p.LastEnergyUpdate).TotalMinutes;
        if (add <= 0) return;
        p.Energy = Math.Min(p.MaxEnergy, p.Energy + add);
        p.LastEnergyUpdate = p.Energy >= p.MaxEnergy ? now : p.LastEnergyUpdate.AddMinutes(add);
    }

    // Додає XP; рівень не перевищує максимальний рядок у CauldronUpgrades
    public static async Task<bool> AddXpAsync(AlchemistGameContext ctx, Player p, int xp)
    {
        int maxLevel = await ctx.CauldronUpgrades.MaxAsync(u => u.Level);
        int level = p.CauldronLevel ?? 1;
        int exp = (p.Experience ?? 0) + xp;
        bool leveledUp = false;
        while (level < maxLevel && exp >= level * 100)
        {
            exp -= level * 100;
            level++;
            leveledUp = true;
        }
        p.Experience = exp;
        p.CauldronLevel = level;
        return leveledUp;
    }



    public const int GatherEnergyCost = 5;
    public const int BrewEnergyCost = 10;

    public const string TypeIngredient = "Ingredient";
    public const string TypeUnknown = "Unknown";
    public const string TypePoison = "Poison";

    public const string QuestActive = "Active";
    public const string QuestCompleted = "Completed";

    public const int MaxQuantityPerIngredient = 5;
    public const int FailBrewSeconds = 5;
    public const int MaxTradeQuantity = 100;
    public const int BrewSuccessXp = 50;
    public const int BrewFailXp = 10;

    public const int EnergyRegenSeconds = 2;

    // Метод AsUtc
    public static DateTime AsUtc(DateTime dateTime) => dateTime.ToUniversalTime();

    // Методи розрахунку досвіду та енергії, які контролери викликають з параметром (level)
    public static int XpForLevel(int level) => level * 100;


    public const string EffGatherBonus = "GatherBonus";
    public const string EffSellBonus = "SellBonus";
    public const string EffBuyDiscount = "BuyDiscount";
    public const string EffBrewTime = "BrewTime";
    public const string EffMaxEnergy = "MaxEnergy";

    // У контролері (ShopController) IsSellable приймає рядок (x.Type), тому аргумент має бути string
    public static bool IsSellable(string itemType) => itemType != null && itemType != TypeUnknown;
}

// Захист адмін-ендпоїнтів: заголовок X-Admin-Key має збігатися зі змінною Admin__Key
public class AdminKeyAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var expected = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Admin:Key"] ?? "";
        var given = context.HttpContext.Request.Headers["X-Admin-Key"].ToString();
        bool ok = expected.Length > 0 && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(expected));
        if (!ok)
        {
            context.Result = new UnauthorizedObjectResult("Невірний адмін-ключ");
            return;
        }
        await next();
    }
}