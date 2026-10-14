using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseProficiencies;

public record ChooseProficienciesCommand(Guid DraftId, List<string> ProficiencyIds) : ICommand<Response>;