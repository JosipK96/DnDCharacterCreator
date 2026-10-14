using CharacterCreatorApp.Domain.DomainModels.Feats;

namespace CharacterCreatorApp.Infrastructure.Seed.Rows;

/// <summary>
/// One entry of Feats.json, written by the generator and read by FeatSeeder. The prerequisites
/// are already parsed from 5etools' list; nothing downstream reads 5etools shapes.
/// </summary>
public sealed record FeatSeedRow
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Edition { get; init; }
    public string? Category { get; init; }
    public FeatPrerequisites Prerequisites { get; init; } = new();
    public bool IsRepeatable { get; init; }

    /// <summary>The ability score the feat raises, the options as canonical ids; null when it raises none.</summary>
    public FeatAbilityIncrease? AbilityIncrease { get; init; }

    public List<string> GrantedProficiencyIds { get; init; } = [];

    /// <summary>Language names as 5etools writes them ("sylvan"), which LanguageResolver turns into ids.</summary>
    public List<string> GrantedLanguageIds { get; init; } = [];

    public List<string> GrantedSpellIds { get; init; } = [];
    public FeatChoice? Choice { get; init; }
}
