using System.Globalization;
using System.Text.RegularExpressions;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Reads the school limit on a 2014 Eldritch Knight's or Arcane Trickster's spells from its
/// Spellcasting feature. 5etools records only that the subclass picks from the Wizard's list, so
/// the limit is in the text alone: "it must be an abjuration or evocation spell, unless you're
/// replacing the spell you gained at 3rd, 8th, 14th, or 20th level from any school of magic".
/// The 2024 versions have no limit and say nothing of the kind.
/// </summary>
internal static partial class SpellSchoolLimitParser
{
    private static readonly string[] Schools =
        ["abjuration", "conjuration", "divination", "enchantment", "evocation", "illusion", "necromancy", "transmutation"];

    /// <returns>The schools, capitalised as the spell files have them, and the levels that each
    /// give a spell from any school; or null when the text names no limit.</returns>
    public static (List<string> Schools, List<int> AnySchoolLevels)? Parse(string featureText)
    {
        var schools = MustBeSchools().Match(featureText);

        if (!schools.Success
            || !Schools.Contains(schools.Groups[1].Value, StringComparer.OrdinalIgnoreCase)
            || !Schools.Contains(schools.Groups[2].Value, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        var anySchool = AnySchoolLevels().Match(featureText);

        if (!anySchool.Success)
        {
            throw new InvalidOperationException(
                $"A spellcasting feature limits its spells to {schools.Groups[1].Value} and {schools.Groups[2].Value} but names no level for a spell from any school.");
        }

        var levels = Ordinal().Matches(anySchool.Groups[1].Value)
            .Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))
            .ToList();

        return ([Capitalise(schools.Groups[1].Value), Capitalise(schools.Groups[2].Value)], levels);
    }

    private static string Capitalise(string school) =>
        char.ToUpperInvariant(school[0]) + school[1..].ToLowerInvariant();

    [GeneratedRegex(@"must be an? (\w+) or (\w+) spell", RegexOptions.IgnoreCase)]
    private static partial Regex MustBeSchools();

    [GeneratedRegex(@"gained at ((?:\d+(?:st|nd|rd|th),? (?:or )?)+)level from any school", RegexOptions.IgnoreCase)]
    private static partial Regex AnySchoolLevels();

    [GeneratedRegex(@"(\d+)(?:st|nd|rd|th)")]
    private static partial Regex Ordinal();
}
