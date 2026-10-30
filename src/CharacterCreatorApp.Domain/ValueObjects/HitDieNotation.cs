using System.Globalization;

namespace CharacterCreatorApp.Domain.ValueObjects;

/// <summary>
/// How the class data writes a hit die: "1d10", "d10" or "10". The one place it is read; a value
/// it cannot read is an error, never a d8, so a typo in the data cannot quietly change every
/// character of the class.
/// </summary>
public static class HitDieNotation
{
    /// <summary>The die's size: 10 for "1d10", "d10" or "10".</summary>
    public static int SizeOf(string hitDie) =>
        TryParse(hitDie, out var size)
            ? size
            : throw new FormatException($"'{hitDie}' is not a hit die: expected '1d10', 'd10' or '10'.");

    /// <summary>Whether the text is a hit die; the size is 0 when it is not.</summary>
    public static bool TryParse(string? hitDie, out int size)
    {
        size = 0;

        if (string.IsNullOrWhiteSpace(hitDie))
        {
            return false;
        }

        var separatorIndex = hitDie.IndexOf('d', StringComparison.OrdinalIgnoreCase);
        var sizePart = separatorIndex >= 0 ? hitDie[(separatorIndex + 1)..] : hitDie;

        return int.TryParse(sizePart, NumberStyles.None, CultureInfo.InvariantCulture, out size) && size > 0;
    }

    /// <summary>The die's fixed value, which a level takes in place of a roll: its average rounded up, a d10's 6.</summary>
    public static int FixedValueOf(string hitDie) => (SizeOf(hitDie) / 2) + 1;
}
