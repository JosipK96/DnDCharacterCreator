using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetClasses
{
    public class GetClassesQueryHandler(IClassRepository classRepository) : IQueryHandler<GetClassesQuery, Response<List<ClassReadModel>>>
    {
        private readonly IClassRepository _classRepository = classRepository;

        public async Task<Response<List<ClassReadModel>>> Handle(GetClassesQuery query, CancellationToken cancellationToken)
        {
            return await _classRepository.GetClassesAsync(query.Edition, cancellationToken);
        }
    }
}
