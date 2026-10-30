using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Proficiencies;
using CharacterCreatorApp.Domain.Services;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Domain.DomainModels.Classes;

public class Class
{
    public required string Id { get; set; }
    public required string Name { get; set; }

    /// <summary>
    /// RulesEdition.Edition2014 or RulesEdition.Edition2024. The two editions' versions of a
    /// class are separate classes with their own ids ("wizard" and "wizard-2024").
    /// </summary>
    public required string Edition { get; set; }

    /// <summary>The code of the book it comes from ("PHB", "XGE"); see Books.json in the seed data.</summary>
    public string Source { get; set; } = string.Empty;

    public required string HitDie { get; set; }

    /// <summary>The hit die's size (10 for "1d10"); throws when HitDie cannot be read, which the seed tests catch.</summary>
    public int HitDieSize => HitDieNotation.SizeOf(HitDie);

    /// <summary>The hit die's fixed value, which a level takes in place of a roll: a d10's is 6.</summary>
    public int HitDieFixedValue => HitDieNotation.FixedValueOf(HitDie);

    /// <summary>
    /// The level at which the class picks its subclass. 2014 classes differ (a cleric picks at 1,
    /// a wizard at 2, most at 3); every 2024 class picks at 3. From the class data, never assumed.
    /// </summary>
    public int SubclassLevel { get; set; }

    /// <summary>
    /// The levels at which the class raises two ability scores or takes a feat instead. The 2024
    /// Epic Boon at level 19 counts: it is a feat in the same place.
    /// </summary>
    public List<int> AsiLevels { get; set; } = [];

    /// <summary>How many improvements (ability points or a feat) a character of this level has made in all: the AsiLevels it has reached.</summary>
    public int ImprovementsAt(int level) => AsiLevels.Count(l => l <= level);

    /// <summary>The two saving throws the class is proficient in, as canonical ids ("STR").</summary>
    public List<string> SavingThrowAbilityIds { get; set; } = [];

    /// <summary>
    /// A barbarian's or monk's Unarmored Defense, from level 1; null for a class without one.
    /// From the class data, never assumed from the id.
    /// </summary>
    public UnarmoredArmorClass? UnarmoredArmorClass { get; set; }

    public List<ClassProficiency> ClassProficiencies { get; set; } = [];
    public ProficiencyChoice? ProficiencyChoice { get; set; }

    /// <summary>The tools or instruments the player picks for the class: a bard's three instruments.</summary>
    public List<ToolChoice> ToolChoices { get; set; } = [];

    public List<Feature> Features { get; set; } = [];
    public List<Subclass> Subclasses { get; set; } = [];
    public Spellcasting? Spellcasting { get; set; }
    public bool IsSpellcaster => Spellcasting != null;
    public List<ClassSpellProgression> SpellProgression { get; set; } = [];

    /// <summary>The unarmoured armour classes a character of this class and subclass has: the class's, the subclass's, or neither.</summary>
    public IReadOnlyList<UnarmoredArmorClass> UnarmoredArmorClassesWith(Subclass? subclass) =>
        new[] { UnarmoredArmorClass, subclass?.UnarmoredArmorClass }.OfType<UnarmoredArmorClass>().ToList();

    /// <summary>
    /// Whether the spells a character of this level picks follow its ability scores: a 2014
    /// cleric, druid, paladin or artificer, whose table gives no count. The scores passed to
    /// SpellAllowanceAt must then be the character's final ones.
    /// </summary>
    public bool PreparesSpellsFromScoresAt(int level) =>
        Spellcasting?.PreparedSpellsLevelDivisor is not null
        && SpellProgression.Find(p => p.CharacterLevel == level) is { SpellsKnown: 0 };

    /// <summary>
    /// The spells a character of this level picks, from the class's table, or from the
    /// subclass's when the class has none (an Eldritch Knight's fighter). Null when the character
    /// casts no spells yet: a 2014 paladin at level 1, a fighter before its subclass. The
    /// subclass's school limit comes with it. A class whose table gives no count prepares its
    /// spellcasting modifier plus its level, or half of it, in spells, at least one
    /// (PreparesSpellsFromScoresAt); the wizard's table gives its spellbook, so that wins.
    /// </summary>
    public SpellAllowance? SpellAllowanceAt(int level, Subclass? subclass, IReadOnlyDictionary<string, int> abilityScores)
    {
        if (SpellProgression.Find(p => p.CharacterLevel == level) is { } row)
        {
            var spells = PreparesSpellsFromScoresAt(level) ? PreparedSpellCount(level, abilityScores) : row.SpellsKnown;
            return new SpellAllowance(row.CantripsKnown, spells, row.MaxSpellLevel, subclass?.SpellSchoolLimitAt(level));
        }

        return subclass?.SpellProgression.Find(p => p.CharacterLevel == level) is { } subclassRow
            ? new SpellAllowance(subclassRow.CantripsKnown, subclassRow.SpellsKnown, subclassRow.MaxSpellLevel, subclass.SpellSchoolLimitAt(level))
            : null;
    }

    /// <summary>
    /// The spell slots a character of this level has, from the class's table, or from the
    /// subclass's when the class has none (an Eldritch Knight's fighter), as in SpellAllowanceAt.
    /// None when the character casts no spells yet.
    /// </summary>
    public SpellSlots SpellSlotsAt(int level, Subclass? subclass)
    {
        if (SpellProgression.Find(p => p.CharacterLevel == level) is { } row)
        {
            return SlotsOf(row.SpellSlots, row.PactMagicSlots, row.MaxSpellLevel);
        }

        return subclass?.SpellProgression.Find(p => p.CharacterLevel == level) is { } subclassRow
            ? SlotsOf(subclassRow.SpellSlots, subclassRow.PactMagicSlots, subclassRow.MaxSpellLevel)
            : SpellSlots.None;
    }

    /// <summary>
    /// The choices a character of this class gains on reaching this level from the one before,
    /// as DraftChoice names: a subclass from SubclassLevel on, an improvement at one of AsiLevels
    /// or a Fighting Style a feature gives, and spells when the spell table, the prepared count
    /// or the subclass's own spells change. A subclass picked at this level may be any of the
    /// class's, so what any of them would bring counts. The scores are the character's final
    /// ones, which a 2014 cleric's prepared count follows.
    /// </summary>
    public IReadOnlyList<string> ChoicesGainedAt(int level, Subclass? subclass, IReadOnlyDictionary<string, int> abilityScores)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 2);

        var choices = new List<string>();
        var picksSubclass = subclass == null && level >= SubclassLevel;
        var subclassesAfter = picksSubclass && Subclasses.Count != 0 ? Subclasses.ConvertAll<Subclass?>(s => s) : [subclass];

        if (picksSubclass)
        {
            choices.Add(DraftChoice.Subclass);
        }

        var gainsImprovement = AsiLevels.Contains(level);
        var stylesBefore = CharacterFeatures.FightingStylesOwed(this, subclass, level - 1);

        if (gainsImprovement || subclassesAfter.Any(s => CharacterFeatures.FightingStylesOwed(this, s, level) > stylesBefore))
        {
            choices.Add(DraftChoice.Improvements);
        }

        // A domain or oath spell the subclass gives may be one already picked, which finalize refuses
        var spellsBefore = SpellAllowanceAt(level - 1, subclass, abilityScores);
        var preparedBefore = subclass?.AlwaysPreparedSpellIdsAt(level - 1).Count() ?? 0;
        var gainsSpells = subclassesAfter.Any(s =>
                !SameCounts(SpellAllowanceAt(level, s, abilityScores), spellsBefore)
                || (s?.AlwaysPreparedSpellIdsAt(level).Count() ?? 0) > preparedBefore)
            || (gainsImprovement && PreparesSpellsFromScoresAt(level));

        if (gainsSpells)
        {
            choices.Add(DraftChoice.Spells);
        }

        return choices;
    }

    // The school limit's lists are compared by reference in the record, so the numbers are compared one by one
    private static bool SameCounts(SpellAllowance? after, SpellAllowance? before) =>
        after == null || before == null
            ? after == before
            : after.Cantrips == before.Cantrips
                && after.Spells == before.Spells
                && after.MaxSpellLevel == before.MaxSpellLevel
                && after.SchoolLimit?.AnySchoolSpells == before.SchoolLimit?.AnySchoolSpells;

    private static SpellSlots SlotsOf(List<int> perSpellLevel, int pactMagicSlots, int maxSpellLevel) =>
        new(perSpellLevel, pactMagicSlots > 0 ? new PactMagicSlots(pactMagicSlots, maxSpellLevel) : null);

    private int PreparedSpellCount(int level, IReadOnlyDictionary<string, int> abilityScores)
    {
        // A score the draft lacks counts as 10, as on the sheet; finalize refuses such a draft anyway
        var abilityId = AbilityScore.Normalize(Spellcasting!.SpellcastingAbility);
        var score = abilityScores
            .Where(s => AbilityScore.TryNormalize(s.Key, out var id) && id == abilityId)
            .Select(s => s.Value)
            .DefaultIfEmpty(10)
            .First();

        return Math.Max(1, CharacterStatsCalculator.GetAbilityModifier(score) + (level / Spellcasting.PreparedSpellsLevelDivisor!.Value));
    }
}
