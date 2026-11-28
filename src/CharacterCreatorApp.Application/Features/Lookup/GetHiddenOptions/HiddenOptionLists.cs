namespace CharacterCreatorApp.Application.Features.Lookup.GetHiddenOptions;

/// <summary>
/// The wizard's lists that the draft's books filter, by the names GetHiddenOptionsQuery.List takes.
/// The spells step's list is not one: GET spell-allowance counts what it leaves out, with the list.
/// </summary>
public static class HiddenOptionLists
{
    public const string Races = "races";
    public const string Classes = "classes";
    public const string Subclasses = "subclasses";
    public const string Backgrounds = "backgrounds";
    public const string Feats = "feats";

    public static readonly IReadOnlyList<string> All = [Races, Classes, Subclasses, Backgrounds, Feats];

    /// <summary>Whether the list is one class's: its subclasses.</summary>
    public static bool NeedsClass(string? list) => list is Subclasses;
}
