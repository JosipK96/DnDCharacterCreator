using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Races;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>
/// The tool choices a draft owes, in the order CharacterDraft.ToolPicks keeps the picks: the
/// race's, the background's, then the class's. The tools step lists them, ChooseTools checks the
/// picks against them, and finalize checks again in case one of the three changed.
/// </summary>
public static class ToolPicks
{
    public static List<ToolSlot> SlotsFor(Race race, Background background, Class @class) =>
        race.ToolChoices.Select(c => new ToolSlot(race.Name, c))
            .Concat(background.ToolChoices.Select(c => new ToolSlot(background.Name, c)))
            .Concat(@class.ToolChoices.Select(c => new ToolSlot(@class.Name, c)))
            .ToList();

    /// <summary>The first choice the picks leave open or fill wrongly, or null when they fill every one.</summary>
    public static Error? FindInvalidPick(IReadOnlyList<List<string>> picks, IReadOnlyList<ToolSlot> slots)
    {
        if (picks.Count != slots.Count)
        {
            return CharacterErrors.ToolChoicesIncomplete(slots.Select(s => s.Name), picks.Count);
        }

        for (var i = 0; i < slots.Count; i++)
        {
            if (!slots[i].Choice.Allows(picks[i]))
            {
                return CharacterDraftErrors.InvalidToolPick(slots[i].Name, slots[i].Choice.Count, slots[i].Choice.Options);
            }
        }

        return null;
    }
}
