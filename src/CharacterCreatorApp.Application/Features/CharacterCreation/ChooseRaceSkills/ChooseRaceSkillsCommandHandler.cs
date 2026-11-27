using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Choices;
using CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Loaders;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseRaceSkills;

public class ChooseRaceSkillsCommandHandler(
    ICharacterDraftRepository characterDraftRepository,
    IRaceRepository raceRepository,
    IBackgroundRepository backgroundRepository,
    IClassRepository classRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : ICommandHandler<ChooseRaceSkillsCommand, Response>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly DraftSelectionsLoader _selections = new(raceRepository, classRepository, backgroundRepository);
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response> Handle(ChooseRaceSkillsCommand command, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(command.DraftId);
        }

        var raceResult = await _selections.LoadRaceAsync(draft, "selecting race skills", cancellationToken);

        if (!raceResult.IsSuccess)
        {
            return raceResult.Errors;
        }

        var race = raceResult.Value;

        // A variant human's skill comes with the subrace
        var subrace = race.Subraces.FirstOrDefault(s => s.Id == draft.SubraceId);
        var skillCount = race.SkillChoiceCountWith(subrace);

        if (skillCount == 0)
        {
            draft.SetRaceSkills([]);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Response.Success();
        }

        if (command.ProficiencyIds.Count != skillCount)
        {
            return CharacterDraftErrors.InvalidRaceSkillCount(skillCount, subrace?.Name ?? race.Name, command.ProficiencyIds.Count);
        }

        if (command.ProficiencyIds.Count != command.ProficiencyIds.Distinct().Count())
        {
            return CharacterDraftErrors.DuplicateRaceSkills();
        }

        // Skill.All is what the sheet counts as a skill, so it decides here as well. This used
        // to compare each proficiency's type with "Skill" while the seed data says "skill", and
        // turned down every pair of skills a half-elf chose.
        var notSkills = command.ProficiencyIds.Where(id => !Skill.IsSkill(id)).ToList();

        if (notSkills.Count != 0)
        {
            return CharacterDraftErrors.InvalidRaceSkillType(notSkills);
        }

        // A 2024 elf's Keen Senses is one of three skills; a half-elf may take any two
        var notOffered = command.ProficiencyIds.Where(id => !race.OffersSkill(id, subrace)).ToList();

        if (notOffered.Count != 0)
        {
            return CharacterDraftErrors.RaceSkillsNotOffered(race.Name, notOffered, race.SkillChoiceOptionsWith(subrace));
        }

        // What the background and class give is known only when they are chosen; finalize checks
        // again once they all are
        var background = await _selections.LoadBackgroundIfChosenAsync(draft, cancellationToken);
        var @class = await _selections.LoadClassIfChosenAsync(draft, cancellationToken);
        var repeated = ProficiencyPickCheck.FindRepeated(draft, race, subrace, background, @class, raceSkillPicks: command.ProficiencyIds);

        if (repeated is not null)
        {
            return repeated.Value;
        }

        draft.SetRaceSkills(command.ProficiencyIds);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Response.Success();
    }
}
