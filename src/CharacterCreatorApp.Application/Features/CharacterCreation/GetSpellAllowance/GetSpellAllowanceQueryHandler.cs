using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.GetSpellAllowance;

/// <summary>
/// The numbers the spells step shows for the class's spells. A 2014 cleric's prepared spells
/// follow its final scores, so they are worked out here, as ChooseSpells checks them, and the
/// client decides nothing.
/// </summary>
public class GetSpellAllowanceQueryHandler(
    ICharacterDraftRepository characterDraftRepository,
    IClassRepository classRepository,
    IRaceRepository raceRepository,
    IBackgroundRepository backgroundRepository,
    ILanguageRepository languageRepository,
    IFeatRepository featRepository,
    ISpellRepository spellRepository,
    IUserContext userContext,
    TimeProvider timeProvider) : IQueryHandler<GetSpellAllowanceQuery, Response<SpellAllowanceReadModel>>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IUserContext _userContext = userContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    private readonly DraftSourceLoader _sourceLoader = new(
        raceRepository, classRepository, backgroundRepository, languageRepository, featRepository, spellRepository);

    public async Task<Response<SpellAllowanceReadModel>> Handle(GetSpellAllowanceQuery query, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdAsync(query.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(query.DraftId);
        }

        if (string.IsNullOrEmpty(draft.ClassId))
        {
            return CharacterDraftErrors.ClassRequired("selecting spells");
        }

        var @class = await _classRepository.GetByIdWithDetailsAsync(draft.ClassId, cancellationToken);

        if (@class == null)
        {
            return ClassErrors.NotFoundById(draft.ClassId);
        }

        if (draft.Level is not { } level)
        {
            return CharacterDraftErrors.LevelRequired("selecting spells");
        }

        var subclass = @class.Subclasses.FirstOrDefault(s => s.Id == draft.SubclassId);
        var allowance = await SpellChoice.AllowanceForAsync(
            draft, @class, subclass, level, _sourceLoader, _userContext.UserId, _timeProvider, cancellationToken);

        if (!allowance.IsSuccess)
        {
            return allowance.Errors;
        }

        if (allowance.Value is not { } spells)
        {
            return new SpellAllowanceReadModel();
        }

        return new SpellAllowanceReadModel
        {
            CastsSpells = true,
            Cantrips = spells.Cantrips,
            Spells = spells.Spells,
            MaxSpellLevel = spells.MaxSpellLevel,
            SpellsFollowScores = @class.PreparesSpellsFromScoresAt(level),
            AlwaysPreparedSpellIds = subclass?.AlwaysPreparedSpellIdsAt(level).ToList() ?? [],
        };
    }
}
