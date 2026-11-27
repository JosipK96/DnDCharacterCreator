using CharacterCreatorApp.Contracts.Responses.Characters;

namespace CharacterCreatorApp.Client.Pages.CharacterCreation;

/// <summary>
/// Where the wizard is, and where each button takes the player: which steps a draft goes through
/// (<see cref="WizardSteps"/> numbers them), which step opens, and what Next, Back, a step picked on
/// an edit's map, and a step's note on the unticked books do. <c>CreateCharacter</c> shows the step
/// this says and calls the API; this holds no service and draws nothing.
/// </summary>
public sealed class WizardNavigator
{
    private HashSet<int> _stepsNeedingPick = [];

    // The step that sent the player to step 1 to tick books, which step 1's Next returns to
    private int? _returnStep;

    public int CurrentStep { get; private set; } = 1;

    /// <summary>The steps this draft goes through: all of them, or a level-up's few (<see cref="WizardSteps.LevelUpOf"/>).</summary>
    public IReadOnlyList<int> Flow { get; private set; } = WizardSteps.Creation;

    /// <summary>The steps an edit's map marks: what an older character could not fill in, or a level-up has yet to pick.</summary>
    public IReadOnlySet<int> StepsNeedingPick => _stepsNeedingPick;

    public bool IsLevelUp { get; private set; }

    /// <summary>A level-up has no step 1, so its notes on the unticked books offer no way there.</summary>
    public bool CanChangeBooks => Flow.Contains(1);

    /// <summary>
    /// Where a new character's draft opens: the first of Name, Race, Class, Background and Ability
    /// Scores it has not saved, and Proficiencies once it has them all.
    /// </summary>
    public static int FirstStepToFill(CharacterDraftDetailsApiResponse draft)
    {
        if (string.IsNullOrEmpty(draft.Name) || !draft.Level.HasValue)
        {
            return 1;
        }

        if (string.IsNullOrEmpty(draft.RaceId))
        {
            return 2;
        }

        if (string.IsNullOrEmpty(draft.ClassId))
        {
            return 3;
        }

        if (string.IsNullOrEmpty(draft.BackgroundId))
        {
            return 4;
        }

        if (draft.BaseAbilityScores == null || draft.BaseAbilityScores.Count < 6)
        {
            return 6;
        }

        return 7;
    }

    /// <summary>
    /// Opens the wizard on a draft named in the address: a new character's at its first step to
    /// fill, an edit's (a level-up's too) at the first step that needs a pick, if any.
    /// </summary>
    public void Open(CharacterDraftDetailsApiResponse draft)
    {
        _stepsNeedingPick = [.. WizardSteps.StepsOf(draft.ChoicesNeeded)];
        IsLevelUp = draft.LevelUpChoices.Count != 0;
        Flow = IsLevelUp ? WizardSteps.LevelUpOf(draft.LevelUpChoices) : WizardSteps.CreationOf(draft.HitDieLevels);

        // An edit has every step filled in: it opens at the first one that needs a pick, if any
        CurrentStep = draft.CharacterId != null
            ? Flow.Where(_stepsNeedingPick.Contains).DefaultIfEmpty(Flow[0]).First()
            : FirstStepToFill(draft);
    }

    /// <summary>Resumes the player's unfinished new character at its first step to fill.</summary>
    public void Resume(CharacterDraftDetailsApiResponse draft)
    {
        CurrentStep = FirstStepToFill(draft);
        Flow = WizardSteps.CreationOf(draft.HitDieLevels);
    }

    /// <summary>
    /// After step 1 is saved: the level may have changed, and with it the levels whose hit die the
    /// flow asks about. Step 1 goes back to the step that sent the player to tick books, when
    /// nothing it saved went; another edition or an unticked book clears choices, and then the
    /// steps are walked again in order.
    /// </summary>
    /// <param name="hitDieLevels">The saved draft's HitDieLevels.</param>
    /// <param name="keptChoices">Whether the edition stayed and no book was unticked.</param>
    public void AfterStepOne(IReadOnlyCollection<int> hitDieLevels, bool keptChoices)
    {
        var back = _returnStep;

        Flow = WizardSteps.CreationOf(hitDieLevels);
        _returnStep = null;

        if (back is { } step && keptChoices)
        {
            _stepsNeedingPick.Remove(1);
            CurrentStep = step;
            return;
        }

        Next(1);
    }

    /// <summary>On to the step after this one in the flow. A step the player has been through needs no pick any more, even one that had nothing to save.</summary>
    public void Next(int fromStep)
    {
        _stepsNeedingPick.Remove(fromStep);
        CurrentStep = Flow[Flow.ToList().IndexOf(fromStep) + 1];
    }

    /// <summary>
    /// Back to the step before this one in the flow. Returns false, and stays, from the flow's
    /// first step: back from there leaves the wizard, as cancelling does.
    /// </summary>
    public bool Back(int fromStep)
    {
        var position = Flow.ToList().IndexOf(fromStep);

        if (position <= 0)
        {
            return false;
        }

        CurrentStep = Flow[position - 1];
        return true;
    }

    /// <summary>A step's note on what the unticked books hold sends the player to step 1, and back after.</summary>
    public void ChangeBooks()
    {
        _returnStep = CurrentStep;
        CurrentStep = 1;
    }

    /// <summary>A step picked on an edit's map: step 1 no longer returns to the one that sent the player there.</summary>
    public void Select(int step)
    {
        _returnStep = null;
        CurrentStep = step;
    }
}
