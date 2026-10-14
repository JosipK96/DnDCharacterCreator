using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetClass
{
    public class SetCharacterClassCommandHandler(
        ICharacterDraftRepository characterDraftRepository,
        IUserContext userContext,
        IClassRepository classRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<SetCharacterClassCommand, Response>
    {
        private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
        private readonly IUserContext _userContext = userContext;
        private readonly IClassRepository _classRepository = classRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Response> Handle(SetCharacterClassCommand command, CancellationToken cancellationToken)
        {
            var characterDraft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

            if (characterDraft == null)
            {
                return CharacterDraftErrors.NotFoundById(command.DraftId);
            }

            var classEntity = await _classRepository.GetByIdWithSubclassesAsync(command.ClassId, cancellationToken);

            if (classEntity == null)
            {
                return ClassErrors.NotFoundById(command.ClassId);
            }

            if (classEntity.Edition != characterDraft.Edition)
            {
                return CharacterDraftErrors.EditionMismatch("class", classEntity.Name, classEntity.Edition, characterDraft.Edition);
            }

            if (!string.IsNullOrWhiteSpace(command.SubclassId))
            {
                var subclass = classEntity.Subclasses.FirstOrDefault(x => x.Id == command.SubclassId);

                if (subclass == null)
                {
                    return SubclassErrors.NotFoundById(command.SubclassId);
                }
            }

            characterDraft.SetClass(command.ClassId, command.SubclassId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Response.Success();
        }
    }
}
