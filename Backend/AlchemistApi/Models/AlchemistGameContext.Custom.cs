using Microsoft.EntityFrameworkCore;

namespace AlchemistApi.Models;

// Наші власні доповнення до контексту. Основний файл AlchemistGameContext.cs не чіпаємо.
public partial class AlchemistGameContext
{
    public virtual DbSet<PlayerRecipe> PlayerRecipes { get; set; }

    public virtual DbSet<PlayerBrew> PlayerBrews { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<PlayerSkill> PlayerSkills { get; set; }

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

        modelBuilder.Entity<PlayerBrew>(entity =>
        {
            entity.ToTable("PlayerBrews");
            entity.HasKey(e => e.Id);

            entity.HasOne(d => d.Player).WithMany()
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.ResultItem).WithMany()
                .HasForeignKey(d => d.ResultItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.ToTable("Skills");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EffectValue).HasColumnType("decimal(6, 2)");
        });

        modelBuilder.Entity<PlayerSkill>(entity =>
        {
            entity.ToTable("PlayerSkills");
            entity.HasKey(e => new { e.PlayerId, e.SkillId });

            entity.HasOne(d => d.Player).WithMany()
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Skill).WithMany()
                .HasForeignKey(d => d.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
