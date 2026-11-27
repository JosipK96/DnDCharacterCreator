using CharacterCreatorApp.Contracts.Responses.Feats;

namespace CharacterCreatorApp.Contracts.Requests.CharacterDrafts;

/// <summary>
/// The spells picked for the race and, one per feat in the order GET bonus-spells lists them, for
/// each feat with a spell choice. Empty lists where there is nothing to pick. RaceSpellAbilityId
/// is the ability picked for the race's spells where its choice lists AbilityOptions (a 2024 high
/// elf's), null otherwise.
/// </summary>
public class ChooseBonusSpellsApiRequest
{
    public List<string> RaceSpellIds { get; set; } = [];
    public string? RaceSpellAbilityId { get; set; }
    public List<FeatSpellPickApiModel> FeatSpells { get; set; } = [];
}
