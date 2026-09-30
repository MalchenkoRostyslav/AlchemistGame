using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlchemistApi.Models;
using AlchemistApi.Services;

namespace AlchemistApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopController : ControllerBase
    {
        private readonly AlchemistGameContext _context;
        private readonly PlayerService _players;

        public ShopController(AlchemistGameContext context, PlayerService players)
        {
            _context = context;
            _players = players;
        }

        // Знижка на купівлю (навичка), максимум 50%
        private static decimal DiscountPercent(Dictionary<string, decimal> effects) =>
            Math.Min(50m, PlayerService.Effect(effects, GameRules.EffBuyDiscount));

        private static int Discounted(int price, decimal pct) =>
            Math.Max(1, (int)Math.Round(price * (1m - pct / 100m)));

        // Асортимент магазину з таблиці ShopAssortment (з урахуванням знижки)
        [HttpGet("assortment/{playerId:int}")]
        public async Task<IActionResult> GetAssortment(int playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { message = "Гравця не знайдено" });

            decimal disc = DiscountPercent(await _players.GetEffectsAsync(playerId));

            var raw = await _context.ShopAssortments
                .Where(a => a.ItemId != null && a.Item != null)
                .OrderBy(a => a.RequiredPlayerLevel).ThenBy(a => a.PurchasePrice)
                .Select(a => new
                {
                    ItemId = a.ItemId,
                    Name = a.Item!.Name,
                    Type = a.Item.ItemType,
                    Icon = a.Item.IconPath,
                    Rarity = a.Item.Rarity!.Name,
                    Color = a.Item.Rarity!.ColorHex,
                    Price = a.PurchasePrice,
                    RequiredLevel = a.RequiredPlayerLevel ?? 0
                })
                .ToListAsync();

            var items = raw.Select(x => new
            {
                itemId = x.ItemId,
                name = x.Name,
                type = x.Type,
                icon = x.Icon,
                rarity = x.Rarity,
                color = x.Color,
                price = Discounted(x.Price, disc),
                basePrice = x.Price,
                requiredLevel = x.RequiredLevel,
                locked = player.Level < x.RequiredLevel
            });

            return Ok(items);
        }

        // Купівля: ціна з ShopAssortment мінус знижка від навичок
        [HttpPost("buy")]
        public async Task<IActionResult> BuyItem(int playerId, int itemId, int quantity = 1)
        {
            if (quantity < 1 || quantity > GameRules.MaxTradeQuantity)
                return BadRequest(new { message = $"Кількість має бути від 1 до {GameRules.MaxTradeQuantity}." });

            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { message = "Гравця не знайдено" });

            var offer = await _context.ShopAssortments
                .Include(a => a.Item)
                .FirstOrDefaultAsync(a => a.ItemId == itemId);
            if (offer == null || offer.Item == null)
                return NotFound(new { message = "Торговець не продає цей предмет" });

            if (player.Level < (offer.RequiredPlayerLevel ?? 0))
                return BadRequest(new { message = $"Потрібен {offer.RequiredPlayerLevel} рівень!" });

            decimal disc = DiscountPercent(await _players.GetEffectsAsync(playerId));
            long total = (long)Discounted(offer.PurchasePrice, disc) * quantity;
            if ((player.Gold ?? 0) < total)
                return BadRequest(new { message = "Недостатньо золота!" });

            player.Gold = (player.Gold ?? 0) - (int)total;

            var slot = await _context.PlayerInventories
                .FirstOrDefaultAsync(pi => pi.PlayerId == playerId && pi.ItemId == itemId);
            if (slot != null) slot.Quantity += quantity;
            else _context.PlayerInventories.Add(new PlayerInventory { PlayerId = playerId, ItemId = itemId, Quantity = quantity });

            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = $"Куплено {offer.Item.Name} ×{quantity} за {total} 🪙",
                spent = total,
                newBalance = player.Gold
            });
        }

        // Продаж: тільки зілля / отрута / невідоме зілля; ціна з бонусом навичок
        [HttpPost("sell")]
        public async Task<IActionResult> SellItem(int playerId, int itemId, int quantity = 1)
        {
            if (quantity < 1 || quantity > GameRules.MaxTradeQuantity)
                return BadRequest(new { message = $"Кількість має бути від 1 до {GameRules.MaxTradeQuantity}." });

            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { message = "Гравця не знайдено" });

            var slot = await _context.PlayerInventories
                .Include(pi => pi.Item)
                .FirstOrDefaultAsync(pi => pi.PlayerId == playerId && pi.ItemId == itemId);

            if (slot == null || slot.Quantity <= 0) return BadRequest(new { message = "У вас немає цього предмета" });
            if (!GameRules.IsSellable(slot.Item.ItemType)) return BadRequest(new { message = "Цей предмет не можна продати" });
            if (quantity > slot.Quantity) return BadRequest(new { message = "У вас немає стільки предметів" });

            var effects = await _players.GetEffectsAsync(playerId);
            decimal bonus = 1m + PlayerService.Effect(effects, GameRules.EffSellBonus) / 100m;
            int unit = (int)Math.Round(slot.Item.BasePrice * bonus);
            int earned = unit * quantity;

            player.Gold = (player.Gold ?? 0) + earned;
            slot.Quantity -= quantity;
            if (slot.Quantity == 0) _context.PlayerInventories.Remove(slot);

            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = $"Продано {slot.Item.Name} ×{quantity} за {earned} 🪙",
                earned,
                newBalance = player.Gold
            });
        }
    }
}
