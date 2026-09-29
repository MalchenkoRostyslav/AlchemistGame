using Microsoft.EntityFrameworkCore;

namespace AlchemistApi.Models;

// Наші власні доповнення до контексту. Основний файл AlchemistGameContext.cs не чіпаємо.
public partial class AlchemistGameContext
{
    public virtual DbSet<PlayerRecipe> PlayerRecipes { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerRecipe>(entity =>
        {
            entity.ToTable("PlayerRecipes");
            entity.HasKey(e => new { e.PlayerId, e.RecipeId });

            entity.HasOne(d => d.Player).WithMany(p => p.PlayerRecipes)
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Recipe).WithMany(p => p.PlayerRecipes)
                .HasForeignKey(d => d.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}