using System.Globalization;
using System.Text.RegularExpressions;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Reduces 5etools' inline markup to the text a reader of the book would see. 5etools'
/// descriptions point at other entries in place: "{@damage 1d8}", "{@spell fireball|phb}",
/// "{@creature goblin|mm|goblins}". Nothing that shows the data renders those links, so the
/// markup is taken out once, while the seed files are written, instead of by every page and
/// document that prints a description; SeedDataIntegrityTests checks none is left in them.
/// </summary>
internal static partial class FiveEToolsMarkup
{
    public static string ToPlainText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        // Innermost tags first: a tag's text can hold another tag ("{@i 2nd-level {@spell augury}}"),
        // and the outer one only matches once the inner one is gone. Every pass removes at least
        // one tag's braces, so this ends.
        while (InnermostTag().IsMatch(text))
        {
            text = InnermostTag().Replace(text, match => Render(match.Groups["tag"].Value, match.Groups["text"].Value));
        }

        return text;
    }

    private static string Render(string tag, string text)
    {
        var parts = text.Split('|');

        return tag.ToUpperInvariant() switch
        {
            // A roll shows the roll itself, unless the tag gives the text to show in its place
            "DICE" or "DAMAGE" => Part(parts, 1) ?? parts[0],
            "D20" or "HIT" => Part(parts, 1) ?? Signed(parts[0]),
            "DC" => $"DC {Part(parts, 1) ?? parts[0]}",
            "CHANCE" => Part(parts, 1) ?? $"{parts[0]} percent",

            // Tags whose first part is the text and the rest says where it points
            "BOOK" or "ADVENTURE" or "FILTER" or "5ETOOLS" => parts[0],

            // Links that keep their display text further along than the usual third part
            "QUICKREF" => Part(parts, 4) ?? parts[0],
            "CLASSFEATURE" => Part(parts, 5) ?? parts[0],
            "SUBCLASSFEATURE" => Part(parts, 7) ?? parts[0],

            // Everything else, formatting tags such as {@i} included, is "name|source|display text"
            _ => Part(parts, 2) ?? parts[0],
        };
    }

    private static string? Part(string[] parts, int index) =>
        index < parts.Length && parts[index].Length > 0 ? parts[index] : null;

    private static string Signed(string number) =>
        int.TryParse(number, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) && value >= 0
            ? $"+{value}"
            : number;

    [GeneratedRegex(@"\{@(?<tag>\w+)(?:\s(?<text>[^{}]*))?\}")]
    private static partial Regex InnermostTag();
}
