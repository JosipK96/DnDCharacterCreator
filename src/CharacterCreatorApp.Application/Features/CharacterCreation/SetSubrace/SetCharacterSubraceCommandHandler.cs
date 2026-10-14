using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetSubrace;

public class SetCharacterSubraceCommandHandler(
    ICharacterDraftRepository characterDraftRepository,
    IRaceRepository raceRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : ICommandHandler<SetCharacterSubraceCommand, Response>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response> Handle(SetCharacterSubraceCommand command, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(command.DraftId);
        }

        if (string.IsNullOrEmpty(draft.RaceId))
        {
            return CharacterDraftErrors.RaceRequired("selecting a subrace");
        }

        if (command.SubraceId == null)
        {
            draft.SetSubrace(null);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Response.Success();
        }

        var race = await _raceRepository.GetByIdWithDetailsAsync(draft.RaceId, cancellationToken);

        if (race == null)
        {
            return RaceErrors.NotFoundById(draft.RaceId);
        }

        var subrace = race.Subraces.FirstOrDefault(s => s.Id == command.SubraceId);

        if (subrace == null)
        {
            if (!race.Subraces.Any())
            {
                return CharacterDraftErrors.NoSubracesAvailable(race.Name);
            }

            return CharacterDraftErrors.InvalidSubrace(command.SubraceId!, draft.RaceId, race.Subraces.Select(s => s.Name));
        }

        draft.SetSubrace(command.SubraceId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Response.Success();
    }
}