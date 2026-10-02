using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AlchemistApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CauldronUpgrades",
                columns: table => new
                {
                    Level = table.Column<int>(type: "integer", nullable: false),
                    UpgradeCostGold = table.Column<int>(type: "integer", nullable: false),
                    MaxIngredients = table.Column<int>(type: "integer", nullable: false),
                    CraftTimeModifier = table.Column<decimal>(type: "numeric(3,2)", nullable: true, defaultValue: 1.0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Cauldron__AAF8996301680C8E", x => x.Level);
                });

            migrationBuilder.CreateTable(
                name: "Quests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RewardGold = table.Column<int>(type: "integer", nullable: false),
                    RewardXp = table.Column<int>(type: "integer", nullable: false),
                    RequiredLevel = table.Column<int>(type: "integer", nullable: false),
                    IsRepeatable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Quests__3214EC07D2246A3B", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rarities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ColorHex = table.Column<string>(type: "character varying(7)", unicode: false, maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Rarities__3214EC07362BCA1C", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IconPath = table.Column<string>(type: "text", nullable: true),
                    Branch = table.Column<string>(type: "text", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    Cost = table.Column<int>(type: "integer", nullable: false),
                    MaxRank = table.Column<int>(type: "integer", nullable: false),
                    PrerequisiteSkillId = table.Column<int>(type: "integer", nullable: true),
                    EffectType = table.Column<string>(type: "text", nullable: false),
                    EffectValue = table.Column<decimal>(type: "numeric(6,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nickname = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Gold = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    Experience = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    CauldronLevel = table.Column<int>(type: "integer", nullable: true, defaultValue: 1),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    SkillPoints = table.Column<int>(type: "integer", nullable: false),
                    Energy = table.Column<int>(type: "integer", nullable: false),
                    MaxEnergy = table.Column<int>(type: "integer", nullable: false),
                    LastEnergyUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Players__3214EC07AE347488", x => x.Id);
                    table.ForeignKey(
                        name: "FK__Players__Cauldro__571DF1D5",
                        column: x => x.CauldronLevel,
                        principalTable: "CauldronUpgrades",
                        principalColumn: "Level");
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ItemType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RarityId = table.Column<int>(type: "integer", nullable: true),
                    BasePrice = table.Column<int>(type: "integer", nullable: false),
                    IconPath = table.Column<string>(type: "character varying(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Items__3214EC07AD579FCA", x => x.Id);
                    table.ForeignKey(
                        name: "FK__Items__RarityId__5070F446",
                        column: x => x.RarityId,
                        principalTable: "Rarities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlayerQuests",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    QuestId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PlayerQu__E1286D6A692AB7DF", x => new { x.PlayerId, x.QuestId });
                    table.ForeignKey(
                        name: "FK__PlayerQue__Playe__71D1E811",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__PlayerQue__Quest__72C60C4A",
                        column: x => x.QuestId,
                        principalTable: "Quests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlayerSkills",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSkills", x => new { x.PlayerId, x.SkillId });
                    table.ForeignKey(
                        name: "FK_PlayerSkills_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerBrews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    ResultItemId = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadyAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerBrews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerBrews_Items_ResultItemId",
                        column: x => x.ResultItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerBrews_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerInventory",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PlayerIn__ED699CF02A26041C", x => new { x.PlayerId, x.ItemId });
                    table.ForeignKey(
                        name: "FK__PlayerInv__ItemI__5AEE82B9",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__PlayerInv__Playe__59FA5E80",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuestRequirements",
                columns: table => new
                {
                    QuestId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    QuantityNeeded = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__QuestReq__114672134BE28B4F", x => new { x.QuestId, x.ItemId });
                    table.ForeignKey(
                        name: "FK__QuestRequ__ItemI__6E01572D",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__QuestRequ__Quest__6D0D32F4",
                        column: x => x.QuestId,
                        principalTable: "Quests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ResultItemId = table.Column<int>(type: "integer", nullable: true),
                    RequiredXp = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    RequiredLevel = table.Column<int>(type: "integer", nullable: false),
                    BrewTimeSeconds = table.Column<int>(type: "integer", nullable: false),
                    RequiredSkillId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Recipes__3214EC07E19765D7", x => x.Id);
                    table.ForeignKey(
                        name: "FK__Recipes__ResultI__5EBF139D",
                        column: x => x.ResultItemId,
                        principalTable: "Items",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShopAssortment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<int>(type: "integer", nullable: true),
                    PurchasePrice = table.Column<int>(type: "integer", nullable: false),
                    RequiredPlayerLevel = table.Column<int>(type: "integer", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ShopAsso__3214EC076C0E96EF", x => x.Id);
                    table.ForeignKey(
                        name: "FK__ShopAssor__ItemI__6754599E",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlayerRecipes",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    RecipeId = table.Column<int>(type: "integer", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerRecipes", x => new { x.PlayerId, x.RecipeId });
                    table.ForeignKey(
                        name: "FK_PlayerRecipes_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerRecipes_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                columns: table => new
                {
                    RecipeId = table.Column<int>(type: "integer", nullable: false),
                    IngredientItemId = table.Column<int>(type: "integer", nullable: false),
                    QuantityNeeded = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RecipeIn__8A399C04FEE93838", x => new { x.RecipeId, x.IngredientItemId });
                    table.ForeignKey(
                        name: "FK__RecipeIng__Ingre__6383C8BA",
                        column: x => x.IngredientItemId,
                        principalTable: "Items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__RecipeIng__Recip__628FA481",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_RarityId",
                table: "Items",
                column: "RarityId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBrews_PlayerId",
                table: "PlayerBrews",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBrews_ResultItemId",
                table: "PlayerBrews",
                column: "ResultItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInventory_ItemId",
                table: "PlayerInventory",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerQuests_QuestId",
                table: "PlayerQuests",
                column: "QuestId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRecipes_RecipeId",
                table: "PlayerRecipes",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_CauldronLevel",
                table: "Players",
                column: "CauldronLevel");

            migrationBuilder.CreateIndex(
                name: "UQ__Players__CC6CD17ECC37648B",
                table: "Players",
                column: "Nickname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSkills_SkillId",
                table: "PlayerSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestRequirements_ItemId",
                table: "QuestRequirements",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_IngredientItemId",
                table: "RecipeIngredients",
                column: "IngredientItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_ResultItemId",
                table: "Recipes",
                column: "ResultItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopAssortment_ItemId",
                table: "ShopAssortment",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerBrews");

            migrationBuilder.DropTable(
                name: "PlayerInventory");

            migrationBuilder.DropTable(
                name: "PlayerQuests");

            migrationBuilder.DropTable(
                name: "PlayerRecipes");

            migrationBuilder.DropTable(
                name: "PlayerSkills");

            migrationBuilder.DropTable(
                name: "QuestRequirements");

            migrationBuilder.DropTable(
                name: "RecipeIngredients");

            migrationBuilder.DropTable(
                name: "ShopAssortment");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropTable(
                name: "Quests");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "CauldronUpgrades");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Rarities");
        }
    }
}
