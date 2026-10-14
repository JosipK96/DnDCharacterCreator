using CharacterCreatorApp.Domain.Services;

namespace CharacterCreatorApp.Application.ReadModels;

public sealed class CharacterDetailsReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Edition { get; set; } = string.Empty;

    // IDs
    public string RaceId { get; set; } = string.Empty;
    public string? SubraceId { get; set; }
    public string ClassId { get; set; } = string.Empty;
    public string? SubclassId { get; set; }
    public string BackgroundId { get; set; } = string.Empty;

    // Display names
    public string RaceName { get; set; } = string.Empty;
    public string? SubraceName { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string? SubclassName { get; set; }
    public string BackgroundName { get; set; } = string.Empty;

    /// <summary>"Small" or "Medium": the one picked for a species that may be either, else the race's.</summary>
    public string Size { get; set; } = string.Empty;

    // Character data
    public Dictionary<string, int> AbilityScores { get; set; } = [];
    public List<string> ProficiencyIds { get; init; } = [];
    public List<ProficiencyReadModel> Proficiencies { get; set; } = [];
    public List<string> LanguageIds { get; set; } = [];
    public List<LanguageReadModel> Languages { get; set; } = [];
    public List<FeatureReadModel> Features { get; set; } = [];
    public List<string> SpellIds { get; set; } = [];
    public List<SpellReadModel> Spells { get; set; } = [];
    public List<string> FeatIds { get; set; } = [];
    public List<FeatReadModel> Feats { get; set; } = [];

    /// <summary>Saving throws feats add beside the class's: Resilient's.</summary>
    public List<string> FeatSavingThrowAbilityIds { get; set; } = [];

    /// <summary>Derived numbers (modifiers, hit points, armour class, saves, skills).</summary>
    public CharacterStats? Stats { get; set; }

    public DateTime CreatedAt { get; set; }
}