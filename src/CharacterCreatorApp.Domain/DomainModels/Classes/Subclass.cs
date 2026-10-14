using CharacterCreatorApp.Domain.DomainModels.Spells;

namespace CharacterCreatorApp.Domain.DomainModels.Classes;

public class Subclass
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string ClassId { get; set; }

    /// <summary>The code of the book it comes from ("PHB", "XGE"); see Books.json in the seed data.</summary>
    public string Source { get; set; } = string.Empty;
    public Class Class { get; set; } = null!;
    public List<Feature> Features { get; set; } = [];
    public List<SubclassSpellProgression> SpellProgression { get; set; } = [];

    /// <summary>
    /// A class whose whole spell list the subclass picks from: the Wizard's for an Eldritch Knight
    /// or Arcane Trickster, whose own class has no list, and the Cleric's for a Divine Soul
    /// sorcerer on top of the Sorcerer's.
    /// </summary>
    public string? SpellListClassId { get; set; }

    public List<SpellSubclass> Spells { get; set; } = [];

    /// <summary>
    /// The schools the subclass's levelled spells must come from: abjuration and evocation for a
    /// 2014 Eldritch Knight, enchantment and illusion for a 2014 Arcane Trickster. Empty when
    /// any school will do, as for every 2024 subclass.
    /// </summary>
    public List<string> SpellSchools { get; set; } = [];

    /// <summary>The character levels that each give one spell from any school despite <see cref="SpellSchools"/>.</summary>
    public List<int> AnySchoolSpellLevels { get; set; } = [];

    /// <summary>The school limit on a character of this level's spells, or null when there is none.</summary>
    public SpellSchoolLimit? SpellSchoolLimitAt(int level) =>
        SpellSchools.Count == 0 ? null : new SpellSchoolLimit(SpellSchools, AnySchoolSpellLevels.Count(l => l <= level));

    /// <summary>The spells the subclass gives a character of this level without being picked.</summary>
    public IEnumerable<string> AlwaysPreparedSpellIdsAt(int level) =>
        Spells.Where(s => s.IsAlwaysPrepared && s.Level <= level).Select(s => s.SpellId);

    /// <summary>The spells the subclass adds to what its class may pick from.</summary>
    public IEnumerable<string> AddedSpellIds => Spells.Where(s => !s.IsAlwaysPrepared).Select(s => s.SpellId);
}
