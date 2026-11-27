using CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Choices;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.Services;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Finalizing;

/// <summary>
/// What a character casts its race's and feats' picked spells with, for the sheet's save DC and
/// attack bonus beside the class's. The review step and the sheet both come here.
/// </summary>
public static class BonusSpellcasting
{
    /// <summary>
    /// One source for the race's spells, labelled with the subrace or race, then one per feat
    /// spell pick, labelled with the feat. Where the race or feat lets the player pick the ability
    /// (a 2024 high elf, the 2024 Magic Initiate) the pick is used. Otherwise a race's spells are
    /// cast with its own ability (a tiefling's Charisma), and spells picked from a class's list
    /// with that class's (the 2014 Magic Initiate, the 2014 high elf's wizard cantrip).
    /// </summary>
    public static async Task<List<SpellcastingSource>> LoadAsync(
        Race race,
        Subrace? subrace,
        string? raceSpellAbilityId,
        IEnumerable<FeatSpellPick> featSpells,
        IEnumerable<CharacterFeatReadModel> feats,
        IClassRepository classRepository,
        CancellationToken cancellationToken)
    {
        var sources = new List<SpellcastingSource>();
        var abilityByClassId = new Dictionary<string, string?>(StringComparer.Ordinal);

        var raceSlot = BonusSpells.RaceSlot(race, subrace);
        var raceAbility = raceSpellAbilityId
            ?? race.SpellcastingAbilityWith(subrace)
            ?? (raceSlot is { Choice: { ClassIds: [var raceListClassId], AbilityOptions.Count: 0 } }
                ? await ClassAbilityAsync(raceListClassId, abilityByClassId, classRepository, cancellationToken)
                : null);

        if (raceAbility != null)
        {
            sources.Add(new SpellcastingSource(subrace?.Name ?? race.Name, raceAbility));
        }

        var featNames = feats.Select(f => f.Feat).DistinctBy(f => f.Id).ToDictionary(f => f.Id, f => f.Name);

        foreach (var pick in featSpells)
        {
            var ability = pick.AbilityId ?? await ClassAbilityAsync(pick.ClassId, abilityByClassId, classRepository, cancellationToken);

            if (ability != null)
            {
                sources.Add(new SpellcastingSource(featNames.GetValueOrDefault(pick.FeatId, pick.FeatId), ability));
            }
        }

        return sources;
    }

    private static async Task<string?> ClassAbilityAsync(
        string classId, Dictionary<string, string?> abilityByClassId, IClassRepository classRepository, CancellationToken cancellationToken)
    {
        if (!abilityByClassId.TryGetValue(classId, out var ability))
        {
            ability = (await classRepository.GetByIdAsync(classId, cancellationToken))?.Spellcasting?.SpellcastingAbility;
            abilityByClassId[classId] = ability;
        }

        return ability;
    }
}
