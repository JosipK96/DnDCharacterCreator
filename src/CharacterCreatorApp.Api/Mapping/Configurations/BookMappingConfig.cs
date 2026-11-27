using CharacterCreatorApp.Application.ReadModels.Books;
using CharacterCreatorApp.Contracts.Responses.Books;

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
