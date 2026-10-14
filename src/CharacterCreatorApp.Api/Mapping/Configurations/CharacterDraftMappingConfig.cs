using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Contracts.Responses;

using Mapster;

namespace CharacterCreatorApp.Api.Mapping.Configurations;

public class CharacterDraftMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CharacterDraftDetailsReadModel, CharacterDraftDetailsApiResponse>();

        config.NewConfig<CharacterDetailsReadModel, CharacterDetailsApiResponse>()
            .MapToConstructor(true);
    }
}