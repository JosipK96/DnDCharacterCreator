using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Contracts.Responses;

using Mapster;

namespace CharacterCreatorApp.Api.Mapping.Configurations;

public class RaceMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RaceReadModel, RaceApiResponse>()
            .MapToConstructor(true);

        config.NewConfig<AbilityScoreBonusReadModel, AbilityScoreBonusApiResponse>()
            .Map(dest => dest.AbilityScore, src => src.AbilityScore.Name)
            .MapToConstructor(true);

        config.NewConfig<TraitReadModel, TraitApiResponse>()
            .MapToConstructor(true);

        config.NewConfig<SubraceReadModel, SubraceApiResponse>()
            .MapToConstructor(true);
    }
}