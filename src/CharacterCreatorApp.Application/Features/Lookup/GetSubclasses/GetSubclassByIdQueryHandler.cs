using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetSubclasses;

public class GetSubclassByIdQueryHandler(ISubclassRepository subclassRepository)
    : IQueryHandler<GetSubclassByIdQuery, Response<SubclassReadModel>>
{
    private readonly ISubclassRepository _subclassRepository = subclassRepository;

    public async Task<Response<SubclassReadModel>> Handle(GetSubclassByIdQuery query, CancellationToken cancellationToken)
    {
        var subclass = await _subclassRepository.GetByIdAsync(query.Id, cancellationToken);

        if (subclass == null)
        {
            return SubclassErrors.NotFoundById(query.Id);
        }

        return subclass;
    }
}
