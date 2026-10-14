using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Contracts.Responses;

using Mapster;

namespace CharacterCreatorApp.Api.Mapping.Configurations;

public class BookMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<BookReadModel, BookApiResponse>()
            .MapToConstructor(true);
    }
}
