using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.DomainModels.Proficiencies;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Domain.DomainModels.Backgrounds;

public class Background
{
    public required string Id { get; set; }
    public required string Name { get; set; }

    /// <summary>RulesEdition.Edition2014 or RulesEdition.Edition2024; a 2024 background's id ends in "_2024".</summary>
    public required string Edition { get; set; }

    /// <summary>The code of the book it comes from ("PHB", "XGE"); see Books.json in the seed data.</summary>
    public string Source { get; set; } = string.Empty;

    public List<BackgroundProficiency> BackgroundProficiencies { get; set; } = [];

    /// <summary>The tools, instruments or gaming sets the player picks for the background: a Criminal's gaming set.</summary>
    public List<ToolChoice> ToolChoices { get; set; } = [];

    /// <summary>
    /// Language names ("dwarvish"), with "any" for each one the player picks. A 2024 background
    /// has Common and two picks: the 2024 rules give them to every new character.
    /// </summary>
    public List<string> Languages { get; set; } = [];

    /// <summary>The placeholder in Languages for a language the player picks.</summary>
    public const string AnyLanguage = "any";

    /// <summary>How many languages the background lets the player pick: the AnyLanguage placeholders in Languages.</summary>
    public int LanguageChoiceCount => Languages.Count(IsLanguageChoice);

    /// <summary>The languages the background names, without the placeholders ("Common" for a 2024 background).</summary>
    public IEnumerable<string> NamedLanguages => Languages.Where(l => !IsLanguageChoice(l));

    private static bool IsLanguageChoice(string language) => language.Equals(AnyLanguage, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The three abilities a 2024 background raises: +2 to one and +1 to another, or +1 to each.
    /// Empty for a 2014 background, whose character's race raises them instead.
    /// </summary>
    public List<string> AbilityScoreOptions { get; set; } = [];

    /// <summary>The feat the background gives: a 2024 origin feat, or a 2014 one such as the Giant Foundling's.</summary>
    public string? FeatId { get; set; }

    public Feat? Feat { get; set; }

    /// <summary>
    /// The category the player picks the background's feat from when it has none of its own: any
    /// Dark Gift for a Mist Wanderer or a Spirit Medium. Null when the feat is fixed or there is none.
    /// </summary>
    public string? FeatChoiceCategory { get; set; }

    /// <summary>
    /// The category the player may pick a feat from in place of FeatId: any Dark Gift for a 2024
    /// Haunted One or Investigator. Null when the background's feat, if any, is the only one.
    /// </summary>
    public string? FeatAlternativeCategory { get; set; }

    /// <summary>
    /// The class whose spell list the feat's spells come from, when the background fixes it: the
    /// cleric's ("cleric-2024") for an Acolyte's Magic Initiate.
    /// </summary>
    public string? FeatSpellListClassId { get; set; }

    /// <summary>A 2014 background's feature ("Shelter of the Faithful"); 2024 backgrounds have none.</summary>
    public string? FeatureId { get; set; }

    public Feature? Feature { get; set; }

    /// <summary>
    /// The increases the player picks: +2 and +1 to two of the three abilities or +1 to each,
    /// which is 3 points at most 2 on one. Nothing for a background without options.
    /// </summary>
    public AbilityIncreaseChoice AbilityChoice() =>
        AbilityScoreOptions.Count == 0 ? AbilityIncreaseChoice.None : new AbilityIncreaseChoice(AbilityScoreOptions, 3, 2);

    /// <summary>
    /// Whether the background allows these ability increases (see AbilityChoice). The keys may
    /// be in any spelling ("str", "Strength").
    /// </summary>
    public bool AllowsAbilityIncreases(IReadOnlyDictionary<string, int> increases) => AbilityChoice().Allows(increases);

    /// <summary>
    /// The category a feat picked for the background must be from: the one it must be picked from
    /// (FeatChoiceCategory) or the alternative to its own (FeatAlternativeCategory). Null when no
    /// feat may be picked.
    /// </summary>
    public string? PickableFeatCategory => FeatChoiceCategory ?? FeatAlternativeCategory;

    /// <summary>
    /// The feat the character gets from the background: the one picked, when the background lets
    /// the player pick one, and otherwise its own (null for none).
    /// </summary>
    public string? FeatIdWith(string? pickedFeatId) => PickableFeatCategory == null ? FeatId : pickedFeatId ?? FeatId;
}
