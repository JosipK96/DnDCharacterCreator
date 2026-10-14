namespace CharacterCreatorApp.Domain.Common.Results;

public enum ErrorType
{
    Unexpected,
    Unauthorized,
    Validation,
    Failure,
    Conflict,
    NotFound,
    Forbidden
}