using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseTools;

/// <summary>One list of proficiency ids per tool choice, in the order GetToolChoices lists them.</summary>
public record ChooseToolsCommand(Guid DraftId, List<List<string>> Picks) : ICommand<Response>;
