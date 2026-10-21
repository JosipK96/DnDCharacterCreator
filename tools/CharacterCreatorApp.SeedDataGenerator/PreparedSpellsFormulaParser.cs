using System.Globalization;
using System.Text.RegularExpressions;

using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Reads how many spells a 2014 class prepares from 5etools' "preparedSpells" formula: a cleric's
/// "&lt;$level$&gt; + &lt;$wis_mod$&gt;", a paladin's "&lt;$level$&gt; / 2 + &lt;$cha_mod$&gt;". The app
/// keeps only the level's divisor; the ability is the class's spellcasting ability, which the
/// formula must name too.
/// </summary>
internal static partial class PreparedSpellsFormulaParser
{
    /// <returns>1 or 2, what the level is divided by (rounded down).</returns>
    public static int ParseLevelDivisor(string formula, string spellcastingAbilityId)
    {
        var match = Formula().Match(formula);

        if (!match.Success)
        {
            throw new InvalidOperationException($"The prepared spells formula \"{formula}\" is not a level, maybe divided, plus a modifier.");
        }

        if (AbilityScore.Normalize(match.Groups["ability"].Value) != AbilityScore.Normalize(spellcastingAbilityId))
        {
            throw new InvalidOperationException(
                $"The prepared spells formula \"{formula}\" adds another ability's modifier than the spellcasting ability, {spellcastingAbilityId}.");
        }

        return match.Groups["divisor"].Success ? int.Parse(match.Groups["divisor"].Value, CultureInfo.InvariantCulture) : 1;
    }

    [GeneratedRegex(@"^\s*<\$level\$>\s*(?:/\s*(?<divisor>\d+)\s*)?\+\s*<\$(?<ability>[a-z]{3})_mod\$>\s*$")]
    private static partial Regex Formula();
}
