namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>
/// The spells picked for one feat with a spell choice (Magic Initiate): the class list, and the
/// cantrips and spells. AbilityId is the spellcasting ability ("WIS") where the feat lets the
/// player pick it (the 2024 Magic Initiate's), null otherwise.
/// </summary>
public class FeatSpellPickApiModel
{
    public required string FeatId { get; set; }
    public required string ClassId { get; set; }
    public List<string> SpellIds { get; set; } = [];
    public string? AbilityId { get; set; }
}
