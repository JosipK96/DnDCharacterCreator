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
            .Map(dest => dest.CanBeTakenForImprovement, src => FeatCategory.CanBeTakenForImprovement(src.Edition, src.Category))
            .MapToConstructor(true);

        config.NewConfig<FightingStylesReadModel, FightingStylesApiResponse>()
            .MapToConstructor(true);

        config.NewConfig<FeatAbilityIncrease, FeatAbilityIncreaseApiResponse>()
            .MapToConstructor(true);
    }
}
