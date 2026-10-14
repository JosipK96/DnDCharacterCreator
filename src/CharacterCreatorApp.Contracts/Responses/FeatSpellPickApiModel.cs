namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>The spells picked for one feat with a spell choice (Magic Initiate): the class list, and the cantrips and spells.</summary>
public class FeatSpellPickApiModel
{
    public required string FeatId { get; set; }
    public required string ClassId { get; set; }
    public List<string> SpellIds { get; set; } = [];
}
