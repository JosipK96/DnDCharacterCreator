using System.Collections.Frozen;

namespace CharacterCreatorApp.Domain.ValueObjects;

/// <summary>
/// The eighteen skills and the ability each is checked against. Ids match the seeded
/// proficiency ids, so a character's proficiency list can be matched against them directly.
/// </summary>
public sealed class Skill
{
    public string Id { get; }
    public string Name { get; }
    public string AbilityId { get; }

    private Skill(string id, string name, string abilityId)
    {
        Id = id;
        Name = name;
        AbilityId = abilityId;
    }

    public static readonly Skill Acrobatics = new("acrobatics", "Acrobatics", "DEX");
    public static readonly Skill AnimalHandling = new("animal_handling", "Animal Handling", "WIS");
    public static readonly Skill Arcana = new("arcana", "Arcana", "INT");
    public static readonly Skill Athletics = new("athletics", "Athletics", "STR");
    public static readonly Skill Deception = new("deception", "Deception", "CHA");
    public static readonly Skill History = new("history", "History", "INT");
    public static readonly Skill Insight = new("insight", "Insight", "WIS");
    public static readonly Skill Intimidation = new("intimidation", "Intimidation", "CHA");
    public static readonly Skill Investigation = new("investigation", "Investigation", "INT");
    public static readonly Skill Medicine = new("medicine", "Medicine", "WIS");
    public static readonly Skill Nature = new("nature", "Nature", "INT");
    public static readonly Skill Perception = new("perception", "Perception", "WIS");
    public static readonly Skill Performance = new("performance", "Performance", "CHA");
    public static readonly Skill Persuasion = new("persuasion", "Persuasion", "CHA");
    public static readonly Skill Religion = new("religion", "Religion", "INT");
    public static readonly Skill SleightOfHand = new("sleight_of_hand", "Sleight of Hand", "DEX");
    public static readonly Skill Stealth = new("stealth", "Stealth", "DEX");
    public static readonly Skill Survival = new("survival", "Survival", "WIS");

    public static IReadOnlyList<Skill> All =>
    [
        Acrobatics, AnimalHandling, Arcana, Athletics, Deception, History,
        Insight, Intimidation, Investigation, Medicine, Nature, Perception,
        Performance, Persuasion, Religion, SleightOfHand, Stealth, Survival
    ];

    private static readonly FrozenDictionary<string, Skill> ById =
        All.ToFrozenDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);

    public static bool IsSkill(string proficiencyId) => ById.ContainsKey(proficiencyId);
}
