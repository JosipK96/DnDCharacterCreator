using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
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
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response> Handle(ChooseRaceSkillsCommand command, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(command.DraftId);
        }

        if (string.IsNullOrEmpty(draft.RaceId))
        {
            return CharacterDraftErrors.RaceRequired("selecting race skills");
        }

        var race = await _raceRepository.GetByIdWithDetailsAsync(draft.RaceId, cancellationToken);

        if (race == null)
        {
            return RaceErrors.NotFoundById(draft.RaceId);
        }

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
        var background = string.IsNullOrEmpty(draft.BackgroundId)
            ? null
            : await _backgroundRepository.GetByIdWithDetailsAsync(draft.BackgroundId, cancellationToken);
        var @class = string.IsNullOrEmpty(draft.ClassId)
            ? null
            : await _classRepository.GetByIdWithDetailsAsync(draft.ClassId, cancellationToken);
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
