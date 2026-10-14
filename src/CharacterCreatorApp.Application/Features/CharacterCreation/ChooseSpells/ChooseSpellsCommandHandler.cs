using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseSpells
{
    public class ChooseSpellsCommandHandler(
        ICharacterDraftRepository characterDraftRepository,
        ISpellRepository spellRepository,
        IClassRepository classRepository,
        IUserContext userContext,
        IUnitOfWork unitOfWork) : ICommandHandler<ChooseSpellsCommand, Response>
    {
        private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
        private readonly ISpellRepository _spellRepository = spellRepository;
        private readonly IClassRepository _classRepository = classRepository;
        private readonly IUserContext _userContext = userContext;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Response> Handle(ChooseSpellsCommand command, CancellationToken cancellationToken)
        {
            var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

            if (draft == null)
            {
                return CharacterDraftErrors.NotFoundById(command.DraftId);
            }

            // Without this the lookup below ran with a null id and the player was told the class
            // was not found, when the real problem is that no class had been chosen yet.
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
            var allowance = @class.SpellAllowanceAt(level, subclass);
            var choosable = allowance == null
                ? []
                : await SpellChoice.LoadChoosableAsync(_spellRepository, @class, subclass, cancellationToken);

            var invalidPick = SpellChoice.FindInvalidPick(command.SpellIds, choosable, allowance, @class.Name, level);

            if (invalidPick is not null)
            {
                return invalidPick.Value;
            }

            draft.SetSpells(command.SpellIds);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Response.Success();
        }
    }
}
