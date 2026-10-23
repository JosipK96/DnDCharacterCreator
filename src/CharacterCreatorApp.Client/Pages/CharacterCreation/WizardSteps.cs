namespace CharacterCreatorApp.Client.Pages.CharacterCreation;

/// <summary>
/// The wizard's steps, in order. The progress bar, each step's heading and each "Next" button
/// take their number and name from this list. They used to be written into every step, and when
/// Languages became step 5 the headings and buttons after it kept their old numbers and names.
/// </summary>
public static class WizardSteps
{
    public static readonly IReadOnlyList<string> Names =
    [
        "Name", "Race", "Class", "Background", "Languages",
        "Ability Scores", "Proficiencies", "Improvements", "Spells", "Review",
    ];

    // The steps an edit marks as needing a pick, by the names the API gives them (DraftChoice)
    private static readonly Dictionary<string, string> StepsOfChoices = new()
    {
        ["race"] = "Race",
        ["background"] = "Background",
        ["languages"] = "Languages",
        ["ability-scores"] = "Ability Scores",
        ["proficiencies"] = "Proficiencies",
        ["improvements"] = "Improvements",
        ["spells"] = "Spells",
    };

    public static int Count => Names.Count;

    /// <summary>A step's name from its number, counting from 1.</summary>
    public static string NameOf(int step) => Names[step - 1];

    /// <summary>
    /// The numbers of the steps a draft's ChoicesNeeded names: what an edit of an older character
    /// could not fill in. A name the client does not know marks nothing.
    /// </summary>
    public static IEnumerable<int> StepsOf(IEnumerable<string> choicesNeeded) =>
        choicesNeeded
            .Where(StepsOfChoices.ContainsKey)
            .Select(choice => Names.ToList().IndexOf(StepsOfChoices[choice]) + 1)
            .Order();
}
