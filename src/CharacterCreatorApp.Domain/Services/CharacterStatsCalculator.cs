using System.Globalization;

using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Domain.Services;

/// <summary>
/// Works out the parts of a character sheet that follow from the character's own numbers.
/// Armour and shields are not modelled yet, so armour class is the unarmoured one: 10 + DEX, or
/// what the class or subclass gives instead (a barbarian's Unarmored Defense) when that is higher.
/// Spell slots come from the class's or subclass's spell table, a warlock's Pact Magic apart.
/// </summary>
public static class CharacterStatsCalculator
{
    public static CharacterStats Calculate(CharacterStatsInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var scores = NormalizeScores(input.AbilityScores);
        var proficiencyBonus = GetProficiencyBonus(input.Level);

        var proficientSkillIds = input.ProficiencyIds
            .Where(Skill.IsSkill)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var proficientSaves = input.SavingThrowAbilityIds
            .Select(id => AbilityScore.TryNormalize(id, out var normalized) ? normalized : null)
            .Where(id => id is not null)
            .Select(id => id!)
            .ToHashSet(StringComparer.Ordinal);

        var abilities = AbilityScore.All
            .Select(ability => new AbilityStat(ability.Id, GetScore(scores, ability.Id), GetModifier(scores, ability.Id)))
            .ToList();

        var savingThrows = AbilityScore.All
            .Select(ability =>
            {
                var isProficient = proficientSaves.Contains(ability.Id);
                var modifier = GetModifier(scores, ability.Id) + (isProficient ? proficiencyBonus : 0);

                return new SavingThrowStat(ability.Id, modifier, isProficient);
            })
            .ToList();

        var skills = Skill.All
            .Select(skill =>
            {
                var isProficient = proficientSkillIds.Contains(skill.Id);
                var modifier = GetModifier(scores, skill.AbilityId) + (isProficient ? proficiencyBonus : 0);

                return new SkillStat(skill.Id, skill.Name, skill.AbilityId, modifier, isProficient);
            })
            .ToList();

        var dexterityModifier = GetModifier(scores, AbilityScore.DEX.Id);
        var perception = skills.First(s => s.SkillId == Skill.Perception.Id);

        return new CharacterStats(
            ProficiencyBonus: proficiencyBonus,
            MaxHitPoints: GetMaxHitPoints(input, GetModifier(scores, AbilityScore.CON.Id)),
            ArmorClass: GetArmorClass(input.UnarmoredArmorClasses, abilities, dexterityModifier),
            Initiative: dexterityModifier,
            Speed: input.Speed,
            HitDice: $"{input.Level}{NormalizeHitDie(input.HitDie)}",
            PassivePerception: 10 + perception.Modifier,
            Abilities: abilities,
            SavingThrows: savingThrows,
            Skills: skills,
            SpellSlots: GetSpellSlots(input.SpellSlots),
            PactMagic: GetPactMagic(input.SpellSlots));
    }

    private static PactMagicStat? GetPactMagic(SpellSlots spellSlots) =>
        spellSlots.PactMagic is { Slots: > 0 } pactMagic ? new PactMagicStat(pactMagic.SlotLevel, pactMagic.Slots) : null;

    /// <summary>A row per spell level the character has slots of; a table's empty columns are left out.</summary>
    private static List<SpellSlotStat> GetSpellSlots(SpellSlots spellSlots) =>
        spellSlots.PerSpellLevel
            .Select((slots, index) => new SpellSlotStat(SpellLevel: index + 1, Slots: slots))
            .Where(s => s.Slots > 0)
            .ToList();

    /// <summary>+2 at levels 1-4, rising by one every four levels to +6 at level 17.</summary>
    public static int GetProficiencyBonus(int level) => 2 + ((Math.Clamp(level, 1, 20) - 1) / 4);

    /// <summary>The usual 5E modifier: (score - 10) halved, rounding down including below 10.</summary>
    public static int GetAbilityModifier(int score) =>
        (int)Math.Floor((score - 10) / 2.0);

    /// <summary>
    /// Maximum hit die at level 1, then the die's average rounded up for each level after,
    /// with the constitution modifier added every level. A level always adds at least 1, plus
    /// whatever <see cref="HitPointBonuses"/> the character's race, subrace, subclass and feats give.
    /// </summary>
    private static int GetMaxHitPoints(CharacterStatsInput input, int constitutionModifier)
    {
        var dieSize = ParseDieSize(input.HitDie);
        var clampedLevel = Math.Clamp(input.Level, 1, 20);

        var total = Math.Max(1, dieSize + constitutionModifier);
        var perLevelAfterFirst = Math.Max(1, (dieSize / 2) + 1 + constitutionModifier);
        var bonusPerLevel = HitPointBonuses.PerLevel(input.RaceId, input.SubraceId, input.SubclassId, input.FeatIds);

        return total + ((clampedLevel - 1) * perLevelAfterFirst) + (clampedLevel * bonusPerLevel);
    }

    /// <summary>
    /// The best of the ways the character has to work out armour class without armour: both rule
    /// books let a character with more than one pick which, so a barbarian with CON 8 keeps 10 + DEX.
    /// </summary>
    private static int GetArmorClass(
        IReadOnlyCollection<UnarmoredArmorClass> unarmoredArmorClasses, List<AbilityStat> abilities, int dexterityModifier)
    {
        var modifiers = abilities.ToDictionary(a => a.AbilityId, a => a.Modifier, StringComparer.Ordinal);

        return unarmoredArmorClasses
            .Select(armorClass => armorClass.ArmorClassFor(modifiers))
            .Append(10 + dexterityModifier)
            .Max();
    }

    /// <summary>Reads the die size out of "1d10", "d10" or "10".</summary>
    private static int ParseDieSize(string hitDie)
    {
        if (string.IsNullOrWhiteSpace(hitDie))
        {
            return 8;
        }

        var separatorIndex = hitDie.IndexOf('d', StringComparison.OrdinalIgnoreCase);
        var sizePart = separatorIndex >= 0 ? hitDie[(separatorIndex + 1)..] : hitDie;

        return int.TryParse(sizePart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var size) && size > 0
            ? size
            : 8;
    }

    /// <summary>"1d10" and "d10" both describe a d10, so hit dice read as "5d10" either way.</summary>
    private static string NormalizeHitDie(string hitDie) => $"d{ParseDieSize(hitDie)}";

    private static Dictionary<string, int> NormalizeScores(IReadOnlyDictionary<string, int> scores)
    {
        var normalized = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var (key, value) in scores)
        {
            if (AbilityScore.TryNormalize(key, out var id))
            {
                normalized[id] = value;
            }
        }

        return normalized;
    }

    private static int GetScore(Dictionary<string, int> scores, string abilityId) =>
        scores.TryGetValue(abilityId, out var score) ? score : 10;

    private static int GetModifier(Dictionary<string, int> scores, string abilityId) =>
        GetAbilityModifier(GetScore(scores, abilityId));
}
