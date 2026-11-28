using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Choices;
using CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Loaders;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels.Spells;
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
    private readonly DraftSelectionsLoader _selections = new(raceRepository, classRepository, backgroundRepository);
    private readonly ISpellRepository _spellRepository = spellRepository;
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

        var classResult = await _selections.LoadClassAsync(draft, "selecting spells", cancellationToken);

        if (!classResult.IsSuccess)
        {
            return classResult.Errors;
        }

        var @class = classResult.Value;

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

        // The lists ChooseSpells checks the picks against, less what the draft's books or level rule out
        var choosable = await SpellChoice.LoadChoosableAsync(_spellRepository, @class, subclass, cancellationToken);
        var listed = choosable.Where(s => s.Level <= spells.MaxSpellLevel && draft.AllowsSource(s.Source)).ToList();

        // A domain or oath spell is prepared whatever the books, and may be off the class's list
        // (a Vengeance paladin's Hunter's Mark), so the step can name every one
        var alwaysPrepared = subclass?.AlwaysPreparedSpellIdsAt(level).ToList() ?? [];
        var unlisted = alwaysPrepared.Except(listed.Select(s => s.Id)).ToList();

        if (unlisted.Count != 0)
        {
            listed.AddRange(await _spellRepository.GetByIdsAsync(unlisted, cancellationToken));
        }

        return new SpellAllowanceReadModel
        {
            CastsSpells = true,
            Cantrips = spells.Cantrips,
            Spells = spells.Spells,
            MaxSpellLevel = spells.MaxSpellLevel,
            SpellsFollowScores = @class.PreparesSpellsFromScoresAt(level),
            AlwaysPreparedSpellIds = alwaysPrepared,
            SpellList = [.. listed.OrderBy(s => s.Level).ThenBy(s => s.Name, StringComparer.Ordinal)],
        };
    }
}
