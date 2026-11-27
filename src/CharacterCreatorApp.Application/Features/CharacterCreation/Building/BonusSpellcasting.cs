using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.Services;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>
/// What a character casts its race's and feats' picked spells with, for the sheet's save DC and
/// attack bonus beside the class's. The review step and the sheet both come here.
/// </summary>
public static class BonusSpellcasting
{
    /// <summary>
    /// The race's spell pick (a 2014 high elf's wizard cantrip), then one source per feat spell
    /// pick, labelled with the subrace, race or feat. A feat that lets the player pick the ability
    /// (the 2024 Magic Initiate) uses the pick; otherwise the spells are cast with the ability of
    /// the class whose list they come from (the 2014 Magic Initiate, the 2014 high elf).
    /// </summary>
    public static async Task<List<SpellcastingSource>> LoadAsync(
        Race race,
        Subrace? subrace,
        IEnumerable<FeatSpellPick> featSpells,
        IEnumerable<CharacterFeatReadModel> feats,
        IClassRepository classRepository,
        CancellationToken cancellationToken)
    {
        var sources = new List<SpellcastingSource>();
        var abilityByClassId = new Dictionary<string, string?>(StringComparer.Ordinal);

        // shortcut: race spells that come with no pick (a tiefling's Hellish Rebuke) name no ability
        // in the seed data, and a race cantrip whose ability the player picks (a 2024 elf's, the
        // Kobold Legacy's) has that pick stored nowhere, so neither adds numbers; upgrade when the
        // generator reads the one and the draft stores the other.
        if (BonusSpells.RaceSlot(race, subrace) is { Choice: { ClassIds: [var raceListClassId], AbilityOptions.Count: 0 } } raceSlot
            && await ClassAbilityAsync(raceListClassId, abilityByClassId, classRepository, cancellationToken) is { } raceAbility)
        {
            sources.Add(new SpellcastingSource(raceSlot.Name, raceAbility));
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
