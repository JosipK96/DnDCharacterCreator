using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Contracts.Responses;

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
