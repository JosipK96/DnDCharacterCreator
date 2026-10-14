using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Races;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>
/// Whether a picked proficiency is one the character already has. The race step comes before the
/// background, so a half-elf could pick Athletics and then take Soldier, and the sheet quietly
/// counted it once. The rules let the player pick another instead, so a pick that repeats what
/// the race, subrace, background or class gives, or another step's pick, is refused: in the race
/// skill, proficiency and tool steps for what is known by then, and at finalize for all of it. A
/// tool picked twice (a dwarf's smith's tools and a Guild Artisan's) is refused the same way.
/// </summary>
public static class ProficiencyPickCheck
{
    /// <summary>
    /// The first pick that repeats a proficiency given or picked before it, or null. With no picks
    /// passed, finalize's case, every pick of the draft is checked. A step passes the picks it is
    /// about to save, and only those are checked: a clash between the others is for their own step
    /// or finalize to report, not a reason to refuse this one.
    /// </summary>
    public static Error? FindRepeated(
        CharacterDraft draft,
        Race? race,
        Subrace? subrace,
        Background? background,
        Class? @class,
        IReadOnlyList<string>? raceSkillPicks = null,
        IReadOnlyList<string>? classPicks = null,
        IReadOnlyList<List<string>>? toolPicks = null)
    {
        var sources = new Dictionary<string, string>();

        Add(race?.RaceProficiencies.Select(p => p.ProficiencyId), race?.Name);
        Add(subrace?.SubraceProficiencies.Select(p => p.ProficiencyId), subrace?.Name);
        Add(background?.BackgroundProficiencies.Select(p => p.ProficiencyId), background?.Name);
        Add(@class?.ClassProficiencies.Select(p => p.ProficiencyId), @class?.Name);

        var racePicks = $"the {subrace?.Name ?? race?.Name} skill picks";
        var classPicksName = $"the {@class?.Name} proficiency picks";
        const string toolPicksName = "another tool pick";

        if (raceSkillPicks is not null)
        {
            Add(draft.SelectedClassProficiencyIds, classPicksName);
            return FirstRepeated(raceSkillPicks, racePicks);
        }

        if (classPicks is not null)
        {
            Add(draft.SelectedRaceSkillIds, racePicks);
            return FirstRepeated(classPicks, classPicksName);
        }

        if (toolPicks is not null)
        {
            Add(draft.SelectedRaceSkillIds, racePicks);
            Add(draft.SelectedClassProficiencyIds, classPicksName);
            return FirstRepeated(toolPicks.SelectMany(p => p), toolPicksName);
        }

        return FirstRepeated(draft.SelectedRaceSkillIds, racePicks)
            ?? FirstRepeated(draft.SelectedClassProficiencyIds, classPicksName)
            ?? FirstRepeated(draft.ToolPicks.SelectMany(p => p), toolPicksName);

        Error? FirstRepeated(IEnumerable<string> picks, string pickedIn)
        {
            foreach (var id in picks)
            {
                if (sources.TryGetValue(id, out var source))
                {
                    return CharacterDraftErrors.ProficiencyAlreadyGiven(id, source);
                }

                sources[id] = pickedIn;
            }

            return null;
        }

        void Add(IEnumerable<string>? ids, string? source)
        {
            foreach (var id in ids ?? [])
            {
                sources.TryAdd(id, source!);
            }
        }
    }
}
