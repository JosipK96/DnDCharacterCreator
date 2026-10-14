using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetSubclass;

public class SetCharacterSubclassCommandHandler(
    ICharacterDraftRepository characterDraftRepository,
    IClassRepository classRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : ICommandHandler<SetCharacterSubclassCommand, Response>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response> Handle(SetCharacterSubclassCommand command, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(command.DraftId);
        }

        if (string.IsNullOrEmpty(draft.ClassId))
        {
            return CharacterDraftErrors.ClassRequired("selecting a subclass");
        }

        var @class = await _classRepository.GetByIdWithDetailsAsync(draft.ClassId, cancellationToken);

        if (@class == null)
        {
            return ClassErrors.NotFoundById(draft.ClassId);
        }

        // Which level a subclass becomes available at is class-specific: clerics, sorcerers and
        // warlocks choose at 1, druids and wizards at 2, everyone else at 3.
        if (draft.Level < @class.SubclassLevel)
        {
            return CharacterDraftErrors.SubclassLevelRequirement(@class.Name, @class.SubclassLevel, draft.Level ?? 0);
        }

        var subclass = @class.Subclasses.FirstOrDefault(s => s.Id == command.SubclassId);

        if (subclass == null)
        {
            return CharacterDraftErrors.InvalidSubclass(
                command.SubclassId!, draft.ClassId, @class.Subclasses.Select(s => s.Name));
        }

        if (!draft.AllowsSource(subclass.Source))
        {
            return CharacterDraftErrors.SourceNotAllowed("subclass", subclass.Name, subclass.Source);
        }

        draft.SetSubclass(command.SubclassId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Response.Success();
    }
}