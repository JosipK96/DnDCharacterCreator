using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Contracts.Responses;

using Mapster;

namespace CharacterCreatorApp.Api.Mapping.Configurations;

public class ClassMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ClassReadModel, ClassApiResponse>()
            .MapToConstructor(true);

        config.NewConfig<SpellcastingReadModel, SpellcastingApiResponse>()
            .MapToConstructor(true);

        config.NewConfig<SpellProgressionReadModel, SpellProgressionApiResponse>()
            .MapToConstructor(true);

        config.NewConfig<SubclassReadModel, SubclassApiResponse>()
            .MapToConstructor(true);
    }
}