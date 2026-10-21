namespace CharacterCreatorApp.Contracts.Responses;

public class CharacterDraftDetailsApiResponse
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public int? Level { get; set; }
    public string Edition { get; set; } = string.Empty;

    /// <summary>The books, by code, the character may take its choices from; always its edition's Player's Handbook.</summary>
    public List<string> AllowedSources { get; set; } = [];

    public string? RaceId { get; set; }
    public string? SubraceId { get; set; }

    /// <summary>The size picked for a species that may be Small or Medium; null otherwise.</summary>
    public string? Size { get; set; }

    /// <summary>The spells picked for the race: a high elf's wizard cantrip.</summary>
    public List<string> RaceSpellIds { get; set; } = [];

    /// <summary>The +1s picked for the race or subrace by ability id: a half-elf's two.</summary>
    public Dictionary<string, int> RaceAbilityIncreases { get; set; } = [];

    public string? ClassId { get; set; }
    public string? SubclassId { get; set; }
    public string? BackgroundId { get; set; }

    /// <summary>The feat picked for a background that lets the player choose it (a Dark Gift).</summary>
    public string? BackgroundFeatId { get; set; }

    /// <summary>A 2024 background's ability increases by ability id ("STR"); empty otherwise.</summary>
    public Dictionary<string, int> BackgroundAbilityIncreases { get; set; } = [];

    public Dictionary<string, int> BaseAbilityScores { get; set; } = [];
    public List<string> SelectedClassProficiencyIds { get; set; } = [];
    public List<string> SelectedRaceSkillIds { get; set; } = [];

    /// <summary>The tools picked for each tool choice, in the order GET tool-choices lists them.</summary>
    public List<List<string>> ToolPicks { get; set; } = [];
    public List<string> SelectedLanguageIds { get; set; } = [];
    public List<string> SelectedFeatIds { get; set; } = [];

    /// <summary>What each of SelectedFeatIds raises, in the same order; empty for a feat that raises nothing.</summary>
    public List<Dictionary<string, int>> FeatAbilityIncreases { get; set; } = [];

    /// <summary>
    /// The skills or tools picked for each feat that asks for them (Keen Mind, Skilled): the
    /// background's feat first, then SelectedFeatIds' in order.
    /// </summary>
    public List<FeatProficiencyPickApiModel> FeatProficiencyPicks { get; set; } = [];

    /// <summary>The Fighting Style feats picked for the class's Fighting Style features.</summary>
    public List<string> FightingStyleIds { get; set; } = [];

    public List<string> SelectedSpellIds { get; set; } = [];

    /// <summary>The spells picked for each feat with a spell choice, in the order the bonus spells endpoint lists them.</summary>
    public List<FeatSpellPickApiModel> FeatSpells { get; set; } = [];
    public List<Dictionary<string, int>> AbilityScoreImprovements { get; set; } = [];
    public bool IsCompleted { get; set; }
}