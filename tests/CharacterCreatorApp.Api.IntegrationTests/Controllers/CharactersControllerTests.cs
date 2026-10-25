using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

using CharacterCreatorApp.Api.IntegrationTests.Fixtures;
using CharacterCreatorApp.Contracts.Common;
using CharacterCreatorApp.Contracts.Requests.Character;
using CharacterCreatorApp.Contracts.Requests.User;
using CharacterCreatorApp.Contracts.Responses;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.ValueObjects;
using CharacterCreatorApp.Infrastructure;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

using UglyToad.PdfPig;

namespace CharacterCreatorApp.Api.IntegrationTests.Controllers;

[Collection(SharedApiHost.Name)]
public class CharactersControllerTests : IAsyncLifetime
{
    private const string GetListUrl = "/api/characters/get-list";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero));

    public CharactersControllerTests(CustomWebApplicationFactory factory)
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
    public async Task GetMyCharacters_WithNoSortOrPage_ReturnsTheDefaultFirstPage()
    {
        // Arrange
        var email = await AuthenticateAsync();
        await SeedCharactersAsync(email, "Aragorn", "Boromir");

        // Act
        var response = await _client.PostAsJsonAsync(
            GetListUrl, new FilterRequest<GetCharactersApiRequest>(), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await ReadPageAsync(response);
        page.TotalCount.Should().Be(2);
        page.PageIndex.Should().Be(0);
        page.PageSize.Should().Be(10);
        page.Items.Select(c => c.Name).Should().BeEquivalentTo(["Aragorn", "Boromir"]);
    }

    [Fact]
    public async Task GetMyCharacters_WithASortAndPage_ReturnsThatPageInThatOrder()
    {
        // Arrange - the sort and page have to survive FilterRequest -> FilterOptions on the way
        // in, and PagedResult -> FilterResponse on the way out. Created in name order, so the
        // default newest-first sort would put Aragorn on page 1, not Celeborn.
        var email = await AuthenticateAsync();
        await SeedCharactersAsync(email, "Aragorn", "Boromir", "Celeborn");

        var request = new FilterRequest<GetCharactersApiRequest>
        {
            Sorting = new SortingParams { SortColumn = "name", SortDirection = "ASC" },
            Pagination = new PaginationParams { PageIndex = 1, PageSize = 2 }
        };

        // Act
        var response = await _client.PostAsJsonAsync(GetListUrl, request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await ReadPageAsync(response);
        page.TotalCount.Should().Be(3);
        page.PageIndex.Should().Be(1);
        page.PageSize.Should().Be(2);
        page.Items.Select(c => c.Name).Should().Equal("Celeborn");
    }

    [Fact]
    public async Task GetCharacter_OfAnotherUser_ReturnsNotFound()
    {
        // Arrange
        var email = await AuthenticateAsync();
        var characterId = (await SeedCharactersAsync(email, "Aragorn")).Single();
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync($"/api/characters/{characterId}", TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.NotFound, "Character.NotFound");
    }

    [Fact]
    public async Task GetCharacterSheetPdf_OfTheirCharacter_ReturnsAPdfWithItsNameClassAndAbilityScores()
    {
        // Arrange - a level 5 wizard with STR 15
        var email = await AuthenticateAsync();
        var characterId = (await SeedCharactersAsync(email, "Aragorn")).Single();

        // Act
        var response = await _client.GetAsync($"/api/characters/{characterId}/sheet.pdf", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        response.Content.Headers.ContentDisposition!.FileName.Should().Be("Aragorn.pdf");

        var pdf = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        Encoding.ASCII.GetString(pdf, 0, 4).Should().Be("%PDF");

        using var document = PdfDocument.Open(pdf);
        var firstPage = document.GetPage(1).GetWords().Select(w => w.Text).ToList();
        // PdfPig reads words in drawing order, not reading order, so only that they are there
        firstPage.Should().Contain(["Aragorn", "Wizard", "5", "STRENGTH", "15"]);
    }

    [Fact]
    public async Task GetCharacterSheetPdf_OfALevelTenWizard_DrawsItsSlotsAndRunsItsSpellsOntoMorePages()
    {
        // Arrange
        var email = await AuthenticateAsync();
        var characterId = await SeedCharacterAsync(email, userId =>
        {
            var wizard = Character.Create(
                userId, "Elminster", 10, edition: RulesEdition.Edition2014, raceId: "elf", subraceId: null, classId: "wizard",
                subclassId: "evocation", backgroundId: "sage", StandardArray(), _timeProvider);
            wizard.SetSpells(LevelTenWizardSpells);
            return wizard;
        });

        // Act
        var response = await _client.GetAsync($"/api/characters/{characterId}/sheet.pdf?paper=letter", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = PdfDocument.Open(await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));

        document.NumberOfPages.Should().BeGreaterThan(2);
        ((int)document.GetPage(1).Height).Should().Be(792, "a Letter page is 11 inches tall");

        var laterPages = string.Join(' ', Enumerable.Range(2, document.NumberOfPages - 1)
            .SelectMany(n => document.GetPage(n).GetWords().Select(w => w.Text)));
        laterPages.Should().Contain("Evocation Savant").And.Contain("Cone of Cold").And.Contain("Fire Bolt");
        laterPages.Should().Contain("1st level: 4").And.Contain("5th level: 2");
    }

    [Fact]
    public async Task GetCharacterSheetPdf_OfAnotherUser_ReturnsNotFound()
    {
        // Arrange
        var email = await AuthenticateAsync();
        var characterId = (await SeedCharactersAsync(email, "Aragorn")).Single();
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync($"/api/characters/{characterId}/sheet.pdf", TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.NotFound, "Character.NotFound");
    }

    [Fact]
    public async Task DeleteCharacter_RemovesIt()
    {
        // Arrange
        var email = await AuthenticateAsync();
        var characterId = (await SeedCharactersAsync(email, "Aragorn")).Single();

        // Act
        var response = await _client.DeleteAsync($"/api/characters/{characterId}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var afterwards = await _client.GetAsync($"/api/characters/{characterId}", TestContext.Current.CancellationToken);
        afterwards.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCharacter_WithAnUnknownId_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/characters/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.NotFound, "Character.NotFound");
    }

    [Fact]
    public async Task EditCharacter_WithoutABuild_OpensWhatItStoresAndMarksTheRest()
    {
        // Arrange - a character written straight to the database has no draft, like one saved
        // before the drafts were linked to their characters
        var email = await AuthenticateAsync();
        var characterId = (await SeedCharactersAsync(email, "Aragorn")).Single();

        // Act
        var response = await _client.PostAsync($"/api/characters/{characterId}/edit", null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var draftId = await response.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);
        var draft = await _client.GetFromJsonAsync<CharacterDraftDetailsApiResponse>(
            $"/api/character-drafts/{draftId}", TestContext.Current.CancellationToken);

        draft!.CharacterId.Should().Be(characterId);
        (draft.Name, draft.Level, draft.Edition, draft.RaceId, draft.ClassId, draft.BackgroundId)
            .Should().Be(("Aragorn", 5, "2014", "elf", "wizard", "sage"));
        draft.AllowedSources.Should().Contain("PHB");
        draft.BaseAbilityScores.Should().BeEmpty();
        draft.ChoicesNeeded.Should().Equal(DraftChoice.NotKeptByCharacter);
    }

    [Fact]
    public async Task EditCharacter_OfAnotherUser_ReturnsNotFound()
    {
        // Arrange
        var email = await AuthenticateAsync();
        var characterId = (await SeedCharactersAsync(email, "Aragorn")).Single();
        await AuthenticateAsync();

        // Act
        var response = await _client.PostAsync($"/api/characters/{characterId}/edit", null, TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.NotFound, "Character.NotFound");
    }

    [Fact]
    public async Task LevelUpCharacter_WithoutABuild_OpensItOneLevelUpWithTheStepsItCouldNotFillInMarked()
    {
        // Arrange - a level 5 wizard written straight to the database, with no draft
        var email = await AuthenticateAsync();
        var characterId = (await SeedCharactersAsync(email, "Aragorn")).Single();

        // Act
        var response = await _client.PostAsync($"/api/characters/{characterId}/level-up", null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var draftId = await response.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);
        var draft = await _client.GetFromJsonAsync<CharacterDraftDetailsApiResponse>(
            $"/api/character-drafts/{draftId}", TestContext.Current.CancellationToken);

        (draft!.CharacterId, draft.Level).Should().Be((characterId, 6));
        draft.LevelUpChoices.Should().StartWith(DraftChoice.HitPoints);
        draft.LevelUpChoices.Should().Contain(DraftChoice.NotKeptByCharacter);
        draft.ChoicesNeeded.Should().BeEquivalentTo(draft.LevelUpChoices);
    }

    [Fact]
    public async Task LevelUpCharacter_AtTheHighestLevel_ReturnsAtMaxLevel()
    {
        // Arrange
        var email = await AuthenticateAsync();
        var characterId = (await SeedCharactersAsync(email, CharacterDraft.MaxLevel, "Elminster")).Single();

        // Act
        var response = await _client.PostAsync($"/api/characters/{characterId}/level-up", null, TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.BadRequest, "Character.AtMaxLevel");
    }

    [Fact]
    public async Task LevelUpCharacter_OfAnotherUser_ReturnsNotFound()
    {
        // Arrange
        var email = await AuthenticateAsync();
        var characterId = (await SeedCharactersAsync(email, "Aragorn")).Single();
        await AuthenticateAsync();

        // Act
        var response = await _client.PostAsync($"/api/characters/{characterId}/level-up", null, TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldFailWithAsync(HttpStatusCode.NotFound, "Character.NotFound");
    }

    private async Task<string> AuthenticateAsync()
    {
        var email = $"test-{Guid.NewGuid()}@example.com";
        const string password = "Test123!@#";

        var registerResponse = await _client.PostAsJsonAsync("/api/users/register", new RegisterUserRequest
        {
            Email = email,
            UserName = $"user{Guid.NewGuid():N}",
            Password = password,
            DisplayName = "Test User"
        }, TestContext.Current.CancellationToken);
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/users/login", new LoginUserRequest(email, password), TestContext.Current.CancellationToken);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginApiResponse>(TestContext.Current.CancellationToken);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        return email;
    }

    // Finalizing a draft through the API takes a dozen calls; these tests only need characters
    // to exist, so they are written straight to the database.
    private Task<List<Guid>> SeedCharactersAsync(string email, params string[] names) => SeedCharactersAsync(email, 5, names);

    private async Task<List<Guid>> SeedCharactersAsync(string email, int level, params string[] names)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var userId = await db.Users
            .Where(u => u.Email == email)
            .Select(u => u.Id)
            .SingleAsync(TestContext.Current.CancellationToken);

        var characters = names.Select(name =>
        {
            // A minute apart, so the default sort on CreatedAt has an order to follow
            _timeProvider.Advance(TimeSpan.FromMinutes(1));
            return Character.Create(
                userId, name, level, edition: RulesEdition.Edition2014, raceId: "elf", subraceId: null, classId: "wizard",
                subclassId: null, backgroundId: "sage", StandardArray(), _timeProvider);
        }).ToList();

        db.Characters.AddRange(characters);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return characters.ConvertAll(c => c.Id);
    }

    private async Task<Guid> SeedCharacterAsync(string email, Func<Guid, Character> create)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var userId = await db.Users
            .Where(u => u.Email == email)
            .Select(u => u.Id)
            .SingleAsync(TestContext.Current.CancellationToken);

        var character = create(userId);
        db.Characters.Add(character);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return character.Id;
    }

    // Cantrips and spells of each level a level 10 wizard casts, from the 2014 Player's Handbook
    private static readonly string[] LevelTenWizardSpells =
    [
        "acid_splash", "fire_bolt", "chill_touch", "dancing_lights",
        "alarm", "burning_hands", "charm_person", "chromatic_orb", "color_spray", "comprehend_languages",
        "melfs_acid_arrow", "alter_self", "arcane_lock", "augury",
        "animate_dead", "bestow_curse", "blink", "counterspell", "dispel_magic",
        "arcane_eye", "banishment", "evards_black_tentacles", "confusion",
        "animate_objects", "bigbys_hand", "cloudkill", "cone_of_cold",
    ];

    private static async Task<FilterResponse<CharacterSummaryApiResponse>> ReadPageAsync(HttpResponseMessage response)
    {
        var page = await response.Content.ReadFromJsonAsync<FilterResponse<CharacterSummaryApiResponse>>(
            TestContext.Current.CancellationToken);

        page.Should().NotBeNull();
        return page!;
    }

    private static Dictionary<string, int> StandardArray() => new()
    {
        ["STR"] = 15,
        ["DEX"] = 14,
        ["CON"] = 13,
        ["INT"] = 12,
        ["WIS"] = 10,
        ["CHA"] = 8
    };
}
