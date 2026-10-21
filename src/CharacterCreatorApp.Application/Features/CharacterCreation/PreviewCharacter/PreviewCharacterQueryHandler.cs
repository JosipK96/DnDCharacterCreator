using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.Services;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.PreviewCharacter;

/// <summary>
/// Builds the character the draft would produce and resolves its ids to names, so the review
/// step can show exactly what finalizing will create. Nothing is saved.
/// </summary>
public class PreviewCharacterQueryHandler(
    ICharacterDraftRepository characterDraftRepository,
    IRaceRepository raceRepository,
    IClassRepository classRepository,
    IBackgroundRepository backgroundRepository,
    ILanguageRepository languageRepository,
    IFeatRepository featRepository,
    IProficiencyRepository proficiencyRepository,
    IFeatureRepository featureRepository,
    ISpellRepository spellRepository,
    IUserContext userContext,
    TimeProvider timeProvider) : IQueryHandler<PreviewCharacterQuery, Response<CharacterPreviewReadModel>>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IProficiencyRepository _proficiencyRepository = proficiencyRepository;
    private readonly IFeatureRepository _featureRepository = featureRepository;
    private readonly ISpellRepository _spellRepository = spellRepository;
    private readonly ILanguageRepository _languageRepository = languageRepository;
    private readonly IUserContext _userContext = userContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    private readonly DraftSourceLoader _sourceLoader = new(
        raceRepository, classRepository, backgroundRepository, languageRepository, featRepository, spellRepository);

    public async Task<Response<CharacterPreviewReadModel>> Handle(PreviewCharacterQuery query, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdAsync(query.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(query.DraftId);
        }

        if (!draft.IsReadyToFinalize())
        {
            return CharacterDraftErrors.Invalid();
        }

        var sourcesResponse = await _sourceLoader.LoadAsync(draft, cancellationToken);

        if (!sourcesResponse.IsSuccess)
        {
            return sourcesResponse.Errors;
        }

        var sources = sourcesResponse.Value;

        var character = CharacterBuilder.Build(_userContext.UserId, draft, sources, _timeProvider);

        var stats = CharacterStatsCalculator.Calculate(new CharacterStatsInput(
            Level: character.Level,
            AbilityScores: character.AbilityScores,
            ProficiencyIds: character.ProficiencyIds,
            HitDie: sources.Class.HitDie,
            SavingThrowAbilityIds: sources.Class.SavingThrowAbilityIds.Concat(character.FeatSavingThrowAbilityIds).Distinct().ToList(),
            Speed: sources.Race.SpeedWith(sources.Subrace),
            RaceId: character.RaceId,
            SubraceId: character.SubraceId,
            SubclassId: character.SubclassId,
            FeatIds: character.FeatIds,
            UnarmoredArmorClasses: sources.Class.UnarmoredArmorClassesWith(sources.Subclass)));

        var spells = character.SpellIds.Count > 0
            ? await _spellRepository.GetByIdsAsync(character.SpellIds, cancellationToken)
            : [];

        var preview = new CharacterPreviewReadModel
        {
            Name = character.Name,
            Level = character.Level,
            Edition = character.Edition,
            RaceName = sources.Race.Name,
            SubraceName = sources.Subrace?.Name,
            ClassName = sources.Class.Name,
            SubclassName = sources.Subclass?.Name,
            BackgroundName = sources.Background.Name,
            Size = character.Size ?? sources.Race.Size,
            Stats = stats,
            Feats = CharacterFeatEntries.Build(character.FeatIds, character.FeatSpells, sources.Feats, spells),
            Proficiencies = await _proficiencyRepository.GetByIdsAsync(character.ProficiencyIds, cancellationToken),
            Features = await _featureRepository.GetByIdsAsync(
                CharacterFeatures.IdsFor(sources.Class, sources.Subclass, sources.Background, character.Level), cancellationToken),
            Spells = spells,
            Languages = await _languageRepository.GetByIdsAsync(character.LanguageIds, cancellationToken)
        };

        // Surfaced rather than returned as an error: the player should see the sheet so far
        // alongside what still needs choosing.
        preview.BlockingIssue = DraftCompletenessChecker.FindMissingChoice(draft, sources, character.AbilityScores)?.Description;

        return preview;
    }
}
