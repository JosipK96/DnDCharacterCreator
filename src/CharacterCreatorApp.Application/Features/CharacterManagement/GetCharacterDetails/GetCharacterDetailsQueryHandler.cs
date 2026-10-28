using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterDetails;

public class GetCharacterDetailsQueryHandler(
    ICharacterRepository characterRepository,
    IUserContext userContext,
    IProficiencyRepository proficiencyRepository,
    IFeatureRepository featureRepository,
    ISpellRepository spellRepository,
    ILanguageRepository languageRepository,
    IFeatRepository featRepository,
    IClassRepository classRepository,
    IRaceRepository raceRepository,
    IBackgroundRepository backgroundRepository) : IQueryHandler<GetCharacterDetailsQuery, Response<CharacterDetailsReadModel>>
{
    private readonly IUserContext _userContext = userContext;
    private readonly CharacterDetailsLoader _loader = new(
        characterRepository, proficiencyRepository, featureRepository, spellRepository, languageRepository,
        featRepository, classRepository, raceRepository, backgroundRepository);

    public async Task<Response<CharacterDetailsReadModel>> Handle(GetCharacterDetailsQuery query, CancellationToken cancellationToken)
    {
        return await _loader.LoadAsync(query.CharacterId, _userContext.UserId, cancellationToken);
    }
}
