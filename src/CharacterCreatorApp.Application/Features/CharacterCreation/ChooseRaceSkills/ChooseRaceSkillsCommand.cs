using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseRaceSkills;

public record ChooseRaceSkillsCommand(Guid DraftId, List<string> ProficiencyIds) : ICommand<Response>;
