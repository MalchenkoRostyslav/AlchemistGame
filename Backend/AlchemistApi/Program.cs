using Microsoft.EntityFrameworkCore;
using AlchemistApi.Models;
using AlchemistApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. База даних
builder.Services.AddDbContext<AlchemistGameContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Ігрові сервіси (правила гри: енергія, XP, рівні, розблокування рецептів)
builder.Services.AddScoped<PlayerService>();

// 3. CORS (для розробки дозволяємо будь-який фронтенд)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS має бути перед Authorization та MapControllers
app.UseCors("AllowFrontend");

app.UseAuthorization();
app.MapControllers();

app.Run();