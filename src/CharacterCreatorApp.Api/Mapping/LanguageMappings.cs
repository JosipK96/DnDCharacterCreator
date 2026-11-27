using CharacterCreatorApp.Application.ReadModels.Languages;
using CharacterCreatorApp.Contracts.Responses.Languages;

namespace CharacterCreatorApp.Api.Mapping;

/// <summary>Turns the language read model into the response the client reads.</summary>
public static class LanguageMappings
{
    public static LanguageApiResponse ToResponse(this LanguageReadModel language) =>
        new(Id: language.Id, Name: language.Name, Type: language.Type);
}
