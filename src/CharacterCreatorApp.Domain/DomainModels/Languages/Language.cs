using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Domain.DomainModels.Languages;

public class Language
{
    public const string StandardType = "standard";

    /// <summary>The PHB's exotic languages, and the ones the 2024 book calls rare.</summary>
    public const string ExoticType = "exotic";

    /// <summary>
    /// The type of Druidic and Thieves' Cant: PHB languages a druid or rogue learns from a class
    /// feature, never one a race or background lets the player pick.
    /// </summary>
    public const string SecretType = "secret";

    private static readonly string[] Choosable2014 = [StandardType, ExoticType];
    private static readonly string[] Choosable2024 = [StandardType];

    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public string? Script { get; set; }
    public required string Source { get; set; }

    /// <summary>The book whose languages a character of this rules edition learns: its Player's Handbook.</summary>
    public static string SourceFor(string edition) => RulesEdition.PlayersHandbook(edition);

    /// <summary>
    /// The types a player picks a new character's languages from: any PHB language but the
    /// secret ones in 2014; only the Standard Languages table in 2024, where the rare ones
    /// (Druidic, Thieves' Cant, Abyssal) come from class features and the like.
    /// </summary>
    public static IReadOnlyList<string> ChoosableTypes(string edition) =>
        edition == RulesEdition.Edition2024 ? Choosable2024 : Choosable2014;
}
