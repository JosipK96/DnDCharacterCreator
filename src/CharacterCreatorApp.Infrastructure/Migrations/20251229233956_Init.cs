using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CharacterCreatorApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: true),
                    RaceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SubraceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ClassId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SubclassId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BackgroundId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BaseAbilityScores = table.Column<string>(type: "jsonb", nullable: false),
                    SelectedClassProficiencyIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    SelectedFeatIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    SelectedSpellIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterDrafts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RaceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubraceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ClassId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubclassId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BackgroundId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AbilityScores = table.Column<string>(type: "jsonb", nullable: false),
                    ProficiencyIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    LanguageIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    FeatureIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    SpellIds = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HitDie = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Feats",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    MinimumAbilityScore = table.Column<int>(type: "integer", nullable: true),
                    RequiredAbilityScore = table.Column<string>(type: "text", nullable: true),
                    RequiredRaceId = table.Column<string>(type: "text", nullable: true),
                    RequiredClassId = table.Column<string>(type: "text", nullable: true),
                    RequiredLevel = table.Column<int>(type: "integer", nullable: true),
                    IsRepeatable = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedProficiencyIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    GrantedLanguageIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    GrantedSpellIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    Choice = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Script = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proficiencies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proficiencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Races",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Size = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Speed = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Races", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Spells",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    School = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CastingTimeNumber = table.Column<int>(type: "integer", nullable: false),
                    CastingTimeUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RangeType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RangeDistanceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RangeDistanceAmount = table.Column<int>(type: "integer", nullable: true),
                    HasVerbalComponent = table.Column<bool>(type: "boolean", nullable: false),
                    HasSomaticComponent = table.Column<bool>(type: "boolean", nullable: false),
                    HasMaterialComponent = table.Column<bool>(type: "boolean", nullable: false),
                    Material = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DurationType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DurationAmount = table.Column<int>(type: "integer", nullable: true),
                    DurationUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsConcentration = table.Column<bool>(type: "boolean", nullable: false),
                    IsRitual = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spells", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProficiencyChoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClassId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProficiencyChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProficiencyChoices_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subclasses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClassId = table.Column<string>(type: "character varying(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subclasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subclasses_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassProficiencies",
                columns: table => new
                {
                    ClassId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProficiencyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassProficiencies", x => new { x.ClassId, x.ProficiencyId });
                    table.ForeignKey(
                        name: "FK_ClassProficiencies_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassProficiencies_Proficiencies_ProficiencyId",
                        column: x => x.ProficiencyId,
                        principalTable: "Proficiencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaceAbilityBonuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RaceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AbilityScoreId = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Bonus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceAbilityBonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaceAbilityBonuses_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaceLanguages",
                columns: table => new
                {
                    RaceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Language = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceLanguages", x => new { x.RaceId, x.Language });
                    table.ForeignKey(
                        name: "FK_RaceLanguages_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaceProficiencies",
                columns: table => new
                {
                    RaceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProficiencyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceProficiencies", x => new { x.RaceId, x.ProficiencyId });
                    table.ForeignKey(
                        name: "FK_RaceProficiencies_Proficiencies_ProficiencyId",
                        column: x => x.ProficiencyId,
                        principalTable: "Proficiencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaceProficiencies_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaceTraits",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RaceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceTraits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaceTraits_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subraces",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RaceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subraces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subraces_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpellClasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpellId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClassId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpellClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpellClasses_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpellClasses_Spells_SpellId",
                        column: x => x.SpellId,
                        principalTable: "Spells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProficiencyChoiceOptions",
                columns: table => new
                {
                    ProficiencyChoiceId = table.Column<int>(type: "integer", nullable: false),
                    ProficiencyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProficiencyChoiceOptions", x => new { x.ProficiencyChoiceId, x.ProficiencyId });
                    table.ForeignKey(
                        name: "FK_ProficiencyChoiceOptions_Proficiencies_ProficiencyId",
                        column: x => x.ProficiencyId,
                        principalTable: "Proficiencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProficiencyChoiceOptions_ProficiencyChoices_ProficiencyChoi~",
                        column: x => x.ProficiencyChoiceId,
                        principalTable: "ProficiencyChoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    ClassId = table.Column<string>(type: "character varying(50)", nullable: true),
                    SubclassId = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Features_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Features_Subclasses_SubclassId",
                        column: x => x.SubclassId,
                        principalTable: "Subclasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpellSubclasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpellId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubclassId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpellSubclasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpellSubclasses_Spells_SpellId",
                        column: x => x.SpellId,
                        principalTable: "Spells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpellSubclasses_Subclasses_SubclassId",
                        column: x => x.SubclassId,
                        principalTable: "Subclasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubraceAbilityBonuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubraceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AbilityScoreId = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Bonus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubraceAbilityBonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubraceAbilityBonuses_Subraces_SubraceId",
                        column: x => x.SubraceId,
                        principalTable: "Subraces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubraceProficiencies",
                columns: table => new
                {
                    SubraceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProficiencyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubraceProficiencies", x => new { x.SubraceId, x.ProficiencyId });
                    table.ForeignKey(
                        name: "FK_SubraceProficiencies_Proficiencies_ProficiencyId",
                        column: x => x.ProficiencyId,
                        principalTable: "Proficiencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubraceProficiencies_Subraces_SubraceId",
                        column: x => x.SubraceId,
                        principalTable: "Subraces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubraceTraits",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubraceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubraceTraits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubraceTraits_Subraces_SubraceId",
                        column: x => x.SubraceId,
                        principalTable: "Subraces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Backgrounds",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Languages = table.Column<List<string>>(type: "text[]", nullable: false),
                    FeatureId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Backgrounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Backgrounds_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundProficiencies",
                columns: table => new
                {
                    BackgroundId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProficiencyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundProficiencies", x => new { x.BackgroundId, x.ProficiencyId });
                    table.ForeignKey(
                        name: "FK_BackgroundProficiencies_Backgrounds_BackgroundId",
                        column: x => x.BackgroundId,
                        principalTable: "Backgrounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BackgroundProficiencies_Proficiencies_ProficiencyId",
                        column: x => x.ProficiencyId,
                        principalTable: "Proficiencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundProficiencies_ProficiencyId",
                table: "BackgroundProficiencies",
                column: "ProficiencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Backgrounds_FeatureId",
                table: "Backgrounds",
                column: "FeatureId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterDrafts_CreatedAt",
                table: "CharacterDrafts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterDrafts_CreatedByUserId",
                table: "CharacterDrafts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterDrafts_CreatedByUserId_IsCompleted",
                table: "CharacterDrafts",
                columns: new[] { "CreatedByUserId", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_ClassId",
                table: "Characters",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_ClassId_Level",
                table: "Characters",
                columns: new[] { "ClassId", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_CreatedAt",
                table: "Characters",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Level",
                table: "Characters",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_RaceId",
                table: "Characters",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId",
                table: "Characters",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId_Level",
                table: "Characters",
                columns: new[] { "UserId", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId_Name",
                table: "Characters",
                columns: new[] { "UserId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassProficiencies_ProficiencyId",
                table: "ClassProficiencies",
                column: "ProficiencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Feats_RequiredClassId",
                table: "Feats",
                column: "RequiredClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Feats_RequiredLevel",
                table: "Feats",
                column: "RequiredLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Feats_RequiredRaceId",
                table: "Feats",
                column: "RequiredRaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Features_ClassId_Level",
                table: "Features",
                columns: new[] { "ClassId", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_Features_SubclassId",
                table: "Features",
                column: "SubclassId");

            migrationBuilder.CreateIndex(
                name: "IX_ProficiencyChoiceOptions_ProficiencyId",
                table: "ProficiencyChoiceOptions",
                column: "ProficiencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProficiencyChoices_ClassId",
                table: "ProficiencyChoices",
                column: "ClassId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaceAbilityBonuses_RaceId",
                table: "RaceAbilityBonuses",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceProficiencies_ProficiencyId",
                table: "RaceProficiencies",
                column: "ProficiencyId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceTraits_RaceId",
                table: "RaceTraits",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SpellClasses_ClassId",
                table: "SpellClasses",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_SpellClasses_SpellId_ClassId",
                table: "SpellClasses",
                columns: new[] { "SpellId", "ClassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Spells_HasMaterialComponent",
                table: "Spells",
                column: "HasMaterialComponent");

            migrationBuilder.CreateIndex(
                name: "IX_Spells_IsConcentration",
                table: "Spells",
                column: "IsConcentration");

            migrationBuilder.CreateIndex(
                name: "IX_Spells_IsRitual",
                table: "Spells",
                column: "IsRitual");

            migrationBuilder.CreateIndex(
                name: "IX_Spells_Level",
                table: "Spells",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_Spells_Level_IsRitual",
                table: "Spells",
                columns: new[] { "Level", "IsRitual" });

            migrationBuilder.CreateIndex(
                name: "IX_Spells_Level_School",
                table: "Spells",
                columns: new[] { "Level", "School" });

            migrationBuilder.CreateIndex(
                name: "IX_Spells_School",
                table: "Spells",
                column: "School");

            migrationBuilder.CreateIndex(
                name: "IX_Spells_School_IsConcentration",
                table: "Spells",
                columns: new[] { "School", "IsConcentration" });

            migrationBuilder.CreateIndex(
                name: "IX_SpellSubclasses_SpellId_SubclassId",
                table: "SpellSubclasses",
                columns: new[] { "SpellId", "SubclassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpellSubclasses_SubclassId",
                table: "SpellSubclasses",
                column: "SubclassId");

            migrationBuilder.CreateIndex(
                name: "IX_Subclasses_ClassId",
                table: "Subclasses",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_SubraceAbilityBonuses_SubraceId",
                table: "SubraceAbilityBonuses",
                column: "SubraceId");

            migrationBuilder.CreateIndex(
                name: "IX_SubraceProficiencies_ProficiencyId",
                table: "SubraceProficiencies",
                column: "ProficiencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Subraces_RaceId",
                table: "Subraces",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SubraceTraits_SubraceId",
                table: "SubraceTraits",
                column: "SubraceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BackgroundProficiencies");

            migrationBuilder.DropTable(
                name: "CharacterDrafts");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "ClassProficiencies");

            migrationBuilder.DropTable(
                name: "Feats");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "ProficiencyChoiceOptions");

            migrationBuilder.DropTable(
                name: "RaceAbilityBonuses");

            migrationBuilder.DropTable(
                name: "RaceLanguages");

            migrationBuilder.DropTable(
                name: "RaceProficiencies");

            migrationBuilder.DropTable(
                name: "RaceTraits");

            migrationBuilder.DropTable(
                name: "SpellClasses");

            migrationBuilder.DropTable(
                name: "SpellSubclasses");

            migrationBuilder.DropTable(
                name: "SubraceAbilityBonuses");

            migrationBuilder.DropTable(
                name: "SubraceProficiencies");

            migrationBuilder.DropTable(
                name: "SubraceTraits");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Backgrounds");

            migrationBuilder.DropTable(
                name: "ProficiencyChoices");

            migrationBuilder.DropTable(
                name: "Spells");

            migrationBuilder.DropTable(
                name: "Proficiencies");

            migrationBuilder.DropTable(
                name: "Subraces");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DropTable(
                name: "Races");

            migrationBuilder.DropTable(
                name: "Subclasses");

            migrationBuilder.DropTable(
                name: "Classes");
        }
    }
}
