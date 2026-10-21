using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Contracts.Responses;
using CharacterCreatorApp.Domain.DomainModels.Characters;

using Mapster;

namespace CharacterCreatorApp.Api.Mapping.Configurations;

public class CharacterDraftMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CharacterDraftDetailsReadModel, CharacterDraftDetailsApiResponse>();
        config.NewConfig<FeatSpellPick, FeatSpellPickApiModel>();
        config.NewConfig<FeatProficiencyPick, FeatProficiencyPickApiModel>();

        config.NewConfig<BonusSpellChoiceReadModel, BonusSpellChoiceApiResponse>()
            .MapToConstructor(true);
        config.NewConfig<BonusSpellListReadModel, BonusSpellListApiResponse>()
            .MapToConstructor(true);

        config.NewConfig<ToolChoiceReadModel, ToolChoiceApiResponse>()
            .MapToConstructor(true);

        config.NewConfig<CharacterDetailsReadModel, CharacterDetailsApiResponse>()
            .MapToConstructor(true);
    }
}