using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.DeleteCharacter;

public class DeleteCharacterCommandHandler(
    ICharacterRepository characterRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : ICommandHandler<DeleteCharacterCommand, Response>
{
    private readonly ICharacterRepository _characterRepository = characterRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response> Handle(DeleteCharacterCommand command, CancellationToken cancellationToken)
    {
        var deleted = await _characterRepository.DeleteAsync(command.CharacterId, _userContext.UserId, cancellationToken);

        if (!deleted)
        {
            return CharacterErrors.NotFoundById(command.CharacterId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Response.Success();
    }
}