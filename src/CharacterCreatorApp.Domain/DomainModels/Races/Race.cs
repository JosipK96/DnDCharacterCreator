using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Domain.DomainModels.Races;

public class Race
{
    public required string Id { get; set; }
    public required string Name { get; set; }

    /// <summary>
    /// RulesEdition.Edition2014 or RulesEdition.Edition2024. The 2024 species are separate races
    /// with their own ids ("elf_2024"), and their lineages are their subraces.
    /// </summary>
    public required string Edition { get; set; }

    /// <summary>The code of the book it comes from ("PHB", "XGE"); see Books.json in the seed data.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>"Medium", "Small", or "Small or Medium" for a 2024 species the player sizes.</summary>
    public required string Size { get; set; }

    public int Speed { get; set; }

    /// <summary>
    /// How many skills the player picks for the race: two for a half-elf, one for a 2024 elf or
    /// human. From the race data, never assumed.
    /// </summary>
    public int SkillChoiceCount { get; set; }

    /// <summary>The skills that pick is from (a 2024 elf's Insight, Perception or Survival); empty means any.</summary>
    public List<string> SkillChoiceOptions { get; set; } = [];

    /// <summary>How many languages the player picks for the race; a subrace may add more.</summary>
    public int LanguageChoiceCount { get; set; }

    /// <summary>
    /// How many abilities the player raises by 1 besides the fixed bonuses: a half-elf two of
    /// AbilityChoiceOptions, on top of its Charisma +2.
    /// </summary>
    public int AbilityChoiceCount { get; set; }

    /// <summary>The abilities that choice is from, as canonical ids.</summary>
    public List<string> AbilityChoiceOptions { get; set; } = [];

    /// <summary>
    /// Feats the race gives beside those taken for improvements: the 2024 human's Origin feat.
    /// FeatChoiceCategory limits them to one category; null means any feat.
    /// </summary>
    public int FeatChoiceCount { get; set; }

    public string? FeatChoiceCategory { get; set; }

    public ICollection<RaceLanguage> RaceLanguages { get; set; } = [];
    public ICollection<RaceAbilityBonus> AbilityBonuses { get; set; } = [];
    public ICollection<RaceTrait> Traits { get; set; } = [];
    public ICollection<RaceProficiency> RaceProficiencies { get; set; } = [];
    public ICollection<RaceSpell> Spells { get; set; } = [];
    public ICollection<Subrace> Subraces { get; set; } = [];

    /// <summary>The skills picked for the race and its subrace together: a variant human's one.</summary>
    public int SkillChoiceCountWith(Subrace? subrace) => SkillChoiceCount + (subrace?.SkillChoiceCount ?? 0);

    /// <summary>The skills those picks are from, race and subrace together; empty when any skill will do.</summary>
    public IReadOnlyList<string> SkillChoiceOptionsWith(Subrace? subrace) =>
        OffersAnySkill(subrace) ? [] : SkillChoiceOptions.Concat(subrace?.SkillChoiceOptions ?? []).ToList();

    /// <summary>
    /// Whether a pick for the race or subrace may be this skill: one of the options, or any skill
    /// when either pick has none (a variant human's).
    /// </summary>
    public bool OffersSkill(string skillId, Subrace? subrace)
    {
        var options = SkillChoiceOptionsWith(subrace);
        return options.Count == 0 ? Skill.IsSkill(skillId) : options.Contains(skillId);
    }

    private bool OffersAnySkill(Subrace? subrace) =>
        (SkillChoiceCount > 0 && SkillChoiceOptions.Count == 0) || subrace is { SkillChoiceCount: > 0, SkillChoiceOptions.Count: 0 };

    /// <summary>The languages picked for the race and its subrace together: a high elf's one.</summary>
    public int LanguageChoiceCountWith(Subrace? subrace) => LanguageChoiceCount + (subrace?.LanguageChoiceCount ?? 0);

    /// <summary>
    /// The ability increases the player picks for the race or its subrace, +1 each: a half-elf's
    /// two, or a variant human's, which take the place of a human's +1 to everything.
    /// </summary>
    public AbilityIncreaseChoice AbilityChoiceWith(Subrace? subrace) =>
        subrace is { AbilityChoiceCount: > 0 }
            ? new AbilityIncreaseChoice(subrace.AbilityChoiceOptions, subrace.AbilityChoiceCount, 1)
            : AbilityChoiceCount > 0
                ? new AbilityIncreaseChoice(AbilityChoiceOptions, AbilityChoiceCount, 1)
                : AbilityIncreaseChoice.None;

    /// <summary>The feats the race and its subrace give together: a variant human's any one, a 2024 human's Origin one.</summary>
    public BonusFeatChoice BonusFeatsWith(Subrace? subrace) =>
        subrace is { FeatChoiceCount: > 0 }
            ? new BonusFeatChoice(FeatChoiceCount + subrace.FeatChoiceCount, subrace.FeatChoiceCategory)
            : new BonusFeatChoice(FeatChoiceCount, FeatChoiceCategory);

    /// <summary>The walking speed, which a subrace may change: a wood elf's is 35 feet.</summary>
    public int SpeedWith(Subrace? subrace) => subrace?.Speed ?? Speed;

    /// <summary>Every size the race may be: "Small" and "Medium" for one the player sizes.</summary>
    public IReadOnlyList<string> SizeOptions() =>
        Size.Split(" or ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// The spells the race and its subrace give by this character level: a tiefling's
    /// Thaumaturgy from the start and Hellish Rebuke at 3.
    /// </summary>
    public IEnumerable<string> SpellIdsAt(int level, Subrace? subrace) =>
        Spells.Where(s => s.Level <= level).Select(s => s.SpellId)
            .Concat(subrace?.Spells.Where(s => s.Level <= level).Select(s => s.SpellId) ?? []);
}
