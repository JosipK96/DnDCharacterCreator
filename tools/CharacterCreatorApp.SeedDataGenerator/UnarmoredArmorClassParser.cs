using System.Globalization;
using System.Text.RegularExpressions;

using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Reads the armour class a class or subclass feature gives without armour from its text, since
/// 5etools records it nowhere else. The 2014 books write "your AC equals 10 + your Dexterity
/// modifier + your Constitution modifier" (or "13 + your Dexterity modifier"), the 2024 one "your
/// base Armor Class equals 10 plus your Dexterity and Wisdom modifiers". The feature allows a
/// shield unless it says the character must not be wielding one, as a monk's does.
/// </summary>
internal static partial class UnarmoredArmorClassParser
{
    /// <returns>The unarmoured armour class, or null when the text gives none.</returns>
    public static UnarmoredArmorClass? Parse(string featureText)
    {
        var match = Edition2014().Match(featureText) is { Success: true } match2014 ? match2014 : Edition2024().Match(featureText);

        if (!match.Success)
        {
            return null;
        }

        var ability = match.Groups["ability"];

        return new UnarmoredArmorClass
        {
            Base = int.Parse(match.Groups["base"].Value, CultureInfo.InvariantCulture),
            AbilityId = ability.Success ? AbilityScore.Normalize(ability.Value) : null,
            AllowsShield = !NoShield().IsMatch(featureText)
        };
    }

    [GeneratedRegex(@"\b(?:AC|Armor Class) equals (?<base>\d+) \+ your Dexterity modifier(?: \+ your (?<ability>\w+) modifier)?", RegexOptions.IgnoreCase)]
    private static partial Regex Edition2014();

    [GeneratedRegex(@"\bArmor Class equals (?<base>\d+) plus your Dexterity and (?<ability>\w+) modifiers", RegexOptions.IgnoreCase)]
    private static partial Regex Edition2024();

    [GeneratedRegex(@"\b(?:not|or) wielding a shield", RegexOptions.IgnoreCase)]
    private static partial Regex NoShield();
}
