using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetRace
{
    public class SetCharacterRaceCommandHandler(
        ICharacterDraftRepository draftRepository,
        IRaceRepository raceRepository,
        IUnitOfWork unitOfWork,
        IUserContext userContext) : ICommandHandler<SetCharacterRaceCommand, Response>
    {
        private readonly ICharacterDraftRepository _draftRepository = draftRepository;
        private readonly IRaceRepository _raceRepository = raceRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IUserContext _userContext = userContext;

        public async Task<Response> Handle(SetCharacterRaceCommand command, CancellationToken cancellationToken)
        {
            var characterDraft = await _draftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

            if (characterDraft == null)
            {
                return CharacterDraftErrors.NotFoundById(command.DraftId);
            }

            var race = await _raceRepository.GetByIdWithSubracesAsync(command.RaceId, cancellationToken);

            if (race == null)
            {
                return RaceErrors.NotFoundById(command.RaceId);
            }

            if (race.Edition != characterDraft.Edition)
            {
                return CharacterDraftErrors.EditionMismatch("race", race.Name, race.Edition, characterDraft.Edition);
            }

            Subrace? subrace = null;

            if (!string.IsNullOrWhiteSpace(command.SubraceId))
            {
                subrace = race.Subraces.FirstOrDefault(x => x.Id == command.SubraceId);

                if (subrace == null)
                {
                    return SubraceErrors.NotFoundById(command.SubraceId);
                }
            }

            var abilityIncreases = command.AbilityIncreases ?? [];
            var abilityChoice = race.AbilityChoiceWith(subrace);

            if (!abilityChoice.Allows(abilityIncreases))
            {
                return CharacterDraftErrors.InvalidRaceAbilityIncreases(
                    subrace?.Name ?? race.Name, abilityChoice.Points, abilityChoice.Options.Select(id => AbilityScore.FromId(id).Name).ToList());
            }

            characterDraft.SetRace(command.RaceId, command.SubraceId, abilityIncreases);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Response.Success();
        }
    }
}
