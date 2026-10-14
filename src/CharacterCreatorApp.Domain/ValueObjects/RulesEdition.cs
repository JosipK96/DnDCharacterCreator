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
}
