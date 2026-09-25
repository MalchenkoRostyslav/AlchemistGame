using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AlchemistApi.Models;

public partial class AlchemistGameContext : DbContext
{
    public AlchemistGameContext()
    {
    }

    public AlchemistGameContext(DbContextOptions<AlchemistGameContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CauldronUpgrade> CauldronUpgrades { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<Player> Players { get; set; }

    public virtual DbSet<PlayerInventory> PlayerInventories { get; set; }

    public virtual DbSet<PlayerQuest> PlayerQuests { get; set; }

    public virtual DbSet<Quest> Quests { get; set; }

    public virtual DbSet<QuestRequirement> QuestRequirements { get; set; }

    public virtual DbSet<Rarity> Rarities { get; set; }

    public virtual DbSet<Recipe> Recipes { get; set; }

    public virtual DbSet<RecipeIngredient> RecipeIngredients { get; set; }

    public virtual DbSet<ShopAssortment> ShopAssortments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CauldronUpgrade>(entity =>
        {
            entity.HasKey(e => e.Level).HasName("PK__Cauldron__AAF8996301680C8E");

            entity.Property(e => e.Level).ValueGeneratedNever();
            entity.Property(e => e.CraftTimeModifier)
                .HasDefaultValue(1.0m)
                .HasColumnType("decimal(3, 2)");
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Items__3214EC07AD579FCA");

            entity.Property(e => e.IconPath)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ItemType).HasMaxLength(20);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Rarity).WithMany(p => p.Items)
                .HasForeignKey(d => d.RarityId)
                .HasConstraintName("FK__Items__RarityId__5070F446");
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Players__3214EC07AE347488");

            entity.HasIndex(e => e.Nickname, "UQ__Players__CC6CD17ECC37648B").IsUnique();

            entity.Property(e => e.CauldronLevel).HasDefaultValue(1);
            entity.Property(e => e.Experience).HasDefaultValue(0);
            entity.Property(e => e.Gold).HasDefaultValue(0);
            entity.Property(e => e.Nickname).HasMaxLength(50);

            entity.HasOne(d => d.CauldronLevelNavigation).WithMany(p => p.Players)
                .HasForeignKey(d => d.CauldronLevel)
                .HasConstraintName("FK__Players__Cauldro__571DF1D5");
        });

        modelBuilder.Entity<PlayerInventory>(entity =>
        {
            entity.HasKey(e => new { e.PlayerId, e.ItemId }).HasName("PK__PlayerIn__ED699CF02A26041C");

            entity.ToTable("PlayerInventory");

            entity.HasOne(d => d.Item).WithMany(p => p.PlayerInventories)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PlayerInv__ItemI__5AEE82B9");

            entity.HasOne(d => d.Player).WithMany(p => p.PlayerInventories)
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PlayerInv__Playe__59FA5E80");
        });

        modelBuilder.Entity<PlayerQuest>(entity =>
        {
            entity.HasKey(e => new { e.PlayerId, e.QuestId }).HasName("PK__PlayerQu__E1286D6A692AB7DF");

            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.Player).WithMany(p => p.PlayerQuests)
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PlayerQue__Playe__71D1E811");

            entity.HasOne(d => d.Quest).WithMany(p => p.PlayerQuests)
                .HasForeignKey(d => d.QuestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PlayerQue__Quest__72C60C4A");
        });

        modelBuilder.Entity<Quest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Quests__3214EC07D2246A3B");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<QuestRequirement>(entity =>
        {
            entity.HasKey(e => new { e.QuestId, e.ItemId }).HasName("PK__QuestReq__114672134BE28B4F");

            entity.HasOne(d => d.Item).WithMany(p => p.QuestRequirements)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__QuestRequ__ItemI__6E01572D");

            entity.HasOne(d => d.Quest).WithMany(p => p.QuestRequirements)
                .HasForeignKey(d => d.QuestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__QuestRequ__Quest__6D0D32F4");
        });

        modelBuilder.Entity<Rarity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Rarities__3214EC07362BCA1C");

            entity.Property(e => e.ColorHex)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Recipes__3214EC07E19765D7");

            entity.Property(e => e.RequiredXp).HasDefaultValue(0);

            entity.HasOne(d => d.ResultItem).WithMany(p => p.Recipes)
                .HasForeignKey(d => d.ResultItemId)
                .HasConstraintName("FK__Recipes__ResultI__5EBF139D");
        });

        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.HasKey(e => new { e.RecipeId, e.IngredientItemId }).HasName("PK__RecipeIn__8A399C04FEE93838");

            entity.HasOne(d => d.IngredientItem).WithMany(p => p.RecipeIngredients)
                .HasForeignKey(d => d.IngredientItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RecipeIng__Ingre__6383C8BA");

            entity.HasOne(d => d.Recipe).WithMany(p => p.RecipeIngredients)
                .HasForeignKey(d => d.RecipeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RecipeIng__Recip__628FA481");
        });

        modelBuilder.Entity<ShopAssortment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ShopAsso__3214EC076C0E96EF");

            entity.ToTable("ShopAssortment");

            entity.Property(e => e.RequiredPlayerLevel).HasDefaultValue(0);

            entity.HasOne(d => d.Item).WithMany(p => p.ShopAssortments)
                .HasForeignKey(d => d.ItemId)
                .HasConstraintName("FK__ShopAssor__ItemI__6754599E");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
