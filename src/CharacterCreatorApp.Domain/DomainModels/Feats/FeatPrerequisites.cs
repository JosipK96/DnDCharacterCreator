using System.Globalization;

using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Domain.DomainModels.Feats;

/// <summary>
/// What a character needs before taking a feat. Each list names alternatives, so one entry is
/// enough ("Strength or Dexterity 13", "an elf or a half-elf"), and every list that has entries
/// has to be met. An empty list asks for nothing. The race, subrace and size lists are one
/// requirement between them: Squat Nimbleness takes a dwarf or any Small race.
/// </summary>
public class FeatPrerequisites
{
    public int? MinimumLevel { get; set; }

    public List<AbilityMinimum> AbilityMinimums { get; set; } = [];

    public List<string> RaceIds { get; set; } = [];

    /// <summary>
    /// Subraces, one of which the character needs: Wood Elf Magic is for a wood elf, not any elf.
    /// </summary>
    public List<string> SubraceIds { get; set; } = [];

    /// <summary>Race sizes, "Small" or "Medium", one of which the character's race needs.</summary>
    public List<string> Sizes { get; set; } = [];

    public List<string> ClassIds { get; set; } = [];

    /// <summary>
    /// Class features by name, one of which the character needs: "Fighting Style" for the 2024
    /// Fighting Style feats, which only a fighter, paladin or ranger has.
    /// </summary>
    public List<string> Features { get; set; } = [];

    /// <summary>
    /// Requirements in words that the app does not check, such as another feat or being able to
    /// cast a spell. Shown to the player, never enforced.
    /// </summary>
    public List<string> Unchecked { get; set; } = [];

    /// <summary>
    /// The first requirement a character with these numbers falls short of, worded as in
    /// <see cref="Describe"/>, or null when the feat can be taken. Ability scores may be keyed in
    /// any spelling; one that is missing counts as 0. The race sizes are every size the race
    /// may be, so a race the player sizes counts as Small. The feature names are those of the class
    /// and subclass features the character has at this level.
    /// </summary>
    public string? FindUnmet(
        int level,
        string? raceId,
        string? subraceId,
        IReadOnlyCollection<string> raceSizes,
        string? classId,
        IReadOnlyDictionary<string, int> abilityScores,
        IReadOnlyCollection<string> featureNames)
    {
        ArgumentNullException.ThrowIfNull(raceSizes);
        ArgumentNullException.ThrowIfNull(abilityScores);
        ArgumentNullException.ThrowIfNull(featureNames);

        if (MinimumLevel is { } minimumLevel && level < minimumLevel)
        {
            return DescribeLevel(minimumLevel);
        }

        if (AsksForRace && !MeetsRace(raceId, subraceId, raceSizes))
        {
            return DescribeRaces();
        }

        if (ClassIds.Count != 0 && (classId is null || !ClassIds.Contains(classId)))
        {
            return DescribeAlternatives(ClassIds);
        }

        if (Features.Count != 0 && !Features.Any(f => featureNames.Contains(f, StringComparer.OrdinalIgnoreCase)))
        {
            return DescribeFeatures();
        }

        if (AbilityMinimums.Count != 0 && !AbilityMinimums.Any(m => ScoreOf(abilityScores, m.AbilityId) >= m.Score))
        {
            return DescribeAbilities();
        }

        return null;
    }

    /// <summary>
    /// Every requirement as the player reads it: "Level 19", "Strength or Dexterity 13",
    /// "Elf or Half-Elf", then the unchecked ones.
    /// </summary>
    public IEnumerable<string> Describe()
    {
        if (MinimumLevel is > 1)
        {
            yield return DescribeLevel(MinimumLevel.Value);
        }

        if (AbilityMinimums.Count != 0)
        {
            yield return DescribeAbilities();
        }

        if (AsksForRace)
        {
            yield return DescribeRaces();
        }

        if (ClassIds.Count != 0)
        {
            yield return DescribeAlternatives(ClassIds);
        }

        if (Features.Count != 0)
        {
            yield return DescribeFeatures();
        }

        foreach (var requirement in Unchecked)
        {
            yield return requirement;
        }
    }

    private bool AsksForRace => RaceIds.Count != 0 || SubraceIds.Count != 0 || Sizes.Count != 0;

    private bool MeetsRace(string? raceId, string? subraceId, IReadOnlyCollection<string> raceSizes) =>
        (raceId is not null && RaceIds.Contains(raceId))
        || (subraceId is not null && SubraceIds.Contains(subraceId))
        || Sizes.Exists(size => raceSizes.Contains(size, StringComparer.OrdinalIgnoreCase));

    private static string DescribeLevel(int level) => $"Level {level}";

    // "Dwarf or a Small race", "Wood Elf": a subrace id is its name and its race's, "wood_elf"
    private string DescribeRaces() =>
        string.Join(" or ", RaceIds.Select(DescribeId)
            .Concat(SubraceIds.Select(id =>
                CultureInfo.InvariantCulture.TextInfo.ToTitleCase(WithoutEdition(id).Replace('_', ' '))))
            .Concat(Sizes.Select(size => $"a {size} race")));

    private string DescribeFeatures() => $"The {string.Join(" or ", Features)} feature";

    // "Strength or Dexterity 13" when the minimums agree, "Intelligence 13 or Wisdom 15" when not
    private string DescribeAbilities()
    {
        var names = AbilityMinimums.Select(m => AbilityScore.FromId(m.AbilityId).Name).ToList();
        var scores = AbilityMinimums.Select(m => m.Score).Distinct().ToList();

        return scores.Count == 1
            ? $"{string.Join(" or ", names)} {scores[0]}"
            : string.Join(" or ", AbilityMinimums.Select((m, i) => $"{names[i]} {m.Score}"));
    }

    // Ids read as names: "half_elf" is "Half-Elf", and a 2024 id without its edition,
    // "fighter-2024" is "Fighter"
    private static string DescribeAlternatives(IEnumerable<string> ids) => string.Join(" or ", ids.Select(DescribeId));

    private static string DescribeId(string id) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(WithoutEdition(id).Replace('_', '-'));

    private static string WithoutEdition(string id) =>
        id.EndsWith("-2024", StringComparison.Ordinal) || id.EndsWith("_2024", StringComparison.Ordinal) ? id[..^5] : id;

    private static int ScoreOf(IReadOnlyDictionary<string, int> abilityScores, string abilityId) =>
        abilityScores
            .Where(pair => AbilityScore.TryNormalize(pair.Key, out var id) && id == abilityId)
            .Select(pair => pair.Value)
            .FirstOrDefault();
}
