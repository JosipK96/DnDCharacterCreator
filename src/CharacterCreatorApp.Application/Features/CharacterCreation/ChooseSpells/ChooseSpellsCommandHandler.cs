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
        IRaceRepository raceRepository,
        IBackgroundRepository backgroundRepository,
        ILanguageRepository languageRepository,
        IFeatRepository featRepository,
        IUserContext userContext,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider) : ICommandHandler<ChooseSpellsCommand, Response>
    {
        private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
        private readonly ISpellRepository _spellRepository = spellRepository;
        private readonly IClassRepository _classRepository = classRepository;
        private readonly IUserContext _userContext = userContext;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly TimeProvider _timeProvider = timeProvider;

        // A 2014 cleric's prepared spells follow its final scores, worked out from everything the draft names
        private readonly DraftSourceLoader _sourceLoader = new(
            raceRepository, classRepository, backgroundRepository, languageRepository, featRepository, spellRepository);

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
            var allowance = await SpellChoice.AllowanceForAsync(
                draft, @class, subclass, level, _sourceLoader, _userContext.UserId, _timeProvider, cancellationToken);

            if (!allowance.IsSuccess)
            {
                return allowance.Errors;
            }

            var choosable = allowance.Value == null
                ? []
                : await SpellChoice.LoadChoosableAsync(_spellRepository, @class, subclass, cancellationToken);

            if (choosable.Find(s => command.SpellIds.Contains(s.Id) && !draft.AllowsSource(s.Source)) is { } notAllowed)
            {
                return CharacterDraftErrors.SourceNotAllowed("spell", notAllowed.Name, notAllowed.Source);
            }

            var invalidPick = SpellChoice.FindInvalidPick(command.SpellIds, choosable, allowance.Value, subclass, @class.Name, level);

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
