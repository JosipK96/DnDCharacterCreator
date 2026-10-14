using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseBonusSpells;

public class ChooseBonusSpellsCommandHandler(
    ICharacterDraftRepository characterDraftRepository,
    IRaceRepository raceRepository,
    IBackgroundRepository backgroundRepository,
    IFeatRepository featRepository,
    ISpellRepository spellRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork) : ICommandHandler<ChooseBonusSpellsCommand, Response>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;
    private readonly IFeatRepository _featRepository = featRepository;
    private readonly ISpellRepository _spellRepository = spellRepository;
    private readonly IUserContext _userContext = userContext;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Response> Handle(ChooseBonusSpellsCommand command, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(command.DraftId);
        }

        var (raceSlot, featSlots) = await BonusSpells.LoadSlotsAsync(
            draft, _raceRepository, _backgroundRepository, _featRepository, cancellationToken);
        var lists = await BonusSpells.LoadListsAsync(
            _spellRepository, raceSlot is null ? featSlots : featSlots.Append(raceSlot), cancellationToken);

        var invalidPick = BonusSpells.FindInvalidPick(draft, raceSlot, featSlots, command.RaceSpellIds, command.FeatSpells, lists);

        if (invalidPick is not null)
        {
            return invalidPick.Value;
        }

        draft.ChooseBonusSpells(command.RaceSpellIds, command.FeatSpells);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Response.Success();
    }
}
