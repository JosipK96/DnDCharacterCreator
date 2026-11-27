namespace CharacterCreatorApp.Client.Pages.CharacterCreation.Components.AbilityScores;

/// <summary>How the player gets the six scores to hand out.</summary>
public enum AbilityScoreMethod
{
    None,
    StandardArray,
    PointBuy,
    RollDice,
}

/// <summary>The methods by the ids the API knows: it checks the scores against the method.</summary>
public static class AbilityScoreMethods
{
    /// <summary>The standard array's six scores.</summary>
    public static IReadOnlyList<int> StandardArray { get; } = [15, 14, 13, 12, 10, 8];

    public static string IdOf(AbilityScoreMethod method) => method switch
    {
        AbilityScoreMethod.StandardArray => "standard-array",
        AbilityScoreMethod.PointBuy => "point-buy",
        AbilityScoreMethod.RollDice => "rolled",
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, "No method was chosen."),
    };

    /// <summary>The method a draft saved; None for one the client does not know.</summary>
    public static AbilityScoreMethod FromId(string methodId) => methodId switch
    {
        "standard-array" => AbilityScoreMethod.StandardArray,
        "point-buy" => AbilityScoreMethod.PointBuy,
        "rolled" => AbilityScoreMethod.RollDice,
        _ => AbilityScoreMethod.None,
    };
}
