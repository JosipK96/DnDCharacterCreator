using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Contracts.Responses;

using Mapster;

namespace CharacterCreatorApp.Api.Mapping.Configurations;

public class BackgroundMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<BackgroundReadModel, BackgroundApiResponse>()
            .MapToConstructor(true);
        config.NewConfig<BackgroundProficiencyReadModel, BackgroundProficiencyApiResponse>();
        config.NewConfig<BackgroundFeatReadModel, BackgroundFeatApiResponse>();
        config.NewConfig<BackgroundFeatureReadModel, BackgroundFeatureApiResponse>();
    }
}