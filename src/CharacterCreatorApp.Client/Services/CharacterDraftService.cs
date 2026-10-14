using System.Net.Http.Json;

using CharacterCreatorApp.Client.Services.Http;
using CharacterCreatorApp.Contracts.Requests.CharacterDrafts;
using CharacterCreatorApp.Contracts.Responses;

namespace CharacterCreatorApp.Client.Services;

/// <summary>
/// The character creation wizard's calls. Every failure throws an <see cref="ApiException"/>
/// whose message is the API's own, so a step can show it without translating status codes.
/// </summary>
public class CharacterDraftService(HttpClient http)
{
    private readonly HttpClient _http = http;

    public async Task<Guid> CreateDraftAsync()
    {
        var response = await _http.PostAsync("/api/character-drafts", null);
        return await response.ReadAsync<Guid>();
    }

    public Task SetNameAndLevelAsync(Guid draftId, string name, int level, string edition) =>
        PutAsync(draftId, "name-and-level", new SetNameAndLevelApiRequest { Name = name, Level = level, Edition = edition });

    /// <summary>The books besides the edition's Player's Handbook; choices from a book left out are cleared.</summary>
    public Task SetAllowedBooksAsync(Guid draftId, List<string> books) =>
        PutAsync(draftId, "books", new SetAllowedBooksApiRequest(books));

    /// <summary>Sets the race and subrace, with the +1s picked where they offer them ({"DEX": 1, "WIS": 1}).</summary>
    public Task SetRaceAsync(Guid draftId, string raceId, string? subraceId, Dictionary<string, int>? abilityIncreases = null) =>
        PutAsync(draftId, "race", new SetRaceApiRequest { RaceId = raceId, SubraceId = subraceId, AbilityIncreases = abilityIncreases });

    public Task SetClassAsync(Guid draftId, string classId, string? subclassId) =>
        PutAsync(draftId, "class", new SetClassApiRequest { ClassId = classId, SubclassId = subclassId });

    /// <summary>Sets the background, with a 2024 one's ability increases ({"WIS": 2, "CHA": 1}).</summary>
    public Task SetBackgroundAsync(Guid draftId, string backgroundId, Dictionary<string, int>? abilityIncreases = null) =>
        PutAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = backgroundId, AbilityIncreases = abilityIncreases });

    public Task SetAbilityScoresAsync(Guid draftId, Dictionary<string, int> scores, string method) =>
        PutAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest { Scores = scores, Method = method });

    public Task ChooseProficienciesAsync(Guid draftId, List<string> proficiencyIds) =>
        PutAsync(draftId, "proficiencies", new ChooseProficienciesApiRequest(proficiencyIds));

    public Task ChooseRaceSkillsAsync(Guid draftId, List<string> proficiencyIds) =>
        PutAsync(draftId, "race-skills", new ChooseRaceSkillsApiRequest(proficiencyIds));

    public Task ChooseLanguagesAsync(Guid draftId, List<string> languageIds) =>
        PutAsync(draftId, "languages", new ChooseLanguagesApiRequest(languageIds));

    /// <summary>
    /// Sets the improvements and the feats, the race's own feats first, with what each feat raises
    /// in the same order ({"DEX": 1} for an Athlete who chose Dexterity, {} for any other).
    /// </summary>
    public Task SetAbilityScoreImprovementsAsync(
        Guid draftId,
        List<Dictionary<string, int>> improvements,
        List<string> featIds,
        List<Dictionary<string, int>> featAbilityIncreases) =>
        PutAsync(draftId, "ability-score-improvements", new SetAbilityScoreImprovementsApiRequest(improvements, featIds, featAbilityIncreases));

    /// <summary>How many Fighting Styles the character picks (0 for most classes) and its class's options.</summary>
    public async Task<FightingStylesApiResponse> GetFightingStylesAsync(Guid draftId)
    {
        var response = await _http.GetAsync($"/api/character-drafts/{draftId}/fighting-styles");
        return await response.ReadAsync<FightingStylesApiResponse>();
    }

    public Task ChooseFightingStylesAsync(Guid draftId, List<string> featIds) =>
        PutAsync(draftId, "fighting-styles", new ChooseFightingStylesApiRequest(featIds));

    public Task ChooseSpellsAsync(Guid draftId, List<string> spellIds) =>
        PutAsync(draftId, "spells", new ChooseSpellsApiRequest(spellIds));

    /// <summary>The user's unfinished draft, or null when there is none to resume.</summary>
    public async Task<CharacterDraftDetailsApiResponse?> GetIncompleteDraftAsync()
    {
        var response = await _http.GetAsync("/api/character-drafts/incomplete");
        return await response.ReadOrNullIfNotFoundAsync<CharacterDraftDetailsApiResponse>();
    }

    public async Task DeleteDraftAsync(Guid draftId)
    {
        var response = await _http.DeleteAsync($"/api/character-drafts/{draftId}");
        await response.EnsureSuccessAsync();
    }

    /// <summary>
    /// The character this draft would produce, so it can be reviewed before finalizing. A draft
    /// that is not ready throws with the API's explanation of what is still missing.
    /// </summary>
    public async Task<CharacterPreviewApiResponse> GetPreviewAsync(Guid draftId)
    {
        var response = await _http.GetAsync($"/api/character-drafts/{draftId}/preview");
        return await response.ReadAsync<CharacterPreviewApiResponse>();
    }

    /// <summary>The draft, or null if it does not exist or belongs to someone else.</summary>
    public async Task<CharacterDraftDetailsApiResponse?> GetDraftDetailsAsync(Guid draftId)
    {
        var response = await _http.GetAsync($"/api/character-drafts/{draftId}");
        return await response.ReadOrNullIfNotFoundAsync<CharacterDraftDetailsApiResponse>();
    }

    private async Task PutAsync<TRequest>(Guid draftId, string step, TRequest request)
    {
        var response = await _http.PutAsJsonAsync($"/api/character-drafts/{draftId}/{step}", request);
        await response.EnsureSuccessAsync();
    }
}
