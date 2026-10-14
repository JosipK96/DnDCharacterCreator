namespace CharacterCreatorApp.Domain.DomainModels.Proficiencies;

/// <summary>
/// Tools, instruments or gaming sets a race, background or class lets the player pick: Count of
/// Options, by proficiency id. A dwarf's one of smith's tools, brewer's supplies or mason's
/// tools; a bard's three instruments; a Criminal's one gaming set.
/// </summary>
public sealed class ToolChoice
{
    public int Count { get; set; }

    public List<string> Options { get; set; } = [];

    /// <summary>Whether these picks fill the choice: Count different ids, every one an option.</summary>
    public bool Allows(IReadOnlyCollection<string> picks) =>
        picks.Count == Count && picks.Distinct().Count() == Count && picks.All(Options.Contains);
}
