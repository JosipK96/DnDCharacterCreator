using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Contracts.Responses;
using CharacterCreatorApp.Domain.DomainModels.Feats;

using Mapster;

namespace CharacterCreatorApp.Api.Mapping.Configurations;

public class FeatMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<FeatReadModel, FeatApiResponse>()
            .Map(dest => dest.Prerequisites, src => src.Prerequisites.Describe().ToList())
            .MapToConstructor(true);

        config.NewConfig<FeatAbilityIncrease, FeatAbilityIncreaseApiResponse>()
            .MapToConstructor(true);
    }
}
