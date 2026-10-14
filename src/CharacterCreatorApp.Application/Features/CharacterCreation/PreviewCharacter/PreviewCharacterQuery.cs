using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.PreviewCharacter;

/// <summary>
/// Works out the character a draft would produce, without saving it.
/// </summary>
public record PreviewCharacterQuery(Guid DraftId) : IQuery<Response<CharacterPreviewReadModel>>;
