using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseLanguages;

public record ChooseLanguagesCommand(Guid DraftId, List<string> LanguageIds) : ICommand<Response>;
