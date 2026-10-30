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
    private readonly IUserContext _userContext = userContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    private readonly CharacterSheetContentLoader _contentLoader = new(
        proficiencyRepository, featureRepository, spellRepository, languageRepository, featRepository);

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

        var stats = CharacterStatsCalculator.Calculate(CharacterStatsInput.For(
            character.Level,
            character.AbilityScores,
            character.ProficiencyIds,
            character.HitDieResults,
            character.FeatIds,
            character.FeatSavingThrowAbilityIds,
            sources.Race,
            sources.Subrace,
            sources.Class,
            sources.Subclass));

        var content = await _contentLoader.LoadAsync(
            character.ProficiencyIds,
            CharacterFeatures.IdsFor(sources.Class, sources.Subclass, sources.Background, character.Level),
            character.SpellIds,
            character.LanguageIds,
            character.FeatIds,
            character.FeatSpells,
            cancellationToken);

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
            Feats = content.Feats,
            Proficiencies = content.Proficiencies,
            Features = content.Features,
            Spells = content.Spells,
            Languages = content.Languages
        };

        // Surfaced rather than returned as an error: the player should see the sheet so far
        // alongside what still needs choosing.
        preview.BlockingIssue = DraftCompletenessChecker.FindMissingChoice(draft, sources, character.AbilityScores)?.Description;

        return preview;
    }
}
