namespace CharacterCreatorApp.Domain.ValueObjects;

/// <summary>
/// How a character's six base ability scores were generated, and which scores each way can
/// produce. The rules are the same in both editions: the standard array, point buy with 27
/// points over scores of 8 to 15, or rolling 4d6 and dropping the lowest die, which gives 3 to 18.
/// </summary>
public static class AbilityScoreMethod
{
    public const string StandardArray = "standard-array";
    public const string PointBuy = "point-buy";
    public const string Rolled = "rolled";

    public const int PointBuyBudget = 27;

    public static IReadOnlyList<string> All { get; } = [StandardArray, PointBuy, Rolled];

    private static readonly int[] StandardArrayScores = [15, 14, 13, 12, 10, 8];

    // What each score from 8 to 15 costs in point buy
    private static readonly Dictionary<int, int> PointBuyCosts = new()
    {
        [8] = 0, [9] = 1, [10] = 2, [11] = 3, [12] = 4, [13] = 5, [14] = 7, [15] = 9,
    };

    public static bool IsKnown(string? method) => method is StandardArray or PointBuy or Rolled;

    /// <summary>True when these six scores are ones the method can produce.</summary>
    public static bool Allows(string? method, IReadOnlyCollection<int> scores)
    {
        if (scores.Count != StandardArrayScores.Length)
        {
            return false;
        }

        return method switch
        {
            StandardArray => scores.Order().SequenceEqual(StandardArrayScores.Order()),
            PointBuy => scores.All(PointBuyCosts.ContainsKey) && scores.Sum(s => PointBuyCosts[s]) <= PointBuyBudget,
            Rolled => scores.All(s => s is >= 3 and <= 18),
            _ => false,
        };
    }
}
