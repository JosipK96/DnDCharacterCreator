namespace CharacterCreatorApp.Domain.DomainModels.Characters;

/// <summary>
/// The spells picked for one feat with a spell choice (Feat.SpellChoice): the class list they
/// come from, and the cantrips and 1st-level spells together.
/// </summary>
public class FeatSpellPick
{
    public required string FeatId { get; set; }

    public required string ClassId { get; set; }

    public List<string> SpellIds { get; set; } = [];
}
