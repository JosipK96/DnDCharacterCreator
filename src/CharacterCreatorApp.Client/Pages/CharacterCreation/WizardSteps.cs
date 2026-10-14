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
        "Ability Scores", "Proficiencies", "Spells", "Improvements", "Review",
    ];

    public static int Count => Names.Count;

    /// <summary>A step's name from its number, counting from 1.</summary>
    public static string NameOf(int step) => Names[step - 1];
}
