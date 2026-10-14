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
            }
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
            }
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
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/character-drafts/{draftId}/ability-scores", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
        await PutStepAsync(draftId, "race", new SetRaceApiRequest { RaceId = "dwarf", SubraceId = "hill_dwarf" });
        await PutStepAsync(draftId, "class", new SetClassApiRequest { ClassId = "cleric", SubclassId = "life" });
        await PutStepAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = "acolyte" });
        await PutStepAsync(draftId, "languages", new ChooseLanguagesApiRequest(["celestial-phb", "elvish-phb"]));
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 13, ["DEX"] = 10, ["CON"] = 14, ["INT"] = 8, ["WIS"] = 15, ["CHA"] = 12 }
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
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 15, ["DEX"] = 14, ["CON"] = 13, ["INT"] = 8, ["WIS"] = 12, ["CHA"] = 10 }
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
        await PutStepAsync(draftId, "ability-score-improvements", new SetAbilityScoreImprovementsApiRequest([], ["archery"]));

        // Assert
        var draft = await GetDraftAsync(draftId);
        draft.SelectedFeatIds.Should().Equal("archery");
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
            Scores = new Dictionary<string, int> { ["STR"] = 13, ["DEX"] = 10, ["CON"] = 14, ["INT"] = 8, ["WIS"] = 15, ["CHA"] = 12 }
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
        await PutStepAsync(draftId, "background", Soldier2024());
        await PutStepAsync(draftId, "languages", new ChooseLanguagesApiRequest(["draconic-xphb", "giant-xphb"]));
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 15, ["DEX"] = 13, ["CON"] = 14, ["INT"] = 8, ["WIS"] = 12, ["CHA"] = 10 }
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

        // The soldier's +2 and +1 and its origin feat, and Common with the two picks
        (character.AbilityScores["STR"], character.AbilityScores["CON"]).Should().Be((17, 15));
        character.Feats.Select(f => f.Name).Should().Equal("Savage Attacker");
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
        await PutStepAsync(draftId, "background", Soldier2024());
        await PutStepAsync(draftId, "languages", new ChooseLanguagesApiRequest(["dwarvish-xphb", "giant-xphb"]));
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 15, ["DEX"] = 13, ["CON"] = 14, ["INT"] = 8, ["WIS"] = 12, ["CHA"] = 10 }
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
        await PutStepAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = "soldier" });
        await PutStepAsync(draftId, "languages", new ChooseLanguagesApiRequest(["giant-phb"]));
        await PutStepAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest
        {
            Scores = new Dictionary<string, int> { ["STR"] = 15, ["DEX"] = 13, ["CON"] = 14, ["INT"] = 8, ["WIS"] = 12, ["CHA"] = 10 }
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
        character.Feats.Select(f => f.Name).Should().Equal("Athlete");
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