namespace CharacterCreatorApp.Application.ReadModels;

/// <summary>
/// One feat a character took, with the spells picked for it (Magic Initiate's), empty when the
/// feat has no spell choice. A feat taken twice is two of these, each with its own spells.
/// </summary>
public class CharacterFeatReadModel
{
    public required FeatReadModel Feat { get; set; }

    public List<SpellReadModel> Spells { get; set; } = [];

    /// <summary>The spellcasting ability picked for the spells ("WIS": the 2024 Magic Initiate's); null where there was none to pick.</summary>
    public string? SpellcastingAbilityId { get; set; }
}
