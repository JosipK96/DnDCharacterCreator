using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.Services;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterDetails;

/// <summary>
/// Loads a saved character with everything its sheet shows and works out its numbers. The sheet
/// page and the PDF both go through it, so the two cannot disagree, and it fills the sheet the
/// way the review step's preview does (<see cref="CharacterSheetContentLoader"/>,
/// <see cref="CharacterStatsInput.For"/>).
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
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;

    private readonly CharacterSheetContentLoader _contentLoader = new(
        proficiencyRepository, featureRepository, spellRepository, languageRepository, featRepository);

    /// <summary>
    /// The user's character, or not found when it does not exist or belongs to someone else. Also
    /// not found when its class, race or background (or subclass or subrace) can no longer be
    /// loaded: a sheet built without them would show wrong numbers rather than an error.
    /// </summary>
    public async Task<Response<CharacterDetailsReadModel>> LoadAsync(Guid characterId, Guid userId, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetByIdWithDetailsAsync(characterId, userId, cancellationToken);

        if (character == null)
        {
            return CharacterErrors.NotFoundById(characterId);
        }

        // Hit die, saving throws, features, an unarmoured armour class and spell slots come from
        // the class and subclass, speed from the race and subrace, and one feature from the background; the
        // character stores only their ids
        var @class = await _classRepository.GetByIdWithDetailsAsync(character.ClassId, cancellationToken);

        if (@class == null)
        {
            return CharacterErrors.ClassNotFoundById(character.ClassId);
        }

        var race = await _raceRepository.GetByIdWithDetailsAsync(character.RaceId, cancellationToken);

        if (race == null)
        {
            return CharacterErrors.RaceNotFoundById(character.RaceId);
        }

        var background = await _backgroundRepository.GetByIdWithDetailsAsync(character.BackgroundId, cancellationToken);

        if (background == null)
        {
            return CharacterErrors.BackgroundNotFoundById(character.BackgroundId);
        }

        var subclass = string.IsNullOrWhiteSpace(character.SubclassId)
            ? null
            : @class.Subclasses.FirstOrDefault(s => s.Id == character.SubclassId);

        if (!string.IsNullOrWhiteSpace(character.SubclassId) && subclass == null)
        {
            return CharacterErrors.SubclassNotFoundById(character.SubclassId);
        }

        var subrace = string.IsNullOrWhiteSpace(character.SubraceId)
            ? null
            : race.Subraces.FirstOrDefault(s => s.Id == character.SubraceId);

        if (!string.IsNullOrWhiteSpace(character.SubraceId) && subrace == null)
        {
            return CharacterErrors.SubraceNotFoundById(character.SubraceId);
        }

        var content = await _contentLoader.LoadAsync(
            character.ProficiencyIds,
            CharacterFeatures.IdsFor(@class, subclass, background, character.Level),
            character.SpellIds,
            character.LanguageIds,
            character.FeatIds,
            character.FeatSpells,
            cancellationToken);

        character.Proficiencies = content.Proficiencies;
        character.Features = content.Features;
        character.Spells = content.Spells;
        character.Languages = content.Languages;
        character.Feats = content.Feats;

        character.Stats = CharacterStatsCalculator.Calculate(CharacterStatsInput.For(
            character.Level,
            character.AbilityScores,
            character.ProficiencyIds,
            character.HitDieResults,
            character.FeatIds,
            character.FeatSavingThrowAbilityIds,
            race,
            subrace,
            @class,
            subclass));

        return character;
    }
}
