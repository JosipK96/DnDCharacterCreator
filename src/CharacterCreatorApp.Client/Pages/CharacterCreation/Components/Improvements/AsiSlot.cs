using CharacterCreatorApp.Contracts.Responses.Feats;

namespace CharacterCreatorApp.Client.Pages.CharacterCreation.Components.Improvements;

/// <summary>
/// One earned ability score improvement, spent either on ability points or on a feat, or a feat the
/// race gives (a variant human's), which is a feat and nothing else. What the player can do to one
/// slot, and whether it is filled in, lives here; the step holds the list of them.
/// </summary>
public sealed class AsiSlot
{
    /// <summary>An improvement spends two points: +2 to one ability, or +1 to two.</summary>
    public const int Points = 2;

    public bool IsRaceFeat { get; init; }

    public bool TakesFeat { get; set; }

    /// <summary>The points spent, by ability id ("STR"), when the slot raises abilities.</summary>
    public Dictionary<string, int> Improvement { get; } = [];

    public string? FeatId { get; set; }

    /// <summary>The ability the feat raises where it offers several: Athlete's Strength or Dexterity.</summary>
    public string? FeatAbility { get; set; }

    /// <summary>The skills or tools picked for a feat that asks for them, one entry per pick it gives.</summary>
    public List<string?> ProficiencyPicks { get; set; } = [];

    public string FeatSearch { get; set; } = string.Empty;

    public int PointsLeft => Points - Improvement.Values.Sum();

    public int PointsIn(string abilityId) => Improvement.GetValueOrDefault(abilityId);

    /// <summary>Whether another point can go into the ability: one is left, and it has fewer than two.</summary>
    public bool CanRaise(string abilityId) => PointsLeft > 0 && PointsIn(abilityId) < Points;

    /// <summary>Adds a point to the ability; false, and nothing changes, when <see cref="CanRaise"/> says no.</summary>
    public bool Raise(string abilityId)
    {
        if (!CanRaise(abilityId))
        {
            return false;
        }

        Improvement[abilityId] = PointsIn(abilityId) + 1;
        return true;
    }

    /// <summary>Takes a point back from the ability; false, and nothing changes, when it has none.</summary>
    public bool Lower(string abilityId)
    {
        var current = PointsIn(abilityId);

        if (current <= 0)
        {
            return false;
        }

        if (current == 1)
        {
            Improvement.Remove(abilityId);
            return true;
        }

        Improvement[abilityId] = current - 1;
        return true;
    }

    /// <summary>
    /// Switches the slot between raising abilities and taking a feat, clearing whichever choice no
    /// longer applies so a half-finished one cannot be submitted. Returns whether points were taken
    /// back, which changes which feats the character can take.
    /// </summary>
    public bool SwitchTo(bool takesFeat)
    {
        if (TakesFeat == takesFeat)
        {
            return false;
        }

        TakesFeat = takesFeat;

        if (takesFeat)
        {
            var hadPoints = Improvement.Count > 0;
            Improvement.Clear();
            return hadPoints;
        }

        FeatId = null;
        FeatAbility = null;
        ProficiencyPicks = [];
        return false;
    }

    /// <summary>Picks the feat, or unpicks it when it was the one picked, and starts its picks afresh.</summary>
    public void ToggleFeat(string featId, FeatApiResponse? feat)
    {
        FeatId = FeatId == featId ? null : featId;
        FeatAbility = null;

        // As many empty picks as the feat gives, or none
        ProficiencyPicks = FeatId != null && feat?.Choice is { } choice && choice.AsksForProficiencies()
            ? Enumerable.Repeat<string?>(null, choice.Count).ToList()
            : [];
    }

    /// <summary>
    /// What the character lacks for the feat in this slot, or null when it can take it. The race's
    /// own feat counts no improvement; one the API said nothing about is left to the save.
    /// </summary>
    public string? UnmetFor(FeatEligibilityApiResponse? status) =>
        status is null ? null : IsRaceFeat ? status.UnmetAsRaceFeat : status.Unmet;

    /// <summary>A feat raising one of several abilities needs the pick; one raising a fixed ability does not.</summary>
    public bool HasAbilityPick(FeatApiResponse? feat) =>
        feat?.AbilityIncrease is not { Options.Count: > 1 } increase
        || (!string.IsNullOrEmpty(FeatAbility) && increase.Options.Contains(FeatAbility));

    /// <summary>What the feat raises as picked; the API knows a fixed increase (Actor's Charisma) on its own.</summary>
    public Dictionary<string, int> FeatIncrease(FeatApiResponse? feat) =>
        feat?.AbilityIncrease is { Options.Count: > 1 } increase && !string.IsNullOrEmpty(FeatAbility)
            ? new Dictionary<string, int> { [FeatAbility] = increase.Points }
            : [];

    /// <summary>
    /// Whether the slot is ready to save: every point spent, or a feat the character can take with
    /// its ability and every skill or tool picked.
    /// </summary>
    public bool IsComplete(FeatApiResponse? feat, FeatEligibilityApiResponse? status)
    {
        if (!TakesFeat)
        {
            return PointsLeft == 0;
        }

        return !string.IsNullOrEmpty(FeatId)
            && UnmetFor(status) == null
            && HasAbilityPick(feat)
            && ProficiencyPicks.All(id => !string.IsNullOrEmpty(id));
    }
}
