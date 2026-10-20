using CharacterCreatorApp.Domain.DomainModels.Proficiencies;

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

    /// <summary>
    /// The spells a character of this level picks, from the class's table, or from the
    /// subclass's when the class has none (an Eldritch Knight's fighter). Null when the character
    /// casts no spells yet: a 2014 paladin at level 1, a fighter before its subclass. The
    /// subclass's school limit comes with it.
    /// </summary>
    /// <summary>The unarmoured armour classes a character of this class and subclass has: the class's, the subclass's, or neither.</summary>
    public IReadOnlyList<UnarmoredArmorClass> UnarmoredArmorClassesWith(Subclass? subclass) =>
        new[] { UnarmoredArmorClass, subclass?.UnarmoredArmorClass }.OfType<UnarmoredArmorClass>().ToList();

    public SpellAllowance? SpellAllowanceAt(int level, Subclass? subclass)
    {
        if (SpellProgression.Find(p => p.CharacterLevel == level) is { } row)
        {
            return new SpellAllowance(row.CantripsKnown, row.SpellsKnown, row.MaxSpellLevel, subclass?.SpellSchoolLimitAt(level));
        }

        return subclass?.SpellProgression.Find(p => p.CharacterLevel == level) is { } subclassRow
            ? new SpellAllowance(subclassRow.CantripsKnown, subclassRow.SpellsKnown, subclassRow.MaxSpellLevel, subclass.SpellSchoolLimitAt(level))
            : null;
    }
}
