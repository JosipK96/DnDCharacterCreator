using CharacterCreatorApp.Application.ReadModels.Languages;
using CharacterCreatorApp.Contracts.Responses.Languages;

using Mapster;

namespace CharacterCreatorApp.Api.Mapping.Configurations;

public class LanguageMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<LanguageReadModel, LanguageApiResponse>()
            .MapToConstructor(true);
    }
}
