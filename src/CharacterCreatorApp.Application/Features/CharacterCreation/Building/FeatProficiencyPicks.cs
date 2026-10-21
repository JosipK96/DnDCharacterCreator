using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Characters;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>
/// The feats that ask for skills or tools (Keen Mind's one skill, Skilled's three skills or
/// tools), in the order CharacterDraft.FeatProficiencyPicks keeps their picks: the background's
/// feat, then the race's and the improvements' feats. The improvements step and finalize both
/// check through here.
/// </summary>
public static class FeatProficiencyPicks
{
    /// <summary>
    /// One slot per feat whose choice is a proficiency pick, a repeatable one once per time taken.
    /// </summary>
    public static List<FeatProficiencySlot> SlotsFor(
        IEnumerable<string> backgroundFeatIds, IEnumerable<string> featIds, IReadOnlyDictionary<string, FeatReadModel> featsById)
    {
        // shortcut: the Fighting Styles are left out, since none asks for a skill or tool; add them,
        // between the background's feat and the others as FeatSpells has them, when one does
        return backgroundFeatIds
            .Concat(featIds)
            .Select(id => featsById.GetValueOrDefault(id))
            .Where(feat => feat?.Choice?.IsProficiencyPick() == true)
            .Select(feat => new FeatProficiencySlot(feat!.Id, feat.Name, feat.Choice!))
            .ToList();
    }

    /// <summary>The first slot the picks leave open or fill wrongly, or null when they fill every one in order.</summary>
    public static Error? FindInvalidPick(IReadOnlyList<FeatProficiencyPick> picks, IReadOnlyList<FeatProficiencySlot> slots)
    {
        if (picks.Count != slots.Count || picks.Where((pick, i) => pick.FeatId != slots[i].FeatId).Any())
        {
            return CharacterErrors.FeatProficiencyPicksIncomplete(slots.Select(s => s.Name), picks.Count);
        }

        for (var i = 0; i < slots.Count; i++)
        {
            if (!slots[i].Choice.Allows(picks[i].ProficiencyIds))
            {
                return CharacterDraftErrors.InvalidFeatProficiencyPick(slots[i].Name, slots[i].Choice.Count, slots[i].Choice.Options);
            }
        }

        return null;
    }
}
