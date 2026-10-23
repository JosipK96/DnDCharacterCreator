using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

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
    private async Task<List<Guid>> SeedCharactersAsync(string email, params string[] names)
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
                userId, name, level: 5, edition: RulesEdition.Edition2014, raceId: "elf", subraceId: null, classId: "wizard",
                subclassId: null, backgroundId: "sage", StandardArray(), _timeProvider);
        }).ToList();

        db.Characters.AddRange(characters);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return characters.ConvertAll(c => c.Id);
    }

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
