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

    /// <summary>
    /// The spellcasting ability picked for the spells, by canonical id ("WIS"), where the feat lets
    /// the player pick it (BonusSpellChoice.AbilityOptions: the 2024 Magic Initiate's). Null otherwise.
    /// </summary>
    public string? AbilityId { get; set; }
}
