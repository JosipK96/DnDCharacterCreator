using CharacterCreatorApp.Domain.Services;

namespace CharacterCreatorApp.Application.ReadModels;

/// <summary>
/// The character a draft would produce, worked out without saving anything. Backs the review
/// step so the player can check the result before committing to it.
/// </summary>
public sealed class CharacterPreviewReadModel
{
    public required string Name { get; init; }
    public int Level { get; init; }
    public required string Edition { get; init; }

    public required string RaceName { get; init; }
    public string? SubraceName { get; init; }
    public required string ClassName { get; init; }
    public string? SubclassName { get; init; }
    public required string BackgroundName { get; init; }

    /// <summary>"Small" or "Medium", or the race's "Small or Medium" while none is picked.</summary>
    public required string Size { get; init; }

    public List<ProficiencyReadModel> Proficiencies { get; set; } = [];
    public List<LanguageReadModel> Languages { get; set; } = [];
    public List<FeatureReadModel> Features { get; set; } = [];
    public List<SpellReadModel> Spells { get; set; } = [];

    /// <summary>One entry per feat taken, so a repeatable feat taken twice is two entries.</summary>
    public List<CharacterFeatReadModel> Feats { get; set; } = [];

    public required CharacterStats Stats { get; init; }

    /// <summary>
    /// Set when a required choice is still missing, in which case finalizing would be rejected.
    /// The preview is still returned so the player can see how far along they are.
    /// </summary>
    public string? BlockingIssue { get; set; }
}
