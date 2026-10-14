using System.Text;

namespace CharacterCreatorApp.SeedDataGenerator;

internal static class Slug
{
    /// <summary>
    /// "Path of the Totem Warrior" becomes "path-of-the-totem-warrior". Apostrophes go without a
    /// separator ("Tinker's Tools" is "tinkers-tools"), and a closing parenthesis as well as any
    /// other run of punctuation or spaces becomes one separator.
    /// </summary>
    public static string Of(string text, char separator = '-')
    {
        var slug = new StringBuilder(text.Length);
        var pendingSeparator = false;

        foreach (var c in text.ToLowerInvariant())
        {
            if (c is '\'' or '’')
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(c))
            {
                if (pendingSeparator && slug.Length > 0)
                {
                    slug.Append(separator);
                }

                slug.Append(c);
                pendingSeparator = false;
                continue;
            }

            pendingSeparator = true;
        }

        return slug.ToString();
    }

    /// <summary>"Purple Dragon Knight (Banneret)" without its " (Banneret)".</summary>
    public static string WithoutParenthetical(string text)
    {
        var open = text.IndexOf(" (", StringComparison.Ordinal);
        return open < 0 ? text : text[..open];
    }
}
