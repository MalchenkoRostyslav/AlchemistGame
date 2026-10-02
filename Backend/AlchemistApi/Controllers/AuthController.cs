using System.Security.Claims;
using System.Text;
using AlchemistApi.Services;
using AlchemistApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AlchemistApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AlchemistGameContext _context;
        private readonly IConfiguration _config;

        public AuthController(AlchemistGameContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public class AuthDto
        {
            public string Nickname { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthDto dto)
        {
            var nick = (dto.Nickname ?? "").Trim();
            var password = dto.Password ?? "";

            if (nick.Length < 3 || nick.Length > 20) return BadRequest("Ім'я має містити від 3 до 20 символів");
            if (password.Length < 6 || password.Length > 64) return BadRequest("Пароль має містити від 6 до 64 символів");

            var lower = nick.ToLowerInvariant();
            if (await _context.Players.AnyAsync(p => p.Nickname.ToLower() == lower))
                return Conflict("Це ім'я вже зайняте");

            var player = new Player
            {
                Nickname = nick,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Gold = 100,
                Experience = 0,
                CauldronLevel = 1,
                Energy = 50,
                MaxEnergy = 50,
                LastEnergyUpdate = DateTime.UtcNow
            };

            try
            {
                _context.Players.Add(player);
                await _context.SaveChangesAsync();

                // Видаємо новому гравцю всі існуючі квести
                var questIds = await _context.Quests.Select(q => q.Id).ToListAsync();
                _context.PlayerQuests.AddRange(questIds.Select(id => new PlayerQuest
                {
                    PlayerId = player.Id,
                    QuestId = id,
                    Status = "Active"
                }));
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict("Це ім'я вже зайняте");
            }

            return Ok(new { token = CreateToken(player), id = player.Id, name = player.Nickname });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthDto dto)
        {
            var lower = (dto.Nickname ?? "").Trim().ToLowerInvariant();
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Nickname.ToLower() == lower);

            if (player == null
                || string.IsNullOrEmpty(player.PasswordHash)
                || !BCrypt.Net.BCrypt.Verify(dto.Password ?? "", player.PasswordHash))
            {
                return Unauthorized("Невірне ім'я або пароль");
            }

            return Ok(new { token = CreateToken(player), id = player.Id, name = player.Nickname });
        }

        private string CreateToken(Player player)
        {
            var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key не задано");
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("pid", player.Id.ToString()),
                    new Claim("name", player.Nickname)
                }),
                Issuer = AuthConstants.Issuer,
                Audience = AuthConstants.Audience,
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256)
            };
            return new JsonWebTokenHandler().CreateToken(descriptor);
        }
    }
}
