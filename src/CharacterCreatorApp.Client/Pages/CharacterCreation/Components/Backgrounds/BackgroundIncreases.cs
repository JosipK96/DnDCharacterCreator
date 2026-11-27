namespace CharacterCreatorApp.Client.Pages.CharacterCreation.Components.Backgrounds;

/// <summary>
/// A 2024 background's ability increases as the player picks them: +2 to one of its three
/// abilities and +1 to another, or +1 to each of the three.
/// </summary>
public sealed class BackgroundIncreases
{
    public bool OneToEach { get; set; }

    public string? PlusTwo { get; set; }

    public string? PlusOne { get; set; }

    public void Clear()
    {
        OneToEach = false;
        PlusTwo = null;
        PlusOne = null;
    }

    /// <summary>The picks back from what the draft saved, by ability id ("STR").</summary>
    public void Restore(IReadOnlyDictionary<string, int> saved)
    {
        OneToEach = saved.Count > 2 && saved.Values.All(points => points == 1);
        PlusTwo = saved.FirstOrDefault(i => i.Value == 2).Key;
        PlusOne = OneToEach ? null : saved.FirstOrDefault(i => i.Value == 1).Key;
    }

    /// <summary>
    /// What the step saves, from the background's three abilities: nothing for a background with
    /// none to raise, and null while a 2024 background's are unfinished.
    /// </summary>
    public Dictionary<string, int>? For(IReadOnlyList<string> options)
    {
        if (options.Count == 0)
        {
            return [];
        }

        if (OneToEach)
        {
            return options.ToDictionary(ability => ability, _ => 1);
        }

        return string.IsNullOrEmpty(PlusTwo) || string.IsNullOrEmpty(PlusOne) || PlusTwo == PlusOne
            ? null
            : new Dictionary<string, int> { [PlusTwo] = 2, [PlusOne] = 1 };
    }
}
