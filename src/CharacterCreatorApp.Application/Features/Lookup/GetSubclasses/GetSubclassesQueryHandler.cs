using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetSubclasses;

public class GetSubclassesQueryHandler(
    ISubclassRepository subclassRepository,
    IClassRepository classRepository)
    : IQueryHandler<GetSubclassesQuery, Response<List<SubclassReadModel>>>
{
    private readonly ISubclassRepository _subclassRepository = subclassRepository;
    private readonly IClassRepository _classRepository = classRepository;

    public async Task<Response<List<SubclassReadModel>>> Handle(GetSubclassesQuery query, CancellationToken cancellationToken)
    {
        var classExists = await _classRepository.GetByIdAsync(query.ClassId, cancellationToken);

        if (classExists == null)
        {
            return ClassErrors.NotFoundById(query.ClassId);
        }

        var subclasses = await _subclassRepository.GetByClassIdAsync(query.ClassId, cancellationToken);

        return subclasses;
    }
}