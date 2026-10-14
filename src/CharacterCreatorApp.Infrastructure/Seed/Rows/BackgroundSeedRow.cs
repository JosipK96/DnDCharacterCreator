using CharacterCreatorApp.Domain.DomainModels.Proficiencies;

namespace CharacterCreatorApp.Infrastructure.Seed.Rows;

/// <summary>
/// One entry of Backgrounds.json, written by the generator and read by BackgroundSeeder. A 2014
/// background's feature is a row of BackgroundFeatures.json; a 2024 one has none, and raises
/// three abilities and gives an origin feat instead.
/// </summary>
public sealed record BackgroundSeedRow
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Edition { get; init; }

    /// <summary>The code of the book it comes from ("PHB", "XGE"), a row of Books.json.</summary>
    public required string Source { get; init; }
    public List<string> ProficiencyIds { get; init; } = [];

    /// <summary>Language names ("dwarvish"), with "any" for each one the player picks.</summary>
    public List<ToolChoice> ToolChoices { get; init; } = [];

    public List<string> Languages { get; init; } = [];

    /// <summary>The abilities a 2024 background raises, +2 and +1 or +1 to all three.</summary>
    public List<string> AbilityScoreOptions { get; init; } = [];

    /// <summary>The feat the background gives: a 2024 origin feat, or a 2014 one such as the Giant Foundling's.</summary>
    public string? FeatId { get; init; }

    /// <summary>The category the player picks the feat from instead ("Dark Gift" for a Mist Wanderer); null when it is fixed.</summary>
    public string? FeatChoiceCategory { get; init; }

    /// <summary>The class whose list the feat's spells come from: "cleric-2024" for an Acolyte's Magic Initiate.</summary>
    public string? FeatSpellListClassId { get; init; }

    public string? FeatureId { get; init; }
}
