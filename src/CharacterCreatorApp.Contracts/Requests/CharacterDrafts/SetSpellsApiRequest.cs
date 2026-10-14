namespace CharacterCreatorApp.Contracts.Requests.CharacterDrafts;

public class SetSpellsApiRequest
{
    public List<string> SpellIds { get; set; } = [];
}