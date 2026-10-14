using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.GetToolChoices;

/// <summary>The race's tool choices, then the background's, then the class's, in the order ChooseTools takes the picks.</summary>
public record GetToolChoicesQuery(Guid DraftId) : IQuery<Response<List<ToolChoiceReadModel>>>;
