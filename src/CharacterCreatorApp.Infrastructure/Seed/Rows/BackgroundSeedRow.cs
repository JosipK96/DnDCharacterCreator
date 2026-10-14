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
    public List<string> ProficiencyIds { get; init; } = [];

    /// <summary>Language names ("dwarvish"), with "any" for each one the player picks.</summary>
    public List<string> Languages { get; init; } = [];

    /// <summary>The abilities a 2024 background raises, +2 and +1 or +1 to all three.</summary>
    public List<string> AbilityScoreOptions { get; init; } = [];

    /// <summary>The feat the background gives: a 2024 origin feat, or a 2014 one such as the Giant Foundling's.</summary>
    public string? FeatId { get; init; }

    public string? FeatureId { get; init; }
}
