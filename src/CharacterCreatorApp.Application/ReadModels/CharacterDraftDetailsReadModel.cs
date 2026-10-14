namespace CharacterCreatorApp.Application.ReadModels;

public class CharacterDraftDetailsReadModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public int? Level { get; set; }
    public string Edition { get; set; } = string.Empty;
    public string? RaceId { get; set; }
    public string? SubraceId { get; set; }

    /// <summary>The +1s picked for the race or subrace by ability id: a half-elf's two.</summary>
    public Dictionary<string, int> RaceAbilityIncreases { get; set; } = [];

    public string? ClassId { get; set; }
    public string? SubclassId { get; set; }
    public string? BackgroundId { get; set; }

    /// <summary>A 2024 background's ability increases by ability id ("STR"); empty otherwise.</summary>
    public Dictionary<string, int> BackgroundAbilityIncreases { get; set; } = [];

    public Dictionary<string, int> BaseAbilityScores { get; set; } = [];
    public List<string> SelectedClassProficiencyIds { get; set; } = [];
    public List<string> SelectedRaceSkillIds { get; set; } = [];
    public List<string> SelectedLanguageIds { get; set; } = [];
    public List<string> SelectedFeatIds { get; set; } = [];

    /// <summary>What each of SelectedFeatIds raises, in the same order; empty for a feat that raises nothing.</summary>
    public List<Dictionary<string, int>> FeatAbilityIncreases { get; set; } = [];

    /// <summary>The Fighting Style feats picked for the class's Fighting Style features.</summary>
    public List<string> FightingStyleIds { get; set; } = [];

    public List<string> SelectedSpellIds { get; set; } = [];
    public List<Dictionary<string, int>> AbilityScoreImprovements { get; set; } = [];
    public bool IsCompleted { get; set; }
}