using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using CharacterCreatorApp.Api.IntegrationTests.Fixtures;
using CharacterCreatorApp.Contracts.Responses;
using CharacterCreatorApp.Infrastructure;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CharacterCreatorApp.Api.IntegrationTests.Controllers;

/// <summary>
/// Every reference-data list, read through the API against the seeded database. The races list
/// threw on the seed data's lower-case ability ids and nothing noticed, because no test ever
/// called it; these make sure each list at least loads.
/// </summary>
[Collection(SharedApiHost.Name)]
public class LookupControllersTests(CustomWebApplicationFactory factory)
{
    private readonly CustomWebApplicationFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/api/races/get-list")]
    [InlineData("/api/classes/get-list")]
    [InlineData("/api/backgrounds")]
    [InlineData("/api/feats")]
    [InlineData("/api/languages/get-list")]
    [InlineData("/api/proficiencies")]
    [InlineData("/api/spells?classId=wizard&maxLevel=1")]
    [InlineData("/api/subclasses?classId=wizard")]
    public async Task GetList_ReturnsTheSeededItems(string url)
    {
        // Act
        var response = await _client.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        items.ValueKind.Should().Be(JsonValueKind.Array);
        items.GetArrayLength().Should().BePositive();
    }

    [Theory]
    [InlineData("/api/classes/wizard")]
    [InlineData("/api/backgrounds/acolyte")]
    public async Task GetById_WithASeededId_ReturnsIt(string url)
    {
        // Act
        var response = await _client.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/api/classes/no-such-class", "Class.NotFound")]
    [InlineData("/api/backgrounds/no-such-background", "Background.NotFound")]
    [InlineData("/api/feats/no-such-feat", "Feat.NotFound")]
    public async Task GetById_WithAnUnknownId_ReturnsNotFoundWithTheCode(string url, string code)
    {
        // Act
        var response = await _client.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.ReadProblemAsync();
        problem.Errors.Should().ContainSingle().Which.Code.Should().Be(code);
    }

    [Theory]
    [InlineData("2014", "wizard")]
    [InlineData("2024", "wizard-2024")]
    public async Task GetClasses_ForAnEdition_ReturnsOnlyThatEditionsClasses(string edition, string wizardId)
    {
        // Act
        var classes = await _client.GetFromJsonAsync<List<ClassApiResponse>>(
            $"/api/classes/get-list?edition={edition}", TestContext.Current.CancellationToken);

        // Assert
        classes.Should().HaveCount(13).And.OnlyContain(c => c.Edition == edition);
        classes!.Single(c => c.Name == "Wizard").Id.Should().Be(wizardId);
    }

    [Fact]
    public async Task GetClasses_WithoutAnEdition_ReturnsBoth()
    {
        // Act
        var classes = await _client.GetFromJsonAsync<List<ClassApiResponse>>(
            "/api/classes/get-list", TestContext.Current.CancellationToken);

        // Assert
        classes.Should().HaveCount(26);
    }

    [Fact]
    public async Task GetClasses_WithAnUnknownEdition_ReturnsItsCode()
    {
        // Act
        var response = await _client.GetAsync("/api/classes/get-list?edition=2019", TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "Class.InvalidEdition");
    }

    [Fact]
    public async Task GetClass_ForA2024Class_HasTheRulesOfThatEdition()
    {
        // Act - every 2024 class picks its subclass at 3; the 2014 cleric picks at 1
        var cleric2014 = await _client.GetFromJsonAsync<ClassApiResponse>("/api/classes/cleric", TestContext.Current.CancellationToken);
        var cleric2024 = await _client.GetFromJsonAsync<ClassApiResponse>("/api/classes/cleric-2024", TestContext.Current.CancellationToken);

        // Assert
        (cleric2014!.Edition, cleric2014.SubclassLevel).Should().Be(("2014", 1));
        (cleric2024!.Edition, cleric2024.SubclassLevel).Should().Be(("2024", 3));
    }

    [Theory]
    [InlineData("2014", 9, "elf")]
    [InlineData("2024", 10, "elf_2024")]
    public async Task GetRaces_ForAnEdition_ReturnsOnlyThatEditionsRaces(string edition, int count, string elfId)
    {
        // Act
        var races = await _client.GetFromJsonAsync<List<RaceApiResponse>>(
            $"/api/races/get-list?edition={edition}", TestContext.Current.CancellationToken);

        // Assert
        races.Should().HaveCount(count).And.OnlyContain(r => r.Edition == edition);
        races!.Single(r => r.Name == "Elf").Id.Should().Be(elfId);
    }

    [Fact]
    public async Task GetRaces_WithAnUnknownEdition_ReturnsItsCode()
    {
        // Act
        var response = await _client.GetAsync("/api/races/get-list?edition=2019", TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "Race.InvalidEdition");
    }

    [Fact]
    public async Task GetRaces_For2024_IncludesEachLineageWithWhatItChanges()
    {
        // Act - the query once left out what a race holds; lineages hold speed and extra choices
        var races = await _client.GetFromJsonAsync<List<RaceApiResponse>>(
            "/api/races/get-list?edition=2024", TestContext.Current.CancellationToken);

        // Assert
        var elf = races!.Single(r => r.Id == "elf_2024");
        elf.SkillChoiceOptions.Should().Equal("insight", "perception", "survival");
        elf.Subraces.Select(s => (s.Name, s.Speed)).Should().Equal(("Drow Lineage", null), ("High Elf Lineage", null), ("Wood Elf Lineage", (int?)35));
    }

    [Fact]
    public async Task GetRaces_IncludesTheLanguagesEachRaceSpeaks()
    {
        // Act - the race queries once left RaceLanguages out, and every race spoke nothing
        var races = await _client.GetFromJsonAsync<List<RaceApiResponse>>(
            "/api/races/get-list", TestContext.Current.CancellationToken);

        // Assert
        races!.Single(r => r.Id == "dwarf").Languages.Should().BeEquivalentTo(["Common", "Dwarvish"]);
    }

    [Fact]
    public async Task GetBackgrounds_For2024_RaiseThreeAbilitiesAndGiveAnOriginFeat()
    {
        // Act
        var backgrounds = await _client.GetFromJsonAsync<List<BackgroundApiResponse>>(
            "/api/backgrounds?edition=2024", TestContext.Current.CancellationToken);

        // Assert
        backgrounds.Should().OnlyContain(b => b.Edition == "2024");
        var acolyte = backgrounds!.Single(b => b.Id == "acolyte_2024");
        acolyte.AbilityScoreOptions.Should().Equal("INT", "WIS", "CHA");
        acolyte.Feat!.Name.Should().Be("Magic Initiate");
        acolyte.Feature.Should().BeNull();
        (acolyte.Languages.Select(l => l.Name).Single(), acolyte.LanguageChoiceCount).Should().Be(("Common", 2));
    }

    [Fact]
    public async Task GetBackgrounds_For2014_HaveTheirFeatureAndNoAbilityIncreases()
    {
        // Act
        var backgrounds = await _client.GetFromJsonAsync<List<BackgroundApiResponse>>(
            "/api/backgrounds?edition=2014", TestContext.Current.CancellationToken);

        // Assert
        backgrounds.Should().OnlyContain(b => b.Edition == "2014" && b.AbilityScoreOptions.Count == 0);
        backgrounds!.Single(b => b.Id == "acolyte").Feature!.Name.Should().Be("Shelter of the Faithful");
    }

    [Theory]
    [InlineData("/api/backgrounds?edition=2019", "Background.InvalidEdition")]
    [InlineData("/api/languages/get-list?edition=2019", "Language.InvalidEdition")]
    public async Task GetList_WithAnUnknownEdition_ReturnsItsCode(string url, string code)
    {
        // Act
        var response = await _client.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, code);
    }

    [Fact]
    public async Task GetLanguages_For2024_OffersOnlyThe2024StandardLanguages()
    {
        // Act - a 2024 character picks from the Standard Languages table; the rare ones, such as
        // Celestial, come from features
        var languages = await _client.GetFromJsonAsync<List<LanguageApiResponse>>(
            "/api/languages/get-list?edition=2024", TestContext.Current.CancellationToken);

        // Assert
        languages!.Select(l => l.Id).Should().BeEquivalentTo(
        [
            "common-xphb", "common-sign-language-xphb", "draconic-xphb", "dwarvish-xphb", "elvish-xphb",
            "giant-xphb", "gnomish-xphb", "goblin-xphb", "halfling-xphb", "orc-xphb",
        ]);
    }

    [Fact]
    public async Task GetLanguages_LeavesOutTheSecretLanguagesAClassGrants()
    {
        // Act
        var languages = await _client.GetFromJsonAsync<List<LanguageApiResponse>>(
            "/api/languages/get-list", TestContext.Current.CancellationToken);

        // Assert
        languages!.Select(l => l.Id).Should().Contain("common-phb")
            .And.NotContain(["druidic-phb", "thieves-cant-phb"]);
    }

    [Fact]
    public async Task GetSpells_DescribesTheSpellTheWayTheBookDoes()
    {
        // Act
        var spells = await _client.GetFromJsonAsync<List<SpellApiResponse>>(
            "/api/spells?classId=cleric&maxLevel=0", TestContext.Current.CancellationToken);

        // Assert - the seed data spells this "1 action", "point / touch" and "roll a {@dice d4}"
        var guidance = spells!.Single(s => s.Id == "guidance");
        guidance.CastingTime.Should().Be("1 action");
        guidance.Range.Should().Be("Touch");
        guidance.Duration.Should().Be("1 minute");
        guidance.Description.Should().Contain("roll a d4");
    }

    [Theory]
    [InlineData("2014", "alert_2014")]
    [InlineData("2024", "alert_2024")]
    public async Task GetFeats_ForAnEdition_ReturnsOnlyThatEditionsFeats(string edition, string alertId)
    {
        // Act
        var feats = await _client.GetFromJsonAsync<List<FeatApiResponse>>(
            $"/api/feats?edition={edition}", TestContext.Current.CancellationToken);

        // Assert - both books call it "Alert", and the id tells them apart
        feats.Should().NotBeEmpty().And.OnlyContain(f => f.Edition == edition);
        feats!.Single(f => f.Name == "Alert").Id.Should().Be(alertId);
    }

    [Fact]
    public async Task GetFeats_WithoutAnEdition_ReturnsBothWithTheirCategories()
    {
        // Act
        var feats = await _client.GetFromJsonAsync<List<FeatApiResponse>>("/api/feats", TestContext.Current.CancellationToken);

        // Assert - only the 2024 books sort feats into categories
        feats!.Where(f => f.Name == "Alert").Select(f => (f.Id, f.Edition, f.Category))
            .Should().BeEquivalentTo([("alert_2014", "2014", (string?)null), ("alert_2024", "2024", "Origin")]);
    }

    [Fact]
    public async Task GetFeats_WithAnUnknownEdition_ReturnsItsCode()
    {
        // Act
        var response = await _client.GetAsync("/api/feats?edition=2019", TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "Feat.InvalidEdition");
    }

    [Theory]
    [InlineData("boon_of_fate", new[] { "Level 19" })]
    [InlineData("athlete_2024", new[] { "Level 4", "Strength or Dexterity 13" })]
    [InlineData("elven_accuracy", new[] { "Elf or Half-Elf" })]
    [InlineData("war_caster_2014", new[] { "The ability to cast at least one spell" })]
    [InlineData("war_caster_2024", new[] { "Level 4", "The Spellcasting or Pact Magic feature" })]
    [InlineData("archery_2024", new[] { "The Fighting Style feature" })]
    [InlineData("blessed_warrior", new[] { "Paladin", "The Fighting Style feature" })]
    [InlineData("squire_of_solamnia", new[] { "Fighter or Paladin or the Knight of Solamnia background", "A Dragonlance campaign" })]
    public async Task GetFeats_ListsEachFeatsPrerequisites(string featId, string[] expected)
    {
        // Act - these were in the seed data all along, and never reached a feat
        var feats = await _client.GetFromJsonAsync<List<FeatApiResponse>>("/api/feats", TestContext.Current.CancellationToken);

        // Assert
        feats!.Single(f => f.Id == featId).Prerequisites.Should().Equal(expected);
    }

    [Fact]
    public async Task GetFeats_SaysWhatEachFeatRaises()
    {
        // Act - the improvements step asks which ability when there is more than one
        var feats = await _client.GetFromJsonAsync<List<FeatApiResponse>>("/api/feats", TestContext.Current.CancellationToken);

        // Assert
        var byId = feats!.ToDictionary(f => f.Id);
        byId["athlete_2014"].AbilityIncrease.Should().BeEquivalentTo(new FeatAbilityIncreaseApiResponse(["STR", "DEX"], 1, 20));
        byId["boon_of_fate"].AbilityIncrease!.Maximum.Should().Be(30);
        byId["alert_2014"].AbilityIncrease.Should().BeNull();
    }

    [Fact]
    public async Task GetRaces_For2014_IncludesThePicksTheHalfElfAndVariantHumanMake()
    {
        // Act
        var races = await _client.GetFromJsonAsync<List<RaceApiResponse>>(
            "/api/races/get-list?edition=2014", TestContext.Current.CancellationToken);

        // Assert
        var halfElf = races!.Single(r => r.Id == "half_elf");
        (halfElf.AbilityChoiceCount, halfElf.AbilityChoiceOptions).Should().BeEquivalentTo((2, new[] { "STR", "DEX", "CON", "INT", "WIS" }));

        var variant = races!.Single(r => r.Id == "human").Subraces.Single(s => s.Id == "variant_human");
        (variant.AbilityChoiceCount, variant.SkillChoiceCount, variant.FeatChoiceCount, variant.FeatChoiceCategory).Should().Be((2, 1, 1, (string?)null));
    }

    [Fact]
    public async Task GetRaces_For2024_GivesTheHumanAnOriginFeat()
    {
        // Act
        var races = await _client.GetFromJsonAsync<List<RaceApiResponse>>(
            "/api/races/get-list?edition=2024", TestContext.Current.CancellationToken);

        // Assert
        var human = races!.Single(r => r.Id == "human_2024");
        (human.FeatChoiceCount, human.FeatChoiceCategory).Should().Be((1, "Origin"));
    }

    [Fact]
    public async Task Seeding_LeavesNoFiveEToolsMarkupInAnyDescription()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var spells = await db.Spells.CountAsync(s => s.Description.Contains("{@"), cancellationToken);
        var features = await db.Features.CountAsync(f => f.Description.Contains("{@"), cancellationToken);
        var feats = await db.Feats.CountAsync(f => f.Description.Contains("{@"), cancellationToken);

        // Assert - over 4,000 tags in the source files, none of them in the database
        (spells, features, feats).Should().Be((0, 0, 0));
    }
}
