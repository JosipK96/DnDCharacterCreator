namespace CharacterCreatorApp.Client.Pages.CharacterCreation.Components.AbilityScores;

/// <summary>
/// The six scores the player rolled or generated, and which ability each one went to. A score is
/// known by its place in <see cref="Scores"/>, not its value, since two rolls can be the same number.
/// The player clicks a score (<see cref="Select"/>), then an ability (<see cref="AssignSelectedTo"/>).
/// </summary>
public sealed class ScoreAssignment
{
    private const int Unassigned = -1;

    private readonly Dictionary<string, int> _scoreIndexByAbility = AbilityNames.Ids.ToDictionary(id => id, _ => Unassigned);

    /// <summary>
    /// The scores to hand out. The point buy and dice components fill this same list in place
    /// before they report it, so the step passes it to them as it is.
    /// </summary>
    public List<int> Scores { get; private set; } = [];

    /// <summary>The place of the score the player clicked, waiting for an ability; null when none is.</summary>
    public int? SelectedIndex { get; private set; }

    /// <summary>The score the player clicked, null when none is.</summary>
    public int? SelectedScore => SelectedIndex is { } index && index >= 0 && index < Scores.Count ? Scores[index] : null;

    public bool AllAssigned => _scoreIndexByAbility.Values.All(index => index >= 0);

    /// <summary>Takes the scores as rolled or bought, keeping whatever was assigned.</summary>
    public void UseScores(IEnumerable<int> scores) => Scores = scores.ToList();

    /// <summary>Drops the scores, the click and every assignment, for another method or a reroll.</summary>
    public void Reset()
    {
        Scores.Clear();
        ClearAssignments();
    }

    /// <summary>Takes back every assignment and the click, keeping the scores.</summary>
    public void ClearAssignments()
    {
        SelectedIndex = null;

        foreach (var abilityId in _scoreIndexByAbility.Keys.ToList())
        {
            _scoreIndexByAbility[abilityId] = Unassigned;
        }
    }

    /// <summary>
    /// The saved scores back on their abilities, as the draft holds them: STR gets the first score,
    /// DEX the second, and so on, which is the order <see cref="ToScores"/> sends them in.
    /// </summary>
    public void Restore(IReadOnlyDictionary<string, int> savedScores)
    {
        Scores = AbilityNames.Ids.Select(id => savedScores[id]).ToList();
        SelectedIndex = null;

        for (var i = 0; i < AbilityNames.Ids.Count; i++)
        {
            _scoreIndexByAbility[AbilityNames.Ids[i]] = i;
        }
    }

    public bool IsAssigned(int scoreIndex) => _scoreIndexByAbility.ContainsValue(scoreIndex);

    /// <summary>Clicks a score; one already assigned cannot be clicked.</summary>
    public void Select(int scoreIndex)
    {
        if (IsAssigned(scoreIndex))
        {
            return;
        }

        SelectedIndex = scoreIndex;
    }

    /// <summary>Gives the clicked score to the ability; nothing happens with no score clicked.</summary>
    public void AssignSelectedTo(string abilityId)
    {
        if (SelectedIndex is not { } index)
        {
            return;
        }

        _scoreIndexByAbility[abilityId] = index;
        SelectedIndex = null;
    }

    /// <summary>Takes the ability's score back, so it can be given to another.</summary>
    public void Unassign(string abilityId)
    {
        _scoreIndexByAbility[abilityId] = Unassigned;
        SelectedIndex = null;
    }

    /// <summary>The score the ability holds, 0 when it holds none.</summary>
    public int ScoreOf(string abilityId)
    {
        var index = _scoreIndexByAbility.GetValueOrDefault(abilityId, Unassigned);
        return index >= 0 && index < Scores.Count ? Scores[index] : 0;
    }

    /// <summary>What the step saves: each ability's score by its id ("STR").</summary>
    public Dictionary<string, int> ToScores() => AbilityNames.Ids.ToDictionary(id => id, ScoreOf);
}
