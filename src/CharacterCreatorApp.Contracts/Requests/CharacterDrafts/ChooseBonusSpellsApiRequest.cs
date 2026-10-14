using CharacterCreatorApp.Contracts.Responses;

namespace CharacterCreatorApp.Contracts.Requests.CharacterDrafts;

/// <summary>
/// The spells picked for the race and, one per feat in the order GET bonus-spells lists them, for
/// each feat with a spell choice. Empty lists where there is nothing to pick.
/// </summary>
public class ChooseBonusSpellsApiRequest
{
    public List<string> RaceSpellIds { get; set; } = [];
    public List<FeatSpellPickApiModel> FeatSpells { get; set; } = [];
}
