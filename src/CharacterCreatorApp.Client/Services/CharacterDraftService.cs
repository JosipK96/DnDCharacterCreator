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

    /// <summary>
    /// Sets the race and subrace, with the +1s picked where they offer them ({"DEX": 1, "WIS": 1})
    /// and the size picked for a species that may be Small or Medium.
    /// </summary>
    public Task SetRaceAsync(Guid draftId, string raceId, string? subraceId, Dictionary<string, int>? abilityIncreases = null, string? size = null) =>
        PutAsync(draftId, "race", new SetRaceApiRequest { RaceId = raceId, SubraceId = subraceId, AbilityIncreases = abilityIncreases, Size = size });

    public Task SetClassAsync(Guid draftId, string classId, string? subclassId) =>
        PutAsync(draftId, "class", new SetClassApiRequest { ClassId = classId, SubclassId = subclassId });

    /// <summary>The hit die's result for the level a level-up reaches: rolled, or the die's fixed value.</summary>
    public Task SetHitPointsAsync(Guid draftId, int result) =>
        PutAsync(draftId, "hit-points", new SetHitPointsApiRequest(result));

    /// <summary>
    /// Sets the background, with a 2024 one's ability increases ({"WIS": 2, "CHA": 1}) and the feat
    /// picked for one that lets the player choose it (a Mist Wanderer's Dark Gift).
    /// </summary>
    public Task SetBackgroundAsync(Guid draftId, string backgroundId, Dictionary<string, int>? abilityIncreases = null, string? featId = null) =>
        PutAsync(draftId, "background", new SetBackgroundApiRequest { BackgroundId = backgroundId, AbilityIncreases = abilityIncreases, FeatId = featId });

    public Task SetAbilityScoresAsync(Guid draftId, Dictionary<string, int> scores, string method) =>
        PutAsync(draftId, "ability-scores", new SetAbilityScoresApiRequest { Scores = scores, Method = method });

    public Task ChooseProficienciesAsync(Guid draftId, List<string> proficiencyIds) =>
        PutAsync(draftId, "proficiencies", new ChooseProficienciesApiRequest(proficiencyIds));

    public Task ChooseRaceSkillsAsync(Guid draftId, List<string> proficiencyIds) =>
        PutAsync(draftId, "race-skills", new ChooseRaceSkillsApiRequest(proficiencyIds));

    /// <summary>The tool choices the race, background and class offer, in the order ChooseToolsAsync takes the picks.</summary>
    public async Task<List<ToolChoiceApiResponse>> GetToolChoicesAsync(Guid draftId)
    {
        var response = await _http.GetAsync($"/api/character-drafts/{draftId}/tool-choices");
        return await response.ReadAsync<List<ToolChoiceApiResponse>>();
    }

    /// <summary>One list of tools per tool choice, in the order GetToolChoicesAsync lists them.</summary>
    public Task ChooseToolsAsync(Guid draftId, List<List<string>> picks) =>
        PutAsync(draftId, "tools", new ChooseToolsApiRequest(picks));

    public Task ChooseLanguagesAsync(Guid draftId, List<string> languageIds) =>
        PutAsync(draftId, "languages", new ChooseLanguagesApiRequest(languageIds));

    /// <summary>
    /// Sets the improvements and the feats, the race's own feats first, with what each feat raises
    /// in the same order ({"DEX": 1} for an Athlete who chose Dexterity, {} for any other), and the
    /// skills or tools picked for each feat that asks for them, the background's feat first.
    /// </summary>
    public Task SetAbilityScoreImprovementsAsync(
        Guid draftId,
        List<Dictionary<string, int>> improvements,
        List<string> featIds,
        List<Dictionary<string, int>> featAbilityIncreases,
        List<FeatProficiencyPickApiModel> featProficiencyPicks) =>
        PutAsync(
            draftId,
            "ability-score-improvements",
            new SetAbilityScoreImprovementsApiRequest(improvements, featIds, featAbilityIncreases, featProficiencyPicks));

    /// <summary>How many Fighting Styles the character picks (0 for most classes) and its class's options.</summary>
    public async Task<FightingStylesApiResponse> GetFightingStylesAsync(Guid draftId)
    {
        var response = await _http.GetAsync($"/api/character-drafts/{draftId}/fighting-styles");
        return await response.ReadAsync<FightingStylesApiResponse>();
    }

    public Task ChooseFightingStylesAsync(Guid draftId, List<string> featIds) =>
        PutAsync(draftId, "fighting-styles", new ChooseFightingStylesApiRequest(featIds));

    /// <summary>How many cantrips and spells the class lets the character pick: a 2014 cleric's count follows its scores.</summary>
    public async Task<SpellAllowanceApiResponse> GetSpellAllowanceAsync(Guid draftId)
    {
        var response = await _http.GetAsync($"/api/character-drafts/{draftId}/spell-allowance");
        return await response.ReadAsync<SpellAllowanceApiResponse>();
    }

    public Task ChooseSpellsAsync(Guid draftId, List<string> spellIds) =>
        PutAsync(draftId, "spells", new ChooseSpellsApiRequest(spellIds));

    /// <summary>The spells the race and feats let the character pick, the race's first; empty when none do.</summary>
    public async Task<List<BonusSpellChoiceApiResponse>> GetBonusSpellsAsync(Guid draftId)
    {
        var response = await _http.GetAsync($"/api/character-drafts/{draftId}/bonus-spells");
        return await response.ReadAsync<List<BonusSpellChoiceApiResponse>>();
    }

    /// <summary>The race's spells and one pick per feat, in the order GetBonusSpellsAsync lists them.</summary>
    public Task ChooseBonusSpellsAsync(Guid draftId, List<string> raceSpellIds, List<FeatSpellPickApiModel> featSpells) =>
        PutAsync(draftId, "bonus-spells", new ChooseBonusSpellsApiRequest { RaceSpellIds = raceSpellIds, FeatSpells = featSpells });

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
