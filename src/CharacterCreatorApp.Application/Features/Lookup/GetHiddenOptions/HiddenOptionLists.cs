namespace CharacterCreatorApp.Application.Features.Lookup.GetHiddenOptions;

/// <summary>The wizard's lists that the draft's books filter, by the names GetHiddenOptionsQuery.List takes.</summary>
public static class HiddenOptionLists
{
    public const string Races = "races";
    public const string Classes = "classes";
    public const string Subclasses = "subclasses";
    public const string Backgrounds = "backgrounds";
    public const string Feats = "feats";
    public const string Spells = "spells";

    public static readonly IReadOnlyList<string> All = [Races, Classes, Subclasses, Backgrounds, Feats, Spells];

    /// <summary>Whether the list is one class's: its subclasses or its spells.</summary>
    public static bool NeedsClass(string? list) => list is Subclasses or Spells;
}
