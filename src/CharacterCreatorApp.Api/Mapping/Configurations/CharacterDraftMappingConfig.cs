using CharacterCreatorApp.Application.ReadModels.Characters;
using CharacterCreatorApp.Application.ReadModels.Proficiencies;
using CharacterCreatorApp.Application.ReadModels.Spells;
using CharacterCreatorApp.Contracts.Responses.Characters;
using CharacterCreatorApp.Contracts.Responses.Feats;
using CharacterCreatorApp.Contracts.Responses.Proficiencies;
using CharacterCreatorApp.Contracts.Responses.Spells;
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