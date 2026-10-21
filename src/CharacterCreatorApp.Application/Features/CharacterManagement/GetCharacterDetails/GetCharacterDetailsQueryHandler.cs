using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.Services;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterDetails;

public class GetCharacterDetailsQueryHandler(
    ICharacterRepository characterRepository,
    IUserContext userContext,
    IProficiencyRepository proficiencyRepository,
    IFeatureRepository featureRepository,
    ISpellRepository spellRepository,
    ILanguageRepository languageRepository,
    IFeatRepository featRepository,
    IClassRepository classRepository,
    IRaceRepository raceRepository,
    IBackgroundRepository backgroundRepository) : IQueryHandler<GetCharacterDetailsQuery, Response<CharacterDetailsReadModel>>
{
    private readonly ICharacterRepository _characterRepository = characterRepository;
    private readonly IUserContext _userContext = userContext;
    private readonly IProficiencyRepository _proficiencyRepository = proficiencyRepository;
    private readonly IFeatureRepository _featureRepository = featureRepository;
    private readonly ISpellRepository _spellRepository = spellRepository;
    private readonly ILanguageRepository _languageRepository = languageRepository;
    private readonly IFeatRepository _featRepository = featRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;

    public async Task<Response<CharacterDetailsReadModel>> Handle(GetCharacterDetailsQuery query, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetByIdWithDetailsAsync(query.CharacterId, _userContext.UserId, cancellationToken);

        if (character == null)
        {
            return CharacterErrors.NotFoundById(query.CharacterId);
        }

        // Hit die, saving throws, features and an unarmoured armour class come from the class and
        // subclass, speed from the race and subrace, and one feature from the background; the
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
            SavingThrowAbilityIds: (@class?.SavingThrowAbilityIds ?? []).Concat(character.FeatSavingThrowAbilityIds).Distinct().ToList(),
            Speed: race?.SpeedWith(race.Subraces.FirstOrDefault(s => s.Id == character.SubraceId)) ?? 30,
            RaceId: character.RaceId,
            SubraceId: character.SubraceId,
            SubclassId: character.SubclassId,
            FeatIds: character.FeatIds,
            UnarmoredArmorClasses: @class?.UnarmoredArmorClassesWith(subclass) ?? []));

        return character;
    }
}
