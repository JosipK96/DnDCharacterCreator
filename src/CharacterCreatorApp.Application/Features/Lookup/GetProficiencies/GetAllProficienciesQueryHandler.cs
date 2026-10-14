using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetProficiencies;

public class GetAllProficienciesQueryHandler(IProficiencyRepository proficiencyRepository)
    : IQueryHandler<GetAllProficienciesQuery, Response<List<ProficiencyReadModel>>>
{
    private readonly IProficiencyRepository _proficiencyRepository = proficiencyRepository;

    public async Task<Response<List<ProficiencyReadModel>>> Handle(GetAllProficienciesQuery query, CancellationToken cancellationToken)
    {
        var proficiencies = await _proficiencyRepository.GetAllAsync(cancellationToken);
        return proficiencies;
    }
}
