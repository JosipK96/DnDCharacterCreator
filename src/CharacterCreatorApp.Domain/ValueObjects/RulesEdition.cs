namespace CharacterCreatorApp.Domain.ValueObjects;

/// <summary>
/// The two versions of the fifth-edition rules the seed data carries: the 2014 Player's Handbook
/// and its 2024 revision. A character is built with one of them, chosen in the first step, and is
/// offered only that edition's classes and the rest. What exists in both keeps its book's name in
/// each and has the edition in its id: "Alert" is "alert_2014" and "alert_2024".
/// </summary>
public static class RulesEdition
{
    public const string Edition2014 = "2014";
    public const string Edition2024 = "2024";

    public static IReadOnlyList<string> All { get; } = [Edition2014, Edition2024];

    public static bool IsKnown(string? edition) => edition is Edition2014 or Edition2024;

    /// <summary>
    /// The code of the edition's Player's Handbook, the book every character of it may use:
    /// "PHB" for 2014, "XPHB" for 2024, as Books.json in the seed data flags them.
    /// </summary>
    public static string PlayersHandbook(string edition) => edition switch
    {
        Edition2014 => "PHB",
        Edition2024 => "XPHB",
        _ => throw new ArgumentException($"Unknown rules edition: {edition}", nameof(edition)),
    };
}
