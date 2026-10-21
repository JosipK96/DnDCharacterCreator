using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.GetBonusSpells;

/// <summary>
/// What the spells step shows below the class's spells. The picks owed and the spells each may
/// be are worked out here, as ChooseBonusSpells and finalize check them, so the client decides nothing.
/// </summary>
public class GetBonusSpellsQueryHandler(
    ICharacterDraftRepository characterDraftRepository,
    IRaceRepository raceRepository,
    IBackgroundRepository backgroundRepository,
    IFeatRepository featRepository,
    ISpellRepository spellRepository,
    IClassRepository classRepository,
    IUserContext userContext) : IQueryHandler<GetBonusSpellsQuery, Response<List<BonusSpellChoiceReadModel>>>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;
    private readonly IFeatRepository _featRepository = featRepository;
    private readonly ISpellRepository _spellRepository = spellRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response<List<BonusSpellChoiceReadModel>>> Handle(GetBonusSpellsQuery query, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdAsync(query.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(query.DraftId);
        }

        var (raceSlot, featSlots) = await BonusSpells.LoadSlotsAsync(
            draft, _raceRepository, _backgroundRepository, _featRepository, cancellationToken);
        var slots = (raceSlot is null ? featSlots : featSlots.Prepend(raceSlot)).ToList();

        if (slots.Count == 0)
        {
            return new List<BonusSpellChoiceReadModel>();
        }

        var lists = await BonusSpells.LoadListsAsync(_spellRepository, slots, cancellationToken);
        var classNames = (await _classRepository.GetClassesAsync(draft.Edition, cancellationToken)).ToDictionary(c => c.Id, c => c.Name);

        return slots.ConvertAll(slot => new BonusSpellChoiceReadModel
        {
            FeatId = slot.FeatId,
            Name = slot.Name,
            Cantrips = slot.Choice.Cantrips,
            FirstLevelSpells = slot.Choice.FirstLevelSpells,
            Lists = slot.Choice.ClassIds.ConvertAll(classId => new BonusSpellListReadModel
            {
                ClassId = classId,
                ClassName = classNames.GetValueOrDefault(classId, classId),
                Spells = lists[classId]
                    .Where(s => draft.AllowsSource(s.Source)
                        && ((s.Level == 0 && slot.Choice.Cantrips > 0) || (s.Level == 1 && slot.Choice.FirstLevelSpells > 0)))
                    .OrderBy(s => s.Level)
                    .ThenBy(s => s.Name, StringComparer.Ordinal)
                    .ToList(),
            }),
            AbilityOptions = slot.Choice.AbilityOptions,
        });
    }
}
