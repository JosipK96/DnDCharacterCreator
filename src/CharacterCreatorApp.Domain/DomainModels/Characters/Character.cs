using CharacterCreatorApp.Domain.Common;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Domain.DomainModels.Characters;

public class Character : Entity<Guid>, IAuditable
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; }
    public int Level { get; private set; }

    /// <summary>RulesEdition.Edition2014 or Edition2024, the rules the character was built with.</summary>
    public string Edition { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public string RaceId { get; private set; }
    public string? SubraceId { get; private set; }
    public string ClassId { get; private set; }
    public string? SubclassId { get; private set; }
    public string BackgroundId { get; private set; }

    public Race Race { get; private set; } = null!;
    public Subrace? Subrace { get; private set; }
    public Class Class { get; private set; } = null!;
    public Subclass? Subclass { get; private set; }
    public Background Background { get; private set; } = null!;

    public Dictionary<string, int> AbilityScores { get; private set; } = [];
    public List<string> ProficiencyIds { get; private set; } = [];
    public List<string> LanguageIds { get; private set; } = [];
    public List<string> SpellIds { get; private set; } = [];

    /// <summary>
    /// Feats taken in place of an ability score improvement. Unlike the other lists this one
    /// may repeat an id, since a few feats can be taken more than once.
    /// </summary>
    public List<string> FeatIds { get; private set; } = [];

    /// <summary>
    /// The spells picked for each feat with a spell choice (Magic Initiate), one pick per such
    /// feat in FeatIds order, so a feat taken twice keeps its two picks apart. Empty for a
    /// character made before they were kept; their spells are in SpellIds all the same.
    /// </summary>
    public List<FeatSpellPick> FeatSpells { get; private set; } = [];

    /// <summary>"Small" or "Medium". Null for a character made before the size was kept; its race's size stands.</summary>
    public string? Size { get; private set; }

    /// <summary>Saving throws a feat adds beside the class's (Resilient), by canonical ability id.</summary>
    public List<string> FeatSavingThrowAbilityIds { get; private set; } = [];

    /// <summary>
    /// The hit die's result for each level a level-up reached, by level (rolled, or the fixed
    /// value taken). Every other level takes the fixed value; the sheet adds the CON modifier.
    /// </summary>
    public Dictionary<int, int> HitDieResults { get; private set; } = [];

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private Character() { }
#pragma warning restore CS8618

    public static Character Create(
        Guid userId,
        string name,
        int level,
        string edition,
        string raceId,
        string? subraceId,
        string classId,
        string? subclassId,
        string backgroundId,
        Dictionary<string, int> baseAbilityScores,
        TimeProvider timeProvider
    )
    {
        if (!RulesEdition.IsKnown(edition))
        {
            throw new ArgumentException($"Unknown rules edition: {edition}", nameof(edition));
        }

        var now = timeProvider.GetUtcNow();

        var character = new Character
        {
            // Time-ordered, like CharacterDraft's: see CreateEmpty there
            Id = Guid.CreateVersion7(now),
            UserId = userId,
            Name = name,
            Level = level,
            Edition = edition,
            RaceId = raceId,
            SubraceId = subraceId,
            ClassId = classId,
            SubclassId = subclassId,
            BackgroundId = backgroundId,
            AbilityScores = NormalizeScores(baseAbilityScores),
            CreatedAt = now
        };

        return character;
    }

    /// <summary>
    /// Takes everything an edit built (CharacterBuilder makes a new Character from the edit draft)
    /// but what makes this one this character: its id, its owner and when it was made. UpdatedAt is
    /// stamped on save.
    /// </summary>
    public void ReplaceWith(Character rebuilt)
    {
        ArgumentNullException.ThrowIfNull(rebuilt);

        if (rebuilt.UserId != UserId)
        {
            throw new ArgumentException("A character is only rebuilt for its owner.", nameof(rebuilt));
        }

        Name = rebuilt.Name;
        Level = rebuilt.Level;
        Edition = rebuilt.Edition;
        RaceId = rebuilt.RaceId;
        SubraceId = rebuilt.SubraceId;
        ClassId = rebuilt.ClassId;
        SubclassId = rebuilt.SubclassId;
        BackgroundId = rebuilt.BackgroundId;
        AbilityScores = new Dictionary<string, int>(rebuilt.AbilityScores);
        ProficiencyIds = [.. rebuilt.ProficiencyIds];
        LanguageIds = [.. rebuilt.LanguageIds];
        SpellIds = [.. rebuilt.SpellIds];
        FeatIds = [.. rebuilt.FeatIds];
        FeatSpells = [.. rebuilt.FeatSpells];
        Size = rebuilt.Size;
        FeatSavingThrowAbilityIds = [.. rebuilt.FeatSavingThrowAbilityIds];
        HitDieResults = new Dictionary<int, int>(rebuilt.HitDieResults);
    }

    /// <summary>
    /// Ability keys reach us in several spellings ("CON" from the draft, "con" from race
    /// seed data, "constitution" from ability score improvements). Everything is stored
    /// under the canonical id so the three sources land on the same entry.
    /// </summary>
    private static Dictionary<string, int> NormalizeScores(IEnumerable<KeyValuePair<string, int>> scores)
    {
        var normalized = new Dictionary<string, int>();

        foreach (var (key, value) in scores)
        {
            normalized[AbilityScore.Normalize(key)] = value;
        }

        return normalized;
    }

    // Increases stop at 20, an Epic Boon's at 30; a score already past the cap stays where it is
    private void AddAbilityScoreBonus(string abilityKey, int bonus, int maximum = AbilityScore.Maximum)
    {
        var id = AbilityScore.Normalize(abilityKey);
        var current = AbilityScores.TryGetValue(id, out var value) ? value : 0;

        AbilityScores[id] = AbilityScore.Raise(current, bonus, maximum);
    }

    public void ApplyRaceAbilityScoreBonuses(IEnumerable<RaceAbilityBonus> bonuses)
    {
        foreach (var bonus in bonuses)
        {
            AddAbilityScoreBonus(bonus.AbilityScoreId, bonus.Bonus);
        }
    }

    public void ApplySubraceAbilityScoreBonuses(IEnumerable<SubraceAbilityBonus> bonuses)
    {
        foreach (var bonus in bonuses)
        {
            AddAbilityScoreBonus(bonus.AbilityScoreId, bonus.Bonus);
        }
    }

    /// <summary>
    /// Applies the +1s picked for the race or subrace: a half-elf's two, a variant human's two.
    /// Capped at 20 like every other increase.
    /// </summary>
    public void ApplyRaceAbilityIncreases(IReadOnlyDictionary<string, int> increases)
    {
        foreach (var (ability, points) in increases)
        {
            AddAbilityScoreBonus(ability, points);
        }
    }

    /// <summary>
    /// Applies a 2024 background's ability increases, the ones the player picked from its three
    /// abilities; capped at 20 like every other increase.
    /// </summary>
    public void ApplyBackgroundAbilityIncreases(IReadOnlyDictionary<string, int> increases)
    {
        foreach (var (ability, points) in increases)
        {
            AddAbilityScoreBonus(ability, points);
        }
    }

    /// <summary>
    /// Applies what one feat raises, up to the feat's maximum: 20, or 30 for an Epic Boon.
    /// </summary>
    public void ApplyFeatAbilityIncrease(IReadOnlyDictionary<string, int> increase, int maximum)
    {
        foreach (var (ability, points) in increase)
        {
            AddAbilityScoreBonus(ability, points, maximum);
        }
    }

    /// <summary>
    /// Applies the ability score improvements chosen during character creation. Each entry
    /// is one ASI use distributing 2 points, e.g. {"STR": 2} or {"STR": 1, "DEX": 1}.
    /// Applied after racial bonuses, capped at 20.
    /// </summary>
    public void ApplyAbilityScoreImprovements(IEnumerable<Dictionary<string, int>> improvements)
    {
        foreach (var improvement in improvements)
        {
            foreach (var (ability, points) in improvement)
            {
                AddAbilityScoreBonus(ability, points);
            }
        }
    }

    public void SetLanguages(IEnumerable<string> languageIds)
    {
        LanguageIds = languageIds.Distinct().ToList();
    }

    public void SetProficiencies(IEnumerable<string> proficiencyIds)
    {
        ProficiencyIds = proficiencyIds.Distinct().ToList();
    }

    public void SetSpells(IEnumerable<string> spellIds)
    {
        SpellIds = spellIds.Distinct().ToList();
    }

    /// <summary>
    /// Records the feats taken. Duplicates are kept: a repeatable feat taken twice is two feats.
    /// </summary>
    public void SetFeats(IEnumerable<string> featIds)
    {
        FeatIds = featIds.ToList();
    }

    /// <summary>The spells picked for each feat with a spell choice, as the draft kept them.</summary>
    public void SetFeatSpells(IEnumerable<FeatSpellPick> featSpells)
    {
        FeatSpells = featSpells.ToList();
    }

    /// <summary>"Small" or "Medium": the race's only size, or the one picked for a species that may be either.</summary>
    public void SetSize(string size)
    {
        Size = size;
    }

    /// <summary>The hit die's result for each level a level-up reached, as the draft kept them.</summary>
    public void SetHitDieResults(IReadOnlyDictionary<int, int> results)
    {
        HitDieResults = new Dictionary<int, int>(results);
    }

    /// <summary>The saving throws feats add, by canonical ability id: Resilient's, which is the ability it raised.</summary>
    public void SetFeatSavingThrows(IEnumerable<string> abilityIds)
    {
        FeatSavingThrowAbilityIds = abilityIds.Select(AbilityScore.Normalize).Distinct().ToList();
    }
}