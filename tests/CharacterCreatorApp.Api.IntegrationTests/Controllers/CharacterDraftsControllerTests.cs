using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using CharacterCreatorApp.Api.IntegrationTests.Fixtures;
using CharacterCreatorApp.Contracts.Requests.Character;
using CharacterCreatorApp.Contracts.Requests.CharacterDrafts;
using CharacterCreatorApp.Contracts.Requests.User;
using CharacterCreatorApp.Contracts.Responses;
using CharacterCreatorApp.Domain.ValueObjects;
using CharacterCreatorApp.Infrastructure;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CharacterCreatorApp.Api.IntegrationTests.Controllers;

[Collection(SharedApiHost.Name)]
public class CharacterDraftsControllerTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CharacterDraftsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    [Fact]
    public async Task CreateDraft_WithAuthentication_ReturnsCreatedDraftId()
    {
        // Arrange
        await SignInAsync();

        // Act
        var response = await _client.PostAsync("/api/character-drafts", null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var draftId = await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: TestContext.Current.CancellationToken);
        draftId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateDraft_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/character-drafts", null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetNameAndLevel_WithValidData_ReturnsNoContent()
    {
        // Arrange
        await SignInAsync();

        var draftId = await CreateDraftAsync();
        var request = new SetNameAndLevelApiRequest
        {
            Name = "Thorin Oakenshield",
            Level = 5,
            Edition = RulesEdition.Edition2014
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/character-drafts/{draftId}/name-and-level", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetNameAndLevel_StampsTheDraftsUpdatedAt()
    {
        // Arrange - AuditableEntityInterceptor is registered with the DbContext in
        // AddInfrastructureServices; this proves the running API actually has it
        await SignInAsync();
        var draftId = await CreateDraftAsync();

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/name-and-level",
            new SetNameAndLevelApiRequest { Name = "Thorin", Level = 5, Edition = RulesEdition.Edition2014 },
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var draft = await db.CharacterDrafts.AsNoTracking()
            .SingleAsync(d => d.Id == draftId, TestContext.Current.CancellationToken);
        draft.UpdatedAt.Should().NotBeNull();
        draft.UpdatedAt.Should().BeOnOrAfter(draft.CreatedAt);
    }

    [Fact]
    public async Task SetNameAndLevel_WithInvalidLevel_ReturnsBadRequest()
    {
        // Arrange
        await SignInAsync();

        var draftId = await CreateDraftAsync();
        var request = new SetNameAndLevelApiRequest
        {
            Name = "Test",
            Level = 25, // Invalid - max is 20
            Edition = RulesEdition.Edition2014
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/character-drafts/{draftId}/name-and-level", request, TestContext.Current.CancellationToken);

        // Assert - the validator's own code reaches the client, with the field it is about
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Errors.Should().ContainSingle();
        problem.Errors[0].Code.Should().Be("CharacterDraft.InvalidLevel");
        problem.Errors[0].PropertyName.Should().Be("Level");
    }

    [Fact]
    public async Task SetNameAndLevel_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        await SignInAsync();

        var draftId = await CreateDraftAsync();
        var request = new SetNameAndLevelApiRequest
        {
            Name = string.Empty, // Invalid
            Level = 5,
            Edition = RulesEdition.Edition2014
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/character-drafts/{draftId}/name-and-level", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Errors.Select(e => (e.Code, e.PropertyName)).Should().Contain(("CharacterDraft.NameRequired", "Name"));
    }

    [Fact]
    public async Task SetAbilityScores_WithValidScores_ReturnsNoContent()
    {
        // Arrange
        await SignInAsync();

        var draftId = await CreateDraftAsync();
        var request = new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int>
            {
                ["STR"] = 15,
                ["DEX"] = 14,
                ["CON"] = 13,
                ["INT"] = 12,
                ["WIS"] = 10,
                ["CHA"] = 8
            },
            Method = "standard-array"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/character-drafts/{draftId}/ability-scores", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData("point-buy", new[] { 15, 15, 15, 8, 8, 8 })]
    [InlineData("rolled", new[] { 18, 3, 11, 9, 16, 7 })]
    public async Task SetAbilityScores_WithScoresTheMethodCanProduce_ReturnsNoContent(string method, int[] scores)
    {
        // Arrange
        await SignInAsync();

        var draftId = await CreateDraftAsync();
        var request = new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int>
            {
                ["STR"] = scores[0],
                ["DEX"] = scores[1],
                ["CON"] = scores[2],
                ["INT"] = scores[3],
                ["WIS"] = scores[4],
                ["CHA"] = scores[5]
            },
            Method = method
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/character-drafts/{draftId}/ability-scores", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetAbilityScores_WithInvalidCount_ReturnsBadRequest()
    {
        // Arrange
        await SignInAsync();

        var draftId = await CreateDraftAsync();
        var request = new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int>
            {
                ["STR"] = 15,
                ["DEX"] = 14,
                ["CON"] = 13
                // Missing 3 scores
            },
            Method = "standard-array"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/character-drafts/{draftId}/ability-scores", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetAbilityScores_WithInvalidKeys_ReturnsBadRequest()
    {
        // Arrange
        await SignInAsync();

        var draftId = await CreateDraftAsync();
        var request = new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int>
            {
                ["STRENGTH"] = 15, // Invalid key - should be STR
                ["DEX"] = 14,
                ["CON"] = 13,
                ["INT"] = 12,
                ["WIS"] = 10,
                ["CHA"] = 8
            },
            Method = "standard-array"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/character-drafts/{draftId}/ability-scores", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("point-buy", "CharacterDraft.AbilityScoresDoNotFitMethod")]
    [InlineData("", "CharacterDraft.InvalidAbilityScoreMethod")]
    public async Task SetAbilityScores_WithScoresTheMethodCannotProduce_ReturnsBadRequest(string method, string expectedCode)
    {
        // Arrange - 16 is beyond point buy, and an unnamed method could be anything
        await SignInAsync();

        var draftId = await CreateDraftAsync();
        var request = new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 16, ["DEX"] = 14, ["CON"] = 13, ["INT"] = 12, ["WIS"] = 10, ["CHA"] = 8 },
            Method = method
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/character-drafts/{draftId}/ability-scores", request, TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, expectedCode);
    }

    [Fact]
    public async Task UpdateDraft_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        await SignInAsync();

        var nonExistentId = Guid.NewGuid();
        var request = new SetNameAndLevelApiRequest
        {
            Name = "Test",
            Level = 5,
            Edition = RulesEdition.Edition2014
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/character-drafts/{nonExistentId}/name-and-level", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.ReadProblemAsync();
        problem.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.NotFound");
    }

    [Fact]
    public async Task GetIncompleteDraft_WhenTheUserHasNone_ReturnsNotFoundWithItsOwnCode()
    {
        // Arrange - the wizard reads this 404 as "start a new draft", so it must not be a bare 404
        await SignInAsync();

        // Act
        var response = await _client.GetAsync("/api/character-drafts/incomplete", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.ReadProblemAsync();
        problem.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.NoIncompleteDraft");
    }

    [Fact]
    public async Task UpdateDraft_AsWrongUser_ReturnsNotFound()
    {
        // Arrange - User 1 creates a draft
        var token1 = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        var draftId = await CreateDraftAsync();

        // Act - User 2 tries to update User 1's draft
        var token2 = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);

        var request = new SetNameAndLevelApiRequest
        {
            Name = "Hacker",
            Level = 5,
            Edition = RulesEdition.Edition2014
        };
        var response = await _client.PutAsJsonAsync($"/api/character-drafts/{draftId}/name-and-level", request, TestContext.Current.CancellationToken);

        // Assert - Should return NotFound (not exposing that it exists)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompleteWorkflow_FinalizedCharacter_MatchesThePreviewAndKeepsEveryChoice()
    {
        // Arrange - a level 3 hill dwarf cleric of the Life domain, an acolyte, taken through
        // every step the wizard takes. This test used to stop before finalizing, which is how
        // race languages and skill proficiencies went missing from the sheet unnoticed.
        var cancellationToken = TestContext.Current.CancellationToken;
        await SignInAsync();
        var draftId = await CreateDraftAsync();

        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 3, Edition = RulesEdition.Edition2014 });

        // Toll the Dead, picked below, is from Xanathar's Guide
        await PutStepAsync(draftId, "books", new SetAllowedBooksApiRequest(["XGE"]));
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "dwarf", SubraceId = "hill_dwarf" });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "cleric", SubclassId = "life" });
        await PutStepAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = "acolyte" });
        await PutStepAsync(draftId, "languages", new ChooseLanguagesApiRequest(["celestial-phb", "elvish-phb"]));
        // The hill dwarf's artisan's tools
        await PutStepAsync(draftId, "tools", new ChooseToolsApiRequest([["smith_tools"]]));
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 13, ["DEX"] = 10, ["CON"] = 14, ["INT"] = 8, ["WIS"] = 15, ["CHA"] = 12 },
            Method = "standard-array"
        });
        await PutStepAsync(draftId, "proficiencies", new ChooseProficienciesApiRequest(["history", "medicine"]));
        await PutStepAsync(draftId, "spells", new ChooseSpellsApiRequest(["guidance", "sacred_flame", "toll_the_dead"]));

        var preview = await _client.GetFromJsonAsync<CharacterPreviewApiResponse>(
            $"/api/character-drafts/{draftId}/preview", cancellationToken);

        // Act
        var finalizeResponse = await _client.PostAsJsonAsync(
            "/api/characters", new CreateCharacterApiRequest(draftId), cancellationToken);
        finalizeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var characterId = await finalizeResponse.Content.ReadFromJsonAsync<Guid>(cancellationToken);
        var character = await _client.GetFromJsonAsync<CharacterDetailsApiResponse>(
            $"/api/characters/{characterId}", cancellationToken);

        // Assert - Common and Dwarvish come from the race, the other two were chosen
        preview!.BlockingIssue.Should().BeNull();
        character!.LanguageIds.Should().BeEquivalentTo(["common-phb", "dwarvish-phb", "celestial-phb", "elvish-phb"]);

        character.Proficiencies.Select(p => p.Id).Should().Contain("smith_tools");

        // Insight and Religion from the background, History and Medicine from the class
        character.Stats!.Skills.Where(s => s.IsProficient).Select(s => s.SkillId)
            .Should().BeEquivalentTo(["history", "insight", "medicine", "religion"]);

        // d8 at level 1 plus two average rolls of 5, each with CON +3 (14 + 2 from the dwarf),
        // and the hill dwarf's Dwarven Toughness adds 1 a level
        character.Stats.MaxHitPoints.Should().Be(30);

        // Features come from the class and the Life domain up to level 3, and from the background
        character.Features.Select(f => f.Name).Should().Contain(["Divine Domain", "Disciple of Life", "Shelter of the Faithful"]);

        // The three picked cantrips, and the domain spells a level 3 Life cleric has prepared
        character.Spells.Select(s => s.Id).Should().BeEquivalentTo(
            ["guidance", "sacred_flame", "toll_the_dead", "bless", "cure_wounds", "lesser_restoration", "spiritual_weapon"]);

        // The saved sheet reads back exactly what the review step showed
        character.Stats.Should().BeEquivalentTo(preview.Stats);
        character.Languages.Select(l => l.Id).Should().BeEquivalentTo(preview.Languages.Select(l => l.Id));
        character.Features.Select(f => f.Id).Should().BeEquivalentTo(preview.Features.Select(f => f.Id));
    }

    [Fact]
    public async Task ChooseLanguages_WithALanguageTheRaceAlreadyGrants_ReturnsItsCode()
    {
        // Arrange - a dwarf already speaks Dwarvish, so picking it would waste the choice
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 1, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "dwarf", SubraceId = "hill_dwarf" });
        await PutStepAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = "acolyte" });

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/languages",
            new ChooseLanguagesApiRequest(["dwarvish-phb", "elvish-phb"]),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.LanguagesAlreadyKnown");
    }

    [Fact]
    public async Task SetSubrace_WithASubraceOfTheDraftsRace_SavesIt()
    {
        // Arrange
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 1, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "dwarf" });

        // Act
        await PutStepAsync(draftId, "subrace", new SetSubraceApiRequest("hill_dwarf"));

        // Assert
        (await GetDraftAsync(draftId)).SubraceId.Should().Be("hill_dwarf");
    }

    [Fact]
    public async Task SetSubrace_WithAnotherRacesSubrace_ReturnsInvalidSubrace()
    {
        // Arrange
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 1, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "dwarf" });

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/subrace", new SetSubraceApiRequest("high_elf"), TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.InvalidSubrace");
    }

    [Fact]
    public async Task SetSubclass_AtTheLevelTheClassChoosesOne_SavesIt()
    {
        // Arrange - a cleric picks a domain at level 1
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 1, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "cleric" });

        // Act
        await PutStepAsync(draftId, "subclass", new SetSubclassApiRequest("life"));

        // Assert
        (await GetDraftAsync(draftId)).SubclassId.Should().Be("life");
    }

    [Fact]
    public async Task SetSubclass_BeforeTheClassGetsOne_ReturnsSubclassLevelRequirement()
    {
        // Arrange - a fighter only picks an archetype at level 3
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 1, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter" });

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/subclass", new SetSubclassApiRequest("champion"), TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.SubclassLevelRequirement");
    }

    [Fact]
    public async Task ChooseRaceSkills_WithAsManySkillsAsTheRaceGives_SavesThem()
    {
        // Arrange - a half-elf chooses two skills
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 1, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "half_elf", AbilityIncreases = new() { ["DEX"] = 1, ["WIS"] = 1 } });

        // Act
        await PutStepAsync(draftId, "race-skills", new ChooseRaceSkillsApiRequest(["athletics", "stealth"]));

        // Assert
        (await GetDraftAsync(draftId)).SelectedRaceSkillIds.Should().BeEquivalentTo(["athletics", "stealth"]);
    }

    [Fact]
    public async Task Finalize_WithARaceSkillTheLaterBackgroundGives_SaysSoAndThePreviewShowsIt()
    {
        // Arrange - a half-elf picks Athletics, then takes Soldier, which gives it; this used to
        // finalize with one skill fewer
        var cancellationToken = TestContext.Current.CancellationToken;
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Tanis", Level = 1, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "half_elf", AbilityIncreases = new() { ["DEX"] = 1, ["WIS"] = 1 } });
        await PutStepAsync(draftId, "race-skills", new ChooseRaceSkillsApiRequest(["athletics", "stealth"]));
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter" });
        await PutStepAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = "soldier" });
        await PutStepAsync(draftId, "languages", new ChooseLanguagesApiRequest(["dwarvish-phb"]));
        await PutStepAsync(draftId, "tools", new ChooseToolsApiRequest([["dice_set"]]));
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 15, ["DEX"] = 14, ["CON"] = 13, ["INT"] = 8, ["WIS"] = 12, ["CHA"] = 10 },
            Method = "standard-array"
        });
        await PutStepAsync(draftId, "proficiencies", new ChooseProficienciesApiRequest(["perception", "survival"]));

        // Act
        var preview = await _client.GetFromJsonAsync<CharacterPreviewApiResponse>($"/api/character-drafts/{draftId}/preview", cancellationToken);
        var response = await _client.PostAsJsonAsync("/api/characters", new CreateCharacterApiRequest(draftId), cancellationToken);

        // Assert
        preview!.BlockingIssue.Should().NotBeNullOrEmpty();
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.ProficiencyAlreadyGiven");
    }

    [Fact]
    public async Task ChooseRaceSkills_WithFewerSkillsThanTheRaceGives_ReturnsInvalidRaceSkillCount()
    {
        // Arrange
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 1, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "half_elf", AbilityIncreases = new() { ["DEX"] = 1, ["WIS"] = 1 } });

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/race-skills", new ChooseRaceSkillsApiRequest(["athletics"]), TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.InvalidRaceSkillCount");
    }

    [Fact]
    public async Task SetAbilityScoreImprovements_WithOnePerImprovementLevelReached_SavesThem()
    {
        // Arrange - a level 4 fighter has one improvement to spend
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 4, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter" });

        // Act
        await PutStepAsync(draftId, "ability-score-improvements", new SetAbilityScoreImprovementsApiRequest(
            [new Dictionary<string, int> { ["STR"] = 2 }], []));

        // Assert
        var draft = await GetDraftAsync(draftId);
        draft.AbilityScoreImprovements.Should().ContainSingle().Which.Should().Equal(new Dictionary<string, int> { ["STR"] = 2 });
    }

    [Fact]
    public async Task SetNameAndLevel_ToAnotherLevelAfterAFeatWasTaken_ClearsTheImprovementsStep()
    {
        // Arrange - a level 4 fighter spends its improvement on Alert, then goes back to step 1
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 4, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter" });
        await PutStepAsync(draftId, "ability-score-improvements", new SetAbilityScoreImprovementsApiRequest([], ["alert_2014"]));

        // Act
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 3, Edition = RulesEdition.Edition2014 });

        // Assert
        var draft = await GetDraftAsync(draftId);
        draft.SelectedFeatIds.Should().BeEmpty();
        draft.FeatAbilityIncreases.Should().BeEmpty();
        draft.AbilityScoreImprovements.Should().BeEmpty();
    }

    [Fact]
    public async Task SetAbilityScoreImprovements_WithMoreThanTheLevelGives_ReturnsInvalidAsiCount()
    {
        // Arrange
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 4, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter" });

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/ability-score-improvements",
            new SetAbilityScoreImprovementsApiRequest(
                [new Dictionary<string, int> { ["STR"] = 2 }, new Dictionary<string, int> { ["DEX"] = 2 }], []),
            TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.InvalidAsiCount");
    }

    [Fact]
    public async Task SetAbilityScoreImprovements_WithAnEpicBoonBeforeLevel19_ReturnsFeatPrerequisiteNotMet()
    {
        // Arrange - a level 4 fighter's one improvement, spent on a level 19 feat
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 4, Edition = RulesEdition.Edition2024 });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter-2024" });

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/ability-score-improvements",
            new SetAbilityScoreImprovementsApiRequest([], ["boon_of_fate"]),
            TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.FeatPrerequisiteNotMet");
    }

    [Fact]
    public async Task SetAbilityScoreImprovements_WithAFeatFromTheOtherEdition_ReturnsEditionMismatch()
    {
        // Arrange - a 2014 fighter offered the 2024 Alert
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 4, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter" });

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/ability-score-improvements",
            new SetAbilityScoreImprovementsApiRequest([], ["alert_2024"]),
            TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.EditionMismatch");
    }

    [Fact]
    public async Task SetAbilityScoreImprovements_WithAFightingStyleFeatForAFighter_SavesIt()
    {
        // Arrange - the 2024 fighter has the Fighting Style feature the feat asks for
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 4, Edition = RulesEdition.Edition2024 });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter-2024" });

        // Act
        await PutStepAsync(draftId, "ability-score-improvements", new SetAbilityScoreImprovementsApiRequest([], ["archery_2024"]));

        // Assert
        var draft = await GetDraftAsync(draftId);
        draft.SelectedFeatIds.Should().Equal("archery_2024");
    }

    [Fact]
    public async Task FightingStyles_ForA2014Paladin_OffersItsFourAndSavesThePick()
    {
        // Arrange - a level 2 paladin owes one, from the styles its class lists
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Lancelot", Level = 2, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "paladin" });

        // Act
        var styles = await _client.GetFromJsonAsync<FightingStylesApiResponse>(
            $"/api/character-drafts/{draftId}/fighting-styles", TestContext.Current.CancellationToken);
        await PutStepAsync(draftId, "fighting-styles", new ChooseFightingStylesApiRequest(["protection_2014"]));

        // Assert
        styles!.Owed.Should().Be(1);
        styles.Options.Select(o => o.Id).Should().Equal("defense_2014", "dueling_2014", "great_weapon_fighting_2014", "protection_2014");
        (await GetDraftAsync(draftId)).FightingStyleIds.Should().Equal("protection_2014");
    }

    [Fact]
    public async Task ChooseFightingStyles_WithAStyleFromABookTheDraftDoesNotAllow_ReturnsSourceNotAllowed()
    {
        // Arrange - a new 2024 fighter allows the Player's Handbook alone; Arcane Warrior is from Arcana Unleashed
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Tester", Level = 1, Edition = RulesEdition.Edition2024 });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter-2024" });

        // Act
        var styles = await _client.GetFromJsonAsync<FightingStylesApiResponse>(
            $"/api/character-drafts/{draftId}/fighting-styles", TestContext.Current.CancellationToken);
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/fighting-styles",
            new ChooseFightingStylesApiRequest(["arcane_warrior"]),
            TestContext.Current.CancellationToken);

        // Assert
        styles!.Options.Select(o => o.Id).Should().NotContain("arcane_warrior").And.Contain("defense_2024");
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.SourceNotAllowed");
    }

    [Fact]
    public async Task SetBooks_AllowingTheStylesBook_LetsTheFighterTakeItAndNarrowingThemClearsIt()
    {
        // Arrange
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Tester", Level = 1, Edition = RulesEdition.Edition2024 });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter-2024" });

        // Act
        await PutStepAsync(draftId, "books", new SetAllowedBooksApiRequest(["AU"]));
        await PutStepAsync(draftId, "fighting-styles", new ChooseFightingStylesApiRequest(["arcane_warrior"]));
        var withTheBook = await GetDraftAsync(draftId);
        await PutStepAsync(draftId, "books", new SetAllowedBooksApiRequest([]));
        var withoutIt = await GetDraftAsync(draftId);

        // Assert
        withTheBook.AllowedSources.Should().Equal("XPHB", "AU");
        withTheBook.FightingStyleIds.Should().Equal("arcane_warrior");
        withoutIt.AllowedSources.Should().Equal("XPHB");
        withoutIt.FightingStyleIds.Should().BeEmpty();
        withoutIt.ClassId.Should().Be("fighter-2024");
    }

    [Fact]
    public async Task SetBooks_WithABookOfTheOtherEdition_ReturnsUnknownBooks()
    {
        // Arrange - Xanathar's Guide is a 2014 book
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Tester", Level = 1, Edition = RulesEdition.Edition2024 });

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/books", new SetAllowedBooksApiRequest(["XGE"]), TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.UnknownBooks");
    }

    [Fact]
    public async Task ChooseFightingStyles_WithAStyleTheClassDoesNotList_ReturnsFeatPrerequisiteNotMet()
    {
        // Arrange - Archery is the 2014 fighter's and ranger's, not the paladin's
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Lancelot", Level = 2, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "paladin" });

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/fighting-styles",
            new ChooseFightingStylesApiRequest(["archery_2014"]),
            TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.FeatPrerequisiteNotMet");
    }

    [Fact]
    public async Task GetIncompleteDraft_WhenTheUserHasOne_ReturnsIt()
    {
        // Arrange
        await SignInAsync();
        var draftId = await CreateDraftAsync();

        // Act
        var draft = await _client.GetFromJsonAsync<CharacterDraftDetailsApiResponse>(
            "/api/character-drafts/incomplete", TestContext.Current.CancellationToken);

        // Assert
        draft!.Id.Should().Be(draftId);
        draft.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetDraftById_OfAnotherUser_ReturnsNotFound()
    {
        // Arrange
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await SignInAsync();

        // Act
        var response = await _client.GetAsync($"/api/character-drafts/{draftId}", TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.NotFound, "CharacterDraft.NotFound");
    }

    [Fact]
    public async Task DeleteDraft_RemovesIt()
    {
        // Arrange
        await SignInAsync();
        var draftId = await CreateDraftAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/character-drafts/{draftId}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var afterwards = await _client.GetAsync($"/api/character-drafts/{draftId}", TestContext.Current.CancellationToken);
        afterwards.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteDraft_WithAnUnknownId_ReturnsNotFound()
    {
        // Arrange
        await SignInAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/character-drafts/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.NotFound, "CharacterDraft.NotFound");
    }

    [Fact]
    public async Task Finalize_WithAChoiceStillOpen_SaysWhichAndThePreviewShowsIt()
    {
        // Arrange - everything but the acolyte's two languages
        var cancellationToken = TestContext.Current.CancellationToken;
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 3, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "dwarf", SubraceId = "hill_dwarf" });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "cleric", SubclassId = "life" });
        await PutStepAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = "acolyte" });
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 13, ["DEX"] = 10, ["CON"] = 14, ["INT"] = 8, ["WIS"] = 15, ["CHA"] = 12 },
            Method = "standard-array"
        });
        await PutStepAsync(draftId, "proficiencies", new ChooseProficienciesApiRequest(["history", "medicine"]));

        // Act
        var finalizeResponse = await _client.PostAsJsonAsync(
            "/api/characters", new CreateCharacterApiRequest(draftId), cancellationToken);
        var preview = await _client.GetFromJsonAsync<CharacterPreviewApiResponse>(
            $"/api/character-drafts/{draftId}/preview", cancellationToken);

        // Assert
        await finalizeResponse.ShouldFailWithAsync(HttpStatusCode.BadRequest, "Character.LanguageChoicesIncomplete");
        preview!.BlockingIssue.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SetNameAndLevel_WithAnEdition_SavesIt()
    {
        // Arrange
        await SignInAsync();
        var draftId = await CreateDraftAsync();

        // Act
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 1, Edition = "2024" });

        // Assert
        (await GetDraftAsync(draftId)).Edition.Should().Be("2024");
    }

    [Fact]
    public async Task SetNameAndLevel_WithAnUnknownEdition_ReturnsItsCode()
    {
        // Arrange
        await SignInAsync();
        var draftId = await CreateDraftAsync();

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/name-and-level",
            new SetNameAndLevelApiRequest { Name = "Thorin", Level = 1, Edition = "2019" },
            TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.InvalidEdition");
    }

    [Fact]
    public async Task SetClass_FromTheOtherEdition_ReturnsEditionMismatch()
    {
        // Arrange - a 2014 character offered the 2024 wizard
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 1, Edition = "2014" });

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/class", new SetClassApiRequest { ClassId = "wizard-2024" }, TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.EditionMismatch");
    }

    [Fact]
    public async Task SetNameAndLevel_SwitchingTheEdition_ClearsTheClassAndRace()
    {
        // Arrange
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 1, Edition = "2014" });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "dwarf" });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "wizard" });

        // Act
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 1, Edition = "2024" });

        // Assert
        var draft = await GetDraftAsync(draftId);
        (draft.Edition, draft.RaceId, draft.ClassId).Should().Be(("2024", null, null));
    }

    [Fact]
    public async Task CompleteWorkflow_With2024Class_FinalizesACharacterWithThatEdition()
    {
        // Arrange - a level 1 2024 fighter
        var cancellationToken = TestContext.Current.CancellationToken;
        await SignInAsync();
        var draftId = await CreateDraftAsync();

        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Bruenor", Level = 1, Edition = "2024" });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "dwarf_2024" });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter-2024" });
        await PutStepAsync(draftId, "fighting-styles", new ChooseFightingStylesApiRequest(["defense_2024"]));
        await PutStepAsync(draftId, "background", Soldier2024());
        await PutStepAsync(draftId, "languages", new ChooseLanguagesApiRequest(["draconic-xphb", "giant-xphb"]));
        await PutStepAsync(draftId, "tools", new ChooseToolsApiRequest([["dice_set"]]));
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 15, ["DEX"] = 13, ["CON"] = 14, ["INT"] = 8, ["WIS"] = 12, ["CHA"] = 10 },
            Method = "standard-array"
        });
        await PutStepAsync(draftId, "proficiencies", new ChooseProficienciesApiRequest(["acrobatics", "perception"]));

        // Act
        var finalizeResponse = await _client.PostAsJsonAsync("/api/characters", new CreateCharacterApiRequest(draftId), cancellationToken);
        finalizeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var characterId = await finalizeResponse.Content.ReadFromJsonAsync<Guid>(cancellationToken);
        var character = await _client.GetFromJsonAsync<CharacterDetailsApiResponse>($"/api/characters/{characterId}", cancellationToken);

        // Assert - the 2024 fighter's own level 1 features, not the 2014 one's
        character!.Edition.Should().Be("2024");
        character.Features.Select(f => f.Name).Should().Contain(["Fighting Style", "Second Wind", "Weapon Mastery"]);
        character.Stats!.SavingThrows.Where(s => s.IsProficient).Select(s => s.AbilityId).Should().BeEquivalentTo(["STR", "CON"]);

        // The soldier's +2 and +1 and its origin feat, the Fighting Style, and Common with the two picks
        (character.AbilityScores["STR"], character.AbilityScores["CON"]).Should().Be((17, 15));
        character.Feats.Select(f => f.Feat.Name).Should().BeEquivalentTo(["Savage Attacker", "Defense"]);
        character.LanguageIds.Should().BeEquivalentTo(["common-xphb", "draconic-xphb", "giant-xphb"]);

        // A d10 and CON 15 (+2), and 1 more from the 2024 dwarf's own Dwarven Toughness
        character.Stats.MaxHitPoints.Should().Be(10 + 2 + 1);
    }

    [Fact]
    public async Task SetBackground_With2024BackgroundAndIncreasesItDoesNotAllow_ReturnsItsCode()
    {
        // Arrange - Wisdom is not one of the 2024 soldier's three
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Bruenor", Level = 1, Edition = RulesEdition.Edition2024 });

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/background",
            new SetBackgroundApiRequest { BackgroundId = "soldier_2024", AbilityIncreases = new() { ["STR"] = 2, ["WIS"] = 1 } },
            TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.InvalidBackgroundAbilityIncreases");
    }

    [Fact]
    public async Task SetBackground_FromTheOtherEdition_ReturnsEditionMismatch()
    {
        // Arrange
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 1, Edition = RulesEdition.Edition2014 });

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/background", Soldier2024(), TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.EditionMismatch");
    }

    // The 2024 soldier with +2 Strength and +1 Constitution
    private static SetBackgroundApiRequest Soldier2024() =>
        new() { BackgroundId = "soldier_2024", AbilityIncreases = new() { ["STR"] = 2, ["CON"] = 1 } };

    [Fact]
    public async Task CompleteWorkflow_With2024Lineage_GivesTheCharacterItsSpeedSkillAndSpells()
    {
        // Arrange - a level 3 wood elf fighter, who picks Perception for Keen Senses
        var cancellationToken = TestContext.Current.CancellationToken;
        await SignInAsync();
        var draftId = await CreateDraftAsync();

        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Galinndan", Level = 3, Edition = "2024" });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "elf_2024", SubraceId = "elf_wood_elf_lineage_2024" });
        await PutStepAsync(draftId, "race-skills", new ChooseRaceSkillsApiRequest(["perception"]));
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter-2024", SubclassId = "champion-2024" });
        await PutStepAsync(draftId, "fighting-styles", new ChooseFightingStylesApiRequest(["archery_2024"]));
        await PutStepAsync(draftId, "background", Soldier2024());
        await PutStepAsync(draftId, "languages", new ChooseLanguagesApiRequest(["dwarvish-xphb", "giant-xphb"]));
        await PutStepAsync(draftId, "tools", new ChooseToolsApiRequest([["dice_set"]]));
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 15, ["DEX"] = 13, ["CON"] = 14, ["INT"] = 8, ["WIS"] = 12, ["CHA"] = 10 },
            Method = "standard-array"
        });
        // Not Athletics: the soldier gives it already
        await PutStepAsync(draftId, "proficiencies", new ChooseProficienciesApiRequest(["acrobatics", "survival"]));

        // Act
        var finalizeResponse = await _client.PostAsJsonAsync("/api/characters", new CreateCharacterApiRequest(draftId), cancellationToken);
        finalizeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var characterId = await finalizeResponse.Content.ReadFromJsonAsync<Guid>(cancellationToken);
        var character = await _client.GetFromJsonAsync<CharacterDetailsApiResponse>($"/api/characters/{characterId}", cancellationToken);

        // Assert - Druidcraft from the start and Longstrider at 3; Pass without Trace waits for 5
        character!.SubraceName.Should().Be("Wood Elf Lineage");
        character.Stats!.Speed.Should().Be(35);
        character.Stats.Skills.Single(s => s.SkillId == "perception").IsProficient.Should().BeTrue();
        character.Spells.Select(s => s.Id).Should().BeEquivalentTo(["druidcraft_2024", "longstrider_2024"]);
    }

    [Fact]
    public async Task SetRace_FromTheOtherEdition_ReturnsEditionMismatch()
    {
        // Arrange
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Thorin", Level = 1, Edition = RulesEdition.Edition2014 });

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/race", new SetRaceApiRequest { RaceId = "dwarf_2024" }, TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.EditionMismatch");
    }

    [Fact]
    public async Task SetRace_ForAHalfElfWithoutItsIncreases_ReturnsInvalidRaceAbilityIncreases()
    {
        // Arrange - a half-elf raises two abilities besides its Charisma
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Tanis", Level = 1, Edition = RulesEdition.Edition2014 });

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/race", new SetRaceApiRequest { RaceId = "half_elf" }, TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.InvalidRaceAbilityIncreases");
    }

    [Fact]
    public async Task CompleteWorkflow_WithAVariantHuman_AppliesItsPicksAndGivesItsFeat()
    {
        // Arrange - a level 1 variant human fighter: +1 Strength and Constitution, Persuasion,
        // and Athlete for the feat, raising Dexterity
        var cancellationToken = TestContext.Current.CancellationToken;
        await SignInAsync();
        var draftId = await CreateDraftAsync();

        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Caramon", Level = 1, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest
        {
            RaceId = "human",
            SubraceId = "variant_human",
            AbilityIncreases = new() { ["STR"] = 1, ["CON"] = 1 }
        });
        await PutStepAsync(draftId, "race-skills", new ChooseRaceSkillsApiRequest(["persuasion"]));
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter" });
        await PutStepAsync(draftId, "fighting-styles", new ChooseFightingStylesApiRequest(["protection_2014"]));
        await PutStepAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = "soldier" });
        await PutStepAsync(draftId, "languages", new ChooseLanguagesApiRequest(["giant-phb"]));
        await PutStepAsync(draftId, "tools", new ChooseToolsApiRequest([["dice_set"]]));
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 15, ["DEX"] = 13, ["CON"] = 14, ["INT"] = 8, ["WIS"] = 12, ["CHA"] = 10 },
            Method = "standard-array"
        });
        await PutStepAsync(draftId, "proficiencies", new ChooseProficienciesApiRequest(["acrobatics", "perception"]));
        await PutStepAsync(draftId, "ability-score-improvements", new SetAbilityScoreImprovementsApiRequest([], ["athlete_2014"], [new() { ["DEX"] = 1 }]));

        // Act
        var finalizeResponse = await _client.PostAsJsonAsync("/api/characters", new CreateCharacterApiRequest(draftId), cancellationToken);
        finalizeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var characterId = await finalizeResponse.Content.ReadFromJsonAsync<Guid>(cancellationToken);
        var character = await _client.GetFromJsonAsync<CharacterDetailsApiResponse>($"/api/characters/{characterId}", cancellationToken);

        // Assert
        character!.SubraceName.Should().Be("Variant Human");
        (character.AbilityScores["STR"], character.AbilityScores["DEX"], character.AbilityScores["CON"]).Should().Be((16, 14, 15));
        character.Feats.Select(f => f.Feat.Name).Should().BeEquivalentTo(["Protection", "Athlete"]);
        character.Stats!.Skills.Single(s => s.SkillId == "persuasion").IsProficient.Should().BeTrue();
    }

    [Fact]
    public async Task ChooseRaceSkills_OutsideTheRacesOptions_ReturnsRaceSkillsNotOffered()
    {
        // Arrange - a 2024 elf's Keen Senses is Insight, Perception or Survival
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Galinndan", Level = 1, Edition = RulesEdition.Edition2024 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "elf_2024", SubraceId = "elf_high_elf_lineage_2024" });

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/race-skills", new ChooseRaceSkillsApiRequest(["stealth"]), TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.RaceSkillsNotOffered");
    }

    [Fact]
    public async Task ChooseSpells_EldritchKnightWithWizardSpells_SavesThem()
    {
        // Arrange - the fighter has no spell list of its own; this was refused for every spell
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Durnan", Level = 3, Edition = "2014" });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter", SubclassId = "eldritch-knight" });

        // Act
        await PutStepAsync(draftId, "spells", new ChooseSpellsApiRequest(["fire_bolt", "shocking_grasp", "shield", "magic_missile", "thunderwave"]));

        // Assert
        (await GetDraftAsync(draftId)).SelectedSpellIds.Should().HaveCount(5);
    }

    [Fact]
    public async Task ChooseSpells_ALevel1WizardWithASecondLevelSpell_ReturnsLevelTooHigh()
    {
        // Arrange - the client only offers level 1 spells; a hand-made request got past the server
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Elminster", Level = 1, Edition = "2014" });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "wizard" });

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/spells",
            new ChooseSpellsApiRequest(["fire_bolt", "light", "mage_hand", "shield", "magic_missile", "sleep", "burning_hands", "detect_magic", "misty_step"]),
            TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "Spell.LevelTooHigh");
    }

    [Fact]
    public async Task GetSubclass_EldritchKnight_HasItsSpellTableAndTheWizardList()
    {
        // Act - the lookup by id left the spell table out, so the spells step saw no caster
        var subclass = await _client.GetFromJsonAsync<SubclassApiResponse>(
            "/api/subclasses/eldritch-knight", TestContext.Current.CancellationToken);

        // Assert
        subclass!.SpellListClassId.Should().Be("wizard");
        subclass.SpellProgression.Single(p => p.CharacterLevel == 3).SpellsKnown.Should().Be(3);
    }

    // Helper methods

    /// <summary>Registers and signs in a new user; every later request is theirs.</summary>
    [Fact]
    public async Task CompleteWorkflow_WithASmallHumanAcolyteAndTwoMagicInitiates_GivesTheSizeAndEverySpellPicked()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SignInAsync();
        var draftId = await CreateSmallHumanAcolyteWithTwoMagicInitiatesAsync();
        var choices = await _client.GetFromJsonAsync<List<BonusSpellChoiceApiResponse>>($"/api/character-drafts/{draftId}/bonus-spells", cancellationToken);

        // Act
        var finalizeResponse = await _client.PostAsJsonAsync("/api/characters", new CreateCharacterApiRequest(draftId), cancellationToken);
        finalizeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var characterId = await finalizeResponse.Content.ReadFromJsonAsync<Guid>(cancellationToken);
        var character = await _client.GetFromJsonAsync<CharacterDetailsApiResponse>($"/api/characters/{characterId}", cancellationToken);

        // Assert - the Acolyte's Magic Initiate offers only the cleric's list, the human's all three
        choices!.Select(c => string.Join(" ", c.Lists.Select(l => l.ClassId))).Should().Equal(
            "cleric-2024", "cleric-2024 druid-2024 wizard-2024");
        character!.Size.Should().Be("Small");
        character.Feats.Select(f => f.Feat.Name).Should().Contain("Magic Initiate");
        character.Spells.Select(s => s.Id).Should().BeEquivalentTo(
            ["guidance_2024", "light_2024", "bless_2024", "fire_bolt_2024", "mage_hand_2024", "shield_2024"]);
    }

    [Fact]
    public async Task GetCharacter_WithMagicInitiateTakenTwice_ListsItTwiceEachWithItsOwnSpells()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SignInAsync();
        var draftId = await CreateSmallHumanAcolyteWithTwoMagicInitiatesAsync();
        var finalizeResponse = await _client.PostAsJsonAsync("/api/characters", new CreateCharacterApiRequest(draftId), cancellationToken);
        finalizeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var characterId = await finalizeResponse.Content.ReadFromJsonAsync<Guid>(cancellationToken);

        // Act
        var character = await _client.GetFromJsonAsync<CharacterDetailsApiResponse>($"/api/characters/{characterId}", cancellationToken);

        // Assert - the Acolyte's Magic Initiate, then the Defense fighting style once, then the human's Origin feat
        character!.Feats.Select(f => f.Feat.Id).Should().Equal("magic_initiate_2024", "defense_2024", "magic_initiate_2024");
        character.Feats[0].Spells.Select(s => s.Id).Should().Equal("guidance_2024", "light_2024", "bless_2024");
        character.Feats[1].Spells.Should().BeEmpty();
        character.Feats[2].Spells.Select(s => s.Id).Should().Equal("fire_bolt_2024", "mage_hand_2024", "shield_2024");

        // Each Magic Initiate with the spellcasting ability picked for it, by its canonical id
        character.Feats.Select(f => f.SpellcastingAbilityId).Should().Equal("WIS", null, "INT");
    }

    [Fact]
    public async Task GetPreview_WithMagicInitiateTakenTwice_ListsItTwiceEachWithItsOwnSpells()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SignInAsync();
        var draftId = await CreateSmallHumanAcolyteWithTwoMagicInitiatesAsync();

        // Act
        var preview = await _client.GetFromJsonAsync<CharacterPreviewApiResponse>($"/api/character-drafts/{draftId}/preview", cancellationToken);

        // Assert - as the sheet lists them: the Acolyte's Magic Initiate, Defense, then the human's Origin feat
        preview!.Feats.Select(f => f.Feat.Id).Should().Equal("magic_initiate_2024", "defense_2024", "magic_initiate_2024");
        preview.Feats[0].Spells.Select(s => s.Id).Should().Equal("guidance_2024", "light_2024", "bless_2024");
        preview.Feats[1].Spells.Should().BeEmpty();
        preview.Feats[2].Spells.Select(s => s.Id).Should().Equal("fire_bolt_2024", "mage_hand_2024", "shield_2024");
        preview.Feats.Select(f => f.SpellcastingAbilityId).Should().Equal("WIS", null, "INT");
    }

    [Fact]
    public async Task CompleteWorkflow_WithAHighElf_GivesTheCharacterTheCantripPicked()
    {
        // Arrange - a level 1 2014 high elf fighter soldier, with Fire Bolt for its cantrip
        var cancellationToken = TestContext.Current.CancellationToken;
        await SignInAsync();
        var draftId = await CreateDraftAsync();

        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Laucian", Level = 1, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "elf", SubraceId = "high_elf" });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter" });
        await PutStepAsync(draftId, "fighting-styles", new ChooseFightingStylesApiRequest(["archery_2014"]));
        await PutStepAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = "soldier" });
        await PutStepAsync(draftId, "languages", new ChooseLanguagesApiRequest(["giant-phb"]));
        await PutStepAsync(draftId, "tools", new ChooseToolsApiRequest([["dice_set"]]));
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 15, ["DEX"] = 13, ["CON"] = 14, ["INT"] = 8, ["WIS"] = 12, ["CHA"] = 10 },
            Method = "standard-array"
        });
        await PutStepAsync(draftId, "proficiencies", new ChooseProficienciesApiRequest(["acrobatics", "survival"]));

        // A draft without the cantrip is refused
        var openResponse = await _client.PostAsJsonAsync("/api/characters", new CreateCharacterApiRequest(draftId), cancellationToken);
        await openResponse.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.BonusSpellCount");

        await PutStepAsync(draftId, "bonus-spells", new ChooseBonusSpellsApiRequest { RaceSpellIds = ["fire_bolt"] });

        // Act
        var finalizeResponse = await _client.PostAsJsonAsync("/api/characters", new CreateCharacterApiRequest(draftId), cancellationToken);
        finalizeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var characterId = await finalizeResponse.Content.ReadFromJsonAsync<Guid>(cancellationToken);
        var character = await _client.GetFromJsonAsync<CharacterDetailsApiResponse>($"/api/characters/{characterId}", cancellationToken);

        // Assert
        character!.Spells.Select(s => s.Id).Should().Equal("fire_bolt");
        character.Size.Should().Be("Medium");
    }

    [Fact]
    public async Task ChooseBonusSpells_WithASpellFromAnotherList_ReturnsBonusSpellNotOnList()
    {
        // Arrange - Magic Missile is not a cleric spell
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Mira", Level = 1, Edition = RulesEdition.Edition2024 });
        await PutStepAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = "acolyte_2024", AbilityIncreases = new() { ["WIS"] = 2, ["CHA"] = 1 } });

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/bonus-spells",
            new ChooseBonusSpellsApiRequest
            {
                FeatSpells = [new() { FeatId = "magic_initiate_2024", ClassId = "cleric-2024", SpellIds = ["guidance_2024", "light_2024", "magic_missile_2024"] }],
            },
            TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.BonusSpellNotOnList");
    }

    [Fact]
    public async Task Finalize_WithASmallOrMediumSpeciesAndNoSize_ReturnsSizeRequired()
    {
        // Arrange - the 2024 human of the workflow above, with no size picked
        var cancellationToken = TestContext.Current.CancellationToken;
        await SignInAsync();
        var draftId = await CreateDraftAsync();

        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Mira", Level = 1, Edition = RulesEdition.Edition2024 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "human_2024" });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter-2024" });
        await PutStepAsync(draftId, "background", Soldier2024());
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 15, ["DEX"] = 13, ["CON"] = 14, ["INT"] = 8, ["WIS"] = 12, ["CHA"] = 10 },
            Method = "standard-array"
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/characters", new CreateCharacterApiRequest(draftId), cancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "Character.SizeRequired");
    }

    [Fact]
    public async Task SetBackground_ForAMistWandererWithADarkGift_SavesTheFeat()
    {
        // Arrange - Mist Wanderer and the Dark Gifts are from RHW, which the draft allows first
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Ezra", Level = 1, Edition = RulesEdition.Edition2024 });
        await PutStepAsync(draftId, "books", new SetAllowedBooksApiRequest(["RHW"]));

        // Act
        await PutStepAsync(draftId, "background", new SetBackgroundApiRequest
        {
            BackgroundId = "mist_wanderer_2024",
            AbilityIncreases = new() { ["DEX"] = 2, ["WIS"] = 1 },
            FeatId = "mist_walker",
        });

        // Assert
        (await GetDraftAsync(draftId)).BackgroundFeatId.Should().Be("mist_walker");
    }

    [Fact]
    public async Task SetBackground_WithAFeatForABackgroundWithItsOwn_ReturnsBackgroundFeatNotChoosable()
    {
        // Arrange
        await SignInAsync();
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Bruenor", Level = 1, Edition = RulesEdition.Edition2024 });

        // Act
        var request = Soldier2024();
        request.FeatId = "alert_2024";
        var response = await _client.PutAsJsonAsync($"/api/character-drafts/{draftId}/background", request, TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.BackgroundFeatNotChoosable");
    }

    [Fact]
    public async Task ToolChoices_ForADwarfCriminalBard_ListsTheThreeAndSavesThePicks()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SignInAsync();
        var draftId = await CreateDwarfCriminalBardAsync();

        // Act
        var choices = await _client.GetFromJsonAsync<List<ToolChoiceApiResponse>>($"/api/character-drafts/{draftId}/tool-choices", cancellationToken);
        await PutStepAsync(draftId, "tools", new ChooseToolsApiRequest([["mason_tools"], ["dragonchess"], ["lute", "lyre", "flute"]]));
        var draft = await GetDraftAsync(draftId);

        // Assert
        choices!.Select(c => (c.OfferedBy, c.Count, c.Options.Count)).Should().Equal(("Dwarf", 1, 3), ("Criminal", 1, 4), ("Bard", 3, 10));
        choices![0].Options.Select(o => o.Name).Should().Equal("Brewer's Supplies", "Mason's Tools", "Smith's Tools");
        draft.ToolPicks.Should().BeEquivalentTo(new List<List<string>> { new() { "mason_tools" }, new() { "dragonchess" }, new() { "lute", "lyre", "flute" } }, o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task ChooseTools_WithAToolTheCriminalDoesNotOffer_ReturnsInvalidToolPick()
    {
        // Arrange
        await SignInAsync();
        var draftId = await CreateDwarfCriminalBardAsync();

        // Act - thieves' tools come with a Criminal; its pick is a gaming set
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/tools",
            new ChooseToolsApiRequest([["smith_tools"], ["thieves-tools"], ["lute", "lyre", "flute"]]),
            TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.InvalidToolPick");
    }

    [Fact]
    public async Task Finalize_WithAToolChoiceStillOpen_ReturnsToolChoicesIncompleteAndThePreviewShowsIt()
    {
        // Arrange - every step but the tools
        var cancellationToken = TestContext.Current.CancellationToken;
        await SignInAsync();
        var draftId = await CreateDwarfCriminalBardAsync();
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 10, ["DEX"] = 14, ["CON"] = 13, ["INT"] = 8, ["WIS"] = 12, ["CHA"] = 15 },
            Method = "standard-array"
        });
        await PutStepAsync(draftId, "proficiencies", new ChooseProficienciesApiRequest(["arcana", "history", "performance"]));

        // Act
        var finalizeResponse = await _client.PostAsJsonAsync("/api/characters", new CreateCharacterApiRequest(draftId), cancellationToken);
        var preview = await _client.GetFromJsonAsync<CharacterPreviewApiResponse>($"/api/character-drafts/{draftId}/preview", cancellationToken);

        // Assert
        await finalizeResponse.ShouldFailWithAsync(HttpStatusCode.BadRequest, "Character.ToolChoicesIncomplete");
        preview!.BlockingIssue.Should().Contain("Dwarf, Criminal, Bard");
    }

    [Fact]
    public async Task CompleteWorkflow_With2024Barbarian_ShowsUnarmoredDefenseOnThePreviewAndTheSheet()
    {
        // Arrange - a level 1 dwarf barbarian with DEX 14 (+2) and CON 13 + 1 from the soldier (+2)
        await SignInAsync();
        var draftId = await CreateDraftAsync();

        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Wulfgar", Level = 1, Edition = RulesEdition.Edition2024 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "dwarf_2024" });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "barbarian-2024" });
        await PutStepAsync(draftId, "background", Soldier2024());
        await PutStepAsync(draftId, "languages", new ChooseLanguagesApiRequest(["draconic-xphb", "giant-xphb"]));
        await PutStepAsync(draftId, "tools", new ChooseToolsApiRequest([["dice_set"]]));
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 15, ["DEX"] = 14, ["CON"] = 13, ["INT"] = 8, ["WIS"] = 12, ["CHA"] = 10 },
            Method = "standard-array"
        });
        await PutStepAsync(draftId, "proficiencies", new ChooseProficienciesApiRequest(["perception", "survival"]));

        // Act
        var (preview, sheet) = await PreviewAndFinalizeArmorClassAsync(draftId);

        // Assert - 10 + DEX + CON, where 10 + DEX would be 12
        preview.Should().Be(10 + 2 + 2);
        sheet.Should().Be(preview);
    }

    [Fact]
    public async Task CompleteWorkflow_With2014Monk_ShowsUnarmoredDefenseOnThePreviewAndTheSheet()
    {
        // Arrange - a level 1 hill dwarf monk with DEX 15 (+2) and WIS 14 + 1 from the hill dwarf (+2)
        await SignInAsync();
        var draftId = await CreateDraftAsync();

        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Kwai", Level = 1, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "dwarf", SubraceId = "hill_dwarf" });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "monk" });
        await PutStepAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = "acolyte" });
        await PutStepAsync(draftId, "languages", new ChooseLanguagesApiRequest(["celestial-phb", "elvish-phb"]));
        // The hill dwarf's artisan's tools, then the monk's artisan's tools or instrument
        await PutStepAsync(draftId, "tools", new ChooseToolsApiRequest([["smith_tools"], ["lute"]]));
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 8, ["DEX"] = 15, ["CON"] = 13, ["INT"] = 10, ["WIS"] = 14, ["CHA"] = 12 },
            Method = "standard-array"
        });
        await PutStepAsync(draftId, "proficiencies", new ChooseProficienciesApiRequest(["acrobatics", "stealth"]));

        // Act
        var (preview, sheet) = await PreviewAndFinalizeArmorClassAsync(draftId);

        // Assert - 10 + DEX + WIS
        preview.Should().Be(10 + 2 + 2);
        sheet.Should().Be(preview);
    }

    [Fact]
    public async Task CompleteWorkflow_With2014DraconicSorcerer_ShowsDraconicResilienceOnThePreviewAndTheSheet()
    {
        // Arrange - a level 1 hill dwarf draconic sorcerer with DEX 14 (+2)
        await SignInAsync();
        var draftId = await CreateDraftAsync();

        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Sora", Level = 1, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "dwarf", SubraceId = "hill_dwarf" });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "sorcerer", SubclassId = "draconic" });
        await PutStepAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = "acolyte" });
        await PutStepAsync(draftId, "languages", new ChooseLanguagesApiRequest(["celestial-phb", "elvish-phb"]));
        await PutStepAsync(draftId, "tools", new ChooseToolsApiRequest([["smith_tools"]]));
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 8, ["DEX"] = 14, ["CON"] = 13, ["INT"] = 10, ["WIS"] = 12, ["CHA"] = 15 },
            Method = "standard-array"
        });
        await PutStepAsync(draftId, "proficiencies", new ChooseProficienciesApiRequest(["arcana", "deception"]));
        await PutStepAsync(draftId, "spells", new ChooseSpellsApiRequest(
            ["fire_bolt", "light", "mage_hand", "prestidigitation", "magic_missile", "shield"]));

        // Act
        var (preview, sheet) = await PreviewAndFinalizeArmorClassAsync(draftId);

        // Assert - 13 + DEX
        preview.Should().Be(13 + 2);
        sheet.Should().Be(preview);
    }

    [Fact]
    public async Task CompleteWorkflow_With2024DraconicSorcerer_ShowsDraconicResilienceOnThePreviewAndTheSheet()
    {
        // Arrange - a level 3 dwarf draconic sorcerer, DEX 13 + 2 from the soldier (+2), CHA 15 (+2)
        await SignInAsync();
        var draftId = await CreateDraftAsync();

        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Sora", Level = 3, Edition = RulesEdition.Edition2024 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "dwarf_2024" });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "sorcerer-2024", SubclassId = "draconic-2024" });
        await PutStepAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = "soldier_2024", AbilityIncreases = new() { ["DEX"] = 2, ["CON"] = 1 } });
        await PutStepAsync(draftId, "languages", new ChooseLanguagesApiRequest(["draconic-xphb", "giant-xphb"]));
        await PutStepAsync(draftId, "tools", new ChooseToolsApiRequest([["dice_set"]]));
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 8, ["DEX"] = 13, ["CON"] = 14, ["INT"] = 10, ["WIS"] = 12, ["CHA"] = 15 },
            Method = "standard-array"
        });
        await PutStepAsync(draftId, "proficiencies", new ChooseProficienciesApiRequest(["arcana", "deception"]));
        await PutStepAsync(draftId, "spells", new ChooseSpellsApiRequest(
        [
            "fire_bolt_2024", "light_2024", "mage_hand_2024", "prestidigitation_2024",
            "magic_missile_2024", "shield_2024", "burning_hands_2024", "sleep_2024", "mirror_image_2024", "misty_step_2024"
        ]));

        // Act
        var (preview, sheet) = await PreviewAndFinalizeArmorClassAsync(draftId);

        // Assert - 10 + DEX + CHA
        preview.Should().Be(10 + 2 + 2);
        sheet.Should().Be(preview);
    }

    // The armour class the review step shows, then the one the saved sheet shows
    private async Task<(int Preview, int Sheet)> PreviewAndFinalizeArmorClassAsync(Guid draftId)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var preview = await _client.GetFromJsonAsync<CharacterPreviewApiResponse>($"/api/character-drafts/{draftId}/preview", cancellationToken);
        preview!.BlockingIssue.Should().BeNull();

        var finalizeResponse = await _client.PostAsJsonAsync("/api/characters", new CreateCharacterApiRequest(draftId), cancellationToken);
        finalizeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var characterId = await finalizeResponse.Content.ReadFromJsonAsync<Guid>(cancellationToken);
        var character = await _client.GetFromJsonAsync<CharacterDetailsApiResponse>($"/api/characters/{characterId}", cancellationToken);

        return (preview.Stats!.ArmorClass, character!.Stats!.ArmorClass);
    }

    // A level 1 hill dwarf bard with the Criminal background: a tool pick from each
    private async Task<Guid> CreateDwarfCriminalBardAsync()
    {
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Dain", Level = 1, Edition = RulesEdition.Edition2014 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "dwarf", SubraceId = "hill_dwarf" });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "bard" });
        await PutStepAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = "criminal" });

        return draftId;
    }

    [Fact]
    public async Task CompleteWorkflow_WithANobleHumanTakingSkilledTwice_KeepsBothSetsAndGivesThemAll()
    {
        // Arrange - Skilled from the Noble background, then again as the human's Origin feat
        var cancellationToken = TestContext.Current.CancellationToken;
        await SignInAsync();
        var draftId = await CreateNobleHumanFighterAsync();
        FeatProficiencyPickApiModel[] picks =
        [
            new() { FeatId = "skilled_2024", ProficiencyIds = ["arcana", "lute", "thieves-tools"] },
            new() { FeatId = "skilled_2024", ProficiencyIds = ["insight", "religion", "smith_tools"] },
        ];
        await PutStepAsync(draftId, "ability-score-improvements", new SetAbilityScoreImprovementsApiRequest([], ["skilled_2024"], [[]], [.. picks]));

        // Act
        var draft = await GetDraftAsync(draftId);
        var finalizeResponse = await _client.PostAsJsonAsync("/api/characters", new CreateCharacterApiRequest(draftId), cancellationToken);
        finalizeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var characterId = await finalizeResponse.Content.ReadFromJsonAsync<Guid>(cancellationToken);
        var character = await _client.GetFromJsonAsync<CharacterDetailsApiResponse>($"/api/characters/{characterId}", cancellationToken);

        // Assert - the draft keeps the two sets in order, and the sheet has every one
        draft.FeatProficiencyPicks.Should().BeEquivalentTo(picks, options => options.WithStrictOrdering());
        character!.Proficiencies.Select(p => p.Id).Should().Contain(
            ["arcana", "lute", "thieves-tools", "insight", "religion", "smith_tools", "history", "persuasion"]);
        character.Stats!.Skills.Single(s => s.SkillId == "arcana").IsProficient.Should().BeTrue();
    }

    [Fact]
    public async Task Finalize_WithTheNoblesSkilledPicksOpen_ReturnsFeatProficiencyPicksIncompleteAndThePreviewShowsIt()
    {
        // Arrange - the Noble's Skilled picked, then the background changed to a Charlatan, whose
        // Skilled is another feat to pick for; the human's Origin feat is Alert
        var cancellationToken = TestContext.Current.CancellationToken;
        await SignInAsync();
        var draftId = await CreateNobleHumanFighterAsync();
        await PutStepAsync(draftId, "ability-score-improvements", new SetAbilityScoreImprovementsApiRequest(
            [], ["alert_2024"], [[]], [new() { FeatId = "skilled_2024", ProficiencyIds = ["arcana", "lute", "insight"] }]));
        await PutStepAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = "charlatan_2024", AbilityIncreases = new() { ["DEX"] = 2, ["CHA"] = 1 } });
        await PutStepAsync(draftId, "languages", new ChooseLanguagesApiRequest(["draconic-xphb", "giant-xphb"]));

        // Act
        var finalizeResponse = await _client.PostAsJsonAsync("/api/characters", new CreateCharacterApiRequest(draftId), cancellationToken);
        var preview = await _client.GetFromJsonAsync<CharacterPreviewApiResponse>($"/api/character-drafts/{draftId}/preview", cancellationToken);

        // Assert
        await finalizeResponse.ShouldFailWithAsync(HttpStatusCode.BadRequest, "Character.FeatProficiencyPicksIncomplete");
        preview!.BlockingIssue.Should().Contain("Skilled");
    }

    [Fact]
    public async Task SetAbilityScoreImprovements_WithAFeatPickTheNobleGives_ReturnsProficiencyAlreadyGiven()
    {
        // Arrange - History comes with the Noble background
        await SignInAsync();
        var draftId = await CreateNobleHumanFighterAsync();

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/ability-score-improvements",
            new SetAbilityScoreImprovementsApiRequest(
                [], ["alert_2024"], [[]], [new() { FeatId = "skilled_2024", ProficiencyIds = ["arcana", "history", "lute"] }]),
            TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.ProficiencyAlreadyGiven");
    }

    [Fact]
    public async Task ChooseBonusSpells_ForThe2024MagicInitiateWithoutItsAbility_ReturnsInvalidSpellcastingAbility()
    {
        // Arrange - the Acolyte's Magic Initiate on the cleric list, its ability not picked
        await SignInAsync();
        var draftId = await CreateSmallHumanAcolyteWithTwoMagicInitiatesAsync();

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/bonus-spells",
            new ChooseBonusSpellsApiRequest
            {
                FeatSpells =
                [
                    new() { FeatId = "magic_initiate_2024", ClassId = "cleric-2024", SpellIds = ["guidance_2024", "light_2024", "bless_2024"] },
                    new() { FeatId = "magic_initiate_2024", ClassId = "wizard-2024", SpellIds = ["fire_bolt_2024", "mage_hand_2024", "shield_2024"], AbilityId = "INT" },
                ],
            },
            TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "CharacterDraft.InvalidSpellcastingAbility");
    }

    // A level 1 2024 human fighter Noble with every step done but the improvements: the Noble's
    // Skilled and the human's Origin feat are left to pick
    private async Task<Guid> CreateNobleHumanFighterAsync()
    {
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Aurelia", Level = 1, Edition = RulesEdition.Edition2024 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "human_2024", Size = "Medium" });
        await PutStepAsync(draftId, "race-skills", new ChooseRaceSkillsApiRequest(["stealth"]));
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter-2024" });
        await PutStepAsync(draftId, "fighting-styles", new ChooseFightingStylesApiRequest(["defense_2024"]));
        await PutStepAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = "noble_2024", AbilityIncreases = new() { ["STR"] = 2, ["CHA"] = 1 } });
        await PutStepAsync(draftId, "languages", new ChooseLanguagesApiRequest(["draconic-xphb", "giant-xphb"]));
        await PutStepAsync(draftId, "tools", new ChooseToolsApiRequest([["dice_set"]]));
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 15, ["DEX"] = 13, ["CON"] = 14, ["INT"] = 8, ["WIS"] = 12, ["CHA"] = 10 },
            Method = "standard-array"
        });
        await PutStepAsync(draftId, "proficiencies", new ChooseProficienciesApiRequest(["acrobatics", "perception"]));

        return draftId;
    }

    // A level 1 2024 human fighter, Small, ready to finalize: Magic Initiate twice, the Acolyte's
    // on the cleric list and the human's Origin feat on the wizard list, and Defense
    private async Task<Guid> CreateSmallHumanAcolyteWithTwoMagicInitiatesAsync()
    {
        var draftId = await CreateDraftAsync();
        await PutStepAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = "Mira", Level = 1, Edition = RulesEdition.Edition2024 });
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "human_2024", Size = "Small" });
        await PutStepAsync(draftId, "race-skills", new ChooseRaceSkillsApiRequest(["stealth"]));
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "fighter-2024" });
        await PutStepAsync(draftId, "fighting-styles", new ChooseFightingStylesApiRequest(["defense_2024"]));
        await PutStepAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = "acolyte_2024", AbilityIncreases = new() { ["WIS"] = 2, ["CHA"] = 1 } });
        await PutStepAsync(draftId, "languages", new ChooseLanguagesApiRequest(["draconic-xphb", "giant-xphb"]));
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 15, ["DEX"] = 13, ["CON"] = 14, ["INT"] = 8, ["WIS"] = 12, ["CHA"] = 10 },
            Method = "standard-array"
        });
        await PutStepAsync(draftId, "proficiencies", new ChooseProficienciesApiRequest(["acrobatics", "perception"]));
        await PutStepAsync(draftId, "ability-score-improvements", new SetAbilityScoreImprovementsApiRequest([], ["magic_initiate_2024"], [[]]));
        await PutStepAsync(draftId, "bonus-spells", new ChooseBonusSpellsApiRequest
        {
            FeatSpells =
            [
                new() { FeatId = "magic_initiate_2024", ClassId = "cleric-2024", SpellIds = ["guidance_2024", "light_2024", "bless_2024"], AbilityId = "WIS" },
                new() { FeatId = "magic_initiate_2024", ClassId = "wizard-2024", SpellIds = ["fire_bolt_2024", "mage_hand_2024", "shield_2024"], AbilityId = "intelligence" },
            ],
        });

        return draftId;
    }

    private async Task SignInAsync()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<CharacterDraftDetailsApiResponse> GetDraftAsync(Guid draftId)
    {
        var draft = await _client.GetFromJsonAsync<CharacterDraftDetailsApiResponse>(
            $"/api/character-drafts/{draftId}", TestContext.Current.CancellationToken);

        return draft!;
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var email = $"test-{Guid.NewGuid()}@example.com";
        var password = "Test123!@#";

        var registerRequest = new RegisterUserRequest
        {
            Email = email,
            UserName = $"user{Guid.NewGuid():N}",
            Password = password,
            DisplayName = "Test User"
        };
        await _client.PostAsJsonAsync("/api/users/register", registerRequest);

        var loginRequest = new LoginUserRequest(email, password);

        var loginResponse = await _client.PostAsJsonAsync("/api/users/login", loginRequest);
        var result = await loginResponse.Content.ReadFromJsonAsync<LoginApiResponse>();

        return result!.Token;
    }

    private async Task<Guid> CreateDraftAsync()
    {
        var response = await _client.PostAsync("/api/character-drafts", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task PutStepAsync<TRequest>(Guid draftId, string step, TRequest request)
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/character-drafts/{draftId}/{step}", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, $"the {step} step should be accepted");
    }
}