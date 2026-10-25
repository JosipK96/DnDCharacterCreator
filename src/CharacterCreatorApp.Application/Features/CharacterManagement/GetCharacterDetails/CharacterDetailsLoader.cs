using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.Services;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterDetails;

/// <summary>
/// Loads a saved character with everything its sheet shows and works out its numbers. The sheet
/// page and the PDF both go through it, so the two cannot disagree.
/// </summary>
public class CharacterDetailsLoader(
    ICharacterRepository characterRepository,
    IProficiencyRepository proficiencyRepository,
    IFeatureRepository featureRepository,
    ISpellRepository spellRepository,
    ILanguageRepository languageRepository,
    IFeatRepository featRepository,
    IClassRepository classRepository,
    IRaceRepository raceRepository,
    IBackgroundRepository backgroundRepository)
{
    private readonly ICharacterRepository _characterRepository = characterRepository;
    private readonly IProficiencyRepository _proficiencyRepository = proficiencyRepository;
    private readonly IFeatureRepository _featureRepository = featureRepository;
    private readonly ISpellRepository _spellRepository = spellRepository;
    private readonly ILanguageRepository _languageRepository = languageRepository;
    private readonly IFeatRepository _featRepository = featRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;

    /// <summary>The user's character, or null when it does not exist or belongs to someone else.</summary>
    public async Task<CharacterDetailsReadModel?> LoadAsync(Guid characterId, Guid userId, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetByIdWithDetailsAsync(characterId, userId, cancellationToken);

        if (character == null)
        {
            return null;
        }

        // Hit die, saving throws, features, an unarmoured armour class and spell slots come from
        // the class and subclass, speed from the race and subrace, and one feature from the background; the
        // character stores only their ids
        var @class = await _classRepository.GetByIdWithDetailsAsync(character.ClassId, cancellationToken);
        var race = await _raceRepository.GetByIdWithDetailsAsync(character.RaceId, cancellationToken);
        var background = await _backgroundRepository.GetByIdWithDetailsAsync(character.BackgroundId, cancellationToken);

        var subclass = @class?.Subclasses.FirstOrDefault(s => s.Id == character.SubclassId);

        var featureIds = @class == null || background == null
            ? []
            : CharacterFeatures.IdsFor(@class, subclass, background, character.Level);

        character.Proficiencies = await _proficiencyRepository.GetByIdsAsync(character.ProficiencyIds, cancellationToken);
        character.Features = await _featureRepository.GetByIdsAsync(featureIds, cancellationToken);
        character.Spells = character.SpellIds.Count > 0
            ? await _spellRepository.GetByIdsAsync(character.SpellIds, cancellationToken)
            : [];
        character.Languages = await _languageRepository.GetByIdsAsync(character.LanguageIds, cancellationToken);
        character.Feats = CharacterFeatEntries.Build(
            character.FeatIds, character.FeatSpells, await _featRepository.GetByIdsAsync(character.FeatIds, cancellationToken), character.Spells);

        character.Stats = CharacterStatsCalculator.Calculate(new CharacterStatsInput(
            Level: character.Level,
            AbilityScores: character.AbilityScores,
            ProficiencyIds: character.ProficiencyIds,
            HitDie: @class?.HitDie ?? string.Empty,
            HitDieResults: character.HitDieResults,
            SavingThrowAbilityIds: (@class?.SavingThrowAbilityIds ?? []).Concat(character.FeatSavingThrowAbilityIds).Distinct().ToList(),
            Speed: race?.SpeedWith(race.Subraces.FirstOrDefault(s => s.Id == character.SubraceId)) ?? 30,
            RaceId: character.RaceId,
            SubraceId: character.SubraceId,
            SubclassId: character.SubclassId,
            FeatIds: character.FeatIds,
            UnarmoredArmorClasses: @class?.UnarmoredArmorClassesWith(subclass) ?? [],
            SpellSlots: @class?.SpellSlotsAt(character.Level, subclass) ?? SpellSlots.None));

        return character;
    }
}
