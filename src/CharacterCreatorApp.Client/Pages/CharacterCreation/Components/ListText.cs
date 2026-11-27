namespace CharacterCreatorApp.Client.Pages.CharacterCreation.Components;

/// <summary>How a step's key panel writes a list of names: the class's armor, the background's tools.</summary>
public static class ListText
{
    /// <summary>"Light armor, Shields", or "None" for an empty list.</summary>
    public static string OrNone(IEnumerable<string> names)
    {
        var list = string.Join(", ", names);
        return list.Length == 0 ? "None" : list;
    }
}
