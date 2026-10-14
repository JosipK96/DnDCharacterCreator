using CharacterCreatorApp.Domain.Common.Results;

using FluentValidation;

using MediatR;

namespace CharacterCreatorApp.Application.Common.Behaviours;

public class ValidationBehaviour<TRequest, TResponse>(IValidator<TRequest>? validator = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResponse
{
    private readonly IValidator<TRequest>? _validator = validator;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validator is null)
        {
            return await next();
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);

        if (validationResult.IsValid)
        {
            return await next();
        }

        // The code is the one the rule set with WithErrorCode. The property name goes in
        // FieldName, which is what the API keys a validation problem by. Putting the property
        // name in Code instead used to throw every WithErrorCode away.
        var errors = validationResult.Errors
            .ConvertAll(error => Error.Validation(
                code: error.ErrorCode,
                description: error.ErrorMessage,
                fieldName: error.PropertyName));

        return (dynamic)errors;
    }
}
