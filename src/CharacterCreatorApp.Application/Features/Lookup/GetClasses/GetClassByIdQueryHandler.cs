using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetClasses
{
    public class GetClassByIdQueryHandler(IClassRepository classRepository) : IQueryHandler<GetClassByIdQuery, Response<ClassReadModel>>
    {
        private readonly IClassRepository _classRepository = classRepository;

        public async Task<Response<ClassReadModel>> Handle(GetClassByIdQuery request, CancellationToken cancellationToken)
        {
            var classEntity = await _classRepository.GetByIdAsync(request.Id, cancellationToken);

            if (classEntity == null)
            {
                return ClassErrors.NotFoundById(request.Id);
            }

            return classEntity;
        }
    }
}
