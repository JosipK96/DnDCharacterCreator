using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.DeleteDraft;

public class DeleteCharacterDraftCommandHandler(
    ICharacterDraftRepository characterDraftRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteCharacterDraftCommand, Response>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IUserContext _userContext = userContext;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Response> Handle(DeleteCharacterDraftCommand command, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

        // The repository only finds drafts that belong to this user, so someone else's draft is
        // simply not found; that also avoids telling a caller that the id exists. So is a completed
        // draft: it is a character's build, which goes only with the character.
        if (draft is null)
        {
            return CharacterDraftErrors.NotFoundById(command.DraftId);
        }

        await _characterDraftRepository.DeleteAsync(draft, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Response.Success();
    }
}