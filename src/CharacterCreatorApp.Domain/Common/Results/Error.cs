namespace CharacterCreatorApp.Domain.Common.Results;

public readonly record struct Error
{
    public ErrorType Type { get; }
    public string Code { get; }
    public string Description { get; }
    public string? FieldName { get; }

    private Error(ErrorType type, string code, string description, string? fieldName = null)
    {
        Type = type;
        Code = code;
        Description = description;
        FieldName = fieldName;
    }

    public override string ToString()
    {
        if (FieldName == null)
        {
            return $"Type: {Type}, Code: {Code}, Description: {Description}";
        }

        return $"Type: {Type}, Code: {Code}, Description: {Description}, FieldName: {FieldName}";
    }

    public static Error CreateError(ErrorType type, string code, string description, string? fieldName = null)
    {
        return new Error(type, code, description, fieldName);
    }

    public static Error Unexpected(string code = "Unexpected", string description = "An Unexpected error has occured.")
    {
        return CreateError(ErrorType.Unexpected, code, description);
    }

    public static Error Unauthorized(string code = "Unauthorized", string description = "An Unauthorized error has occured.")
    {
        return CreateError(ErrorType.Unauthorized, code, description);
    }

    public static Error Validation(string code = "Validation", string description = "A Validation error has occured.", string? fieldName = null)
    {
        return CreateError(ErrorType.Validation, code, description, fieldName);
    }

    public static Error Failure(string code = "Failure", string description = "A Failure error has occured.")
    {
        return CreateError(ErrorType.Failure, code, description);
    }

    public static Error Conflict(string code = "Conflict", string description = "A Conflict error has occured.")
    {
        return CreateError(ErrorType.Conflict, code, description);
    }

    public static Error NotFound(string code = "NotFound", string description = "A Not Found error has occured.")
    {
        return CreateError(ErrorType.NotFound, code, description);
    }

    public static Error Forbidden(string code = "Forbidden", string description = "A Forbidden error has occured.")
    {
        return CreateError(ErrorType.Forbidden, code, description);
    }
}