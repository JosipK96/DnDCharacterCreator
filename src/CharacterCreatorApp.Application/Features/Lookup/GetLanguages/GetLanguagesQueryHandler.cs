using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Application.Features.Lookup.GetLanguages;

public class GetLanguagesQueryHandler(ILanguageRepository languageRepository)
    : IQueryHandler<GetLanguagesQuery, Response<List<LanguageReadModel>>>
{
    private readonly ILanguageRepository _languageRepository = languageRepository;

    // The list the language step offers, so it has to be the same set ChooseLanguages accepts
    public async Task<Response<List<LanguageReadModel>>> Handle(GetLanguagesQuery query, CancellationToken cancellationToken)
    {
        return await _languageRepository.GetChoosableAsync(query.Edition ?? RulesEdition.Edition2014, cancellationToken);
    }
}
