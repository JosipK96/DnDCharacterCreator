using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using CharacterCreatorApp.Domain.Services;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Reads what the sheet's attacks table needs from a 5etools spell: "spellAttack" (["R"]),
/// "savingThrow" (["dexterity"]), "damageInflict" (["fire"]) and, for a cantrip,
/// "scalingLevelDice", the dice at each character level its damage grows at.
/// </summary>
internal static partial class SpellAttackReader
{
    /// <summary>"melee" or "ranged" for a spell that makes a spell attack, else null.</summary>
    public static string? AttackType(JsonObject spell) =>
        spell.Arr("spellAttack").Select(node => node.AsString()).FirstOrDefault() switch
        {
            "M" => "melee",
            "R" => "ranged",
            _ => null,
        };

    /// <summary>The ability of the first saving throw the spell calls for, by canonical id ("DEX"), else null.</summary>
    public static string? SaveAbilityId(JsonObject spell) =>
        spell.Arr("savingThrow").Select(node => node.AsString()).FirstOrDefault() is { } ability && AbilityScore.TryNormalize(ability, out var id)
            ? id
            : null;

    /// <summary>What its damage is ("fire"), the ones to pick from joined ("acid/cold/fire"), else null.</summary>
    public static string? DamageType(JsonObject spell)
    {
        var types = spell.Arr("damageInflict").Select(node => node.AsString()).OfType<string>().ToList();

        return types.Count == 0 ? null : string.Join('/', types).ToLowerInvariant();
    }

    /// <summary>
    /// A cantrip's damage dice at each of <see cref="CantripDamage.TierLevels"/>, from the first
    /// scaling entry that gives plain dice at all of them (Toll the Dead's first is its usual
    /// damage, its second the one against a wounded creature). A cantrip with no scaling entry
    /// (Eldritch Blast, whose beams are separate attacks) keeps the first dice of its text at every
    /// level. Empty for a levelled spell, for no damage, and for a cantrip with damage that only
    /// starts at level 5 (Booming Blade, Green-Flame Blade, the 2024 True Strike): that adds to a
    /// weapon's hit, and weapons are not modelled yet.
    /// </summary>
    public static List<string> CantripDice(JsonObject spell)
    {
        if (spell.Int("level") != 0)
        {
            return [];
        }

        var scaling = spell["scalingLevelDice"] switch
        {
            JsonObject entry => [entry],
            JsonArray entries => entries.OfType<JsonObject>().ToList(),
            _ => [],
        };

        if (scaling.Count == 0)
        {
            return DamageInText(spell["entries"]?.ToJsonString()) is { } dice
                ? CantripDamage.TierLevels.Select(_ => dice).ToList()
                : [];
        }

        if (scaling.Exists(entry => entry["scaling"].Str("1") == null))
        {
            return [];
        }

        return scaling
            .Select(entry => CantripDamage.TierLevels
                .Select(level => entry["scaling"].Str(level.ToString(CultureInfo.InvariantCulture)))
                .ToList())
            .FirstOrDefault(dice => dice.TrueForAll(d => d != null && PlainDice().IsMatch(d)))
            ?.Select(d => d!)
            .ToList() ?? [];
    }

    private static string? DamageInText(string? text) =>
        text != null && DamageTag().Match(text) is { Success: true } match ? match.Groups["dice"].Value : null;

    [GeneratedRegex("^[0-9]+d[0-9]+$", RegexOptions.CultureInvariant)]
    private static partial Regex PlainDice();

    [GeneratedRegex("\\{@damage (?<dice>[0-9]+d[0-9]+)", RegexOptions.CultureInvariant)]
    private static partial Regex DamageTag();
}
