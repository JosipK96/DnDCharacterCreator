namespace CharacterCreatorApp.Client.Pages.CharacterCreation;

/// <summary>
/// The wizard's steps, in order. The progress bar, each step's heading and each "Next" button
/// take their number and name from this list. They used to be written into every step, and when
/// Languages became step 5 the headings and buttons after it kept their old numbers and names.
/// </summary>
public static class WizardSteps
{
    /// <summary>
    /// The step a level-up opens with, and that a new character or an edit above level 1 takes
    /// after Spells (<see cref="CreationOf"/>). It is numbered after Review because a level-1
    /// character never visits it: its hit die is the maximum.
    /// </summary>
    public const int HitPoints = 11;

    private const string HitPointsName = "Hit Points";

    /// <summary>The steps of a new character, and of an edit, numbered from 1.</summary>
    public static readonly IReadOnlyList<string> Names =
    [
        "Name", "Race", "Class", "Background", "Languages",
        "Ability Scores", "Proficiencies", "Improvements", "Spells", "Review",
    ];

    // The steps a draft marks or a level-up asks for, by the names the API gives them (DraftChoice)
    private static readonly Dictionary<string, string> StepsOfChoices = new()
    {
        ["race"] = "Race",
        ["subclass"] = "Class",
        ["background"] = "Background",
        ["languages"] = "Languages",
        ["ability-scores"] = "Ability Scores",
        ["proficiencies"] = "Proficiencies",
        ["improvements"] = "Improvements",
        ["spells"] = "Spells",
        ["hit-points"] = HitPointsName,
    };

    public static int Count => Names.Count;

    /// <summary>Review, the last step of every flow.</summary>
    public static int Review => Count;

    /// <summary>A level-1 character's steps, new or edited: every one in order, with no hit points to roll.</summary>
    public static IReadOnlyList<int> Creation { get; } = [.. Enumerable.Range(1, Count)];

    /// <summary>
    /// A new character's steps, and an edit's, from the draft's HitDieLevels
    /// (CharacterDraftDetailsApiResponse): every one in order, and Hit Points between Spells and
    /// Review when the draft has a level to roll for, as every character above level 1 does.
    /// </summary>
    public static IReadOnlyList<int> CreationOf(IReadOnlyCollection<int> hitDieLevels) =>
        hitDieLevels.Count == 0 ? Creation : [.. Creation.Where(step => step != Review), HitPoints, Review];

    /// <summary>A step's name from its number, counting from 1.</summary>
    public static string NameOf(int step) => step == HitPoints ? HitPointsName : Names[step - 1];

    /// <summary>
    /// The numbers of the steps a draft's ChoicesNeeded names: what an edit of an older character
    /// could not fill in, or what a level-up has yet to pick. A name the client does not know
    /// marks nothing.
    /// </summary>
    public static IEnumerable<int> StepsOf(IEnumerable<string> choicesNeeded) =>
        choicesNeeded
            .Where(StepsOfChoices.ContainsKey)
            .Select(choice => NumberOf(StepsOfChoices[choice]))
            .Distinct()
            .Order();

    /// <summary>
    /// The steps a level-up goes through (CharacterDraftDetailsApiResponse.LevelUpChoices): its hit
    /// points first, then the steps the new level brings in their usual order, then Review.
    /// </summary>
    public static IReadOnlyList<int> LevelUpOf(IEnumerable<string> levelUpChoices) =>
        [HitPoints, .. StepsOf(levelUpChoices).Where(step => step != HitPoints), Review];

    private static int NumberOf(string name) => name == HitPointsName ? HitPoints : Names.ToList().IndexOf(name) + 1;
}
