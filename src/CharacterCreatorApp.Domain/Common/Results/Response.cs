using System.Text;

namespace CharacterCreatorApp.Domain.Common.Results;

/// <summary>
/// The result of an operation that returns a value: either the value or the errors that stopped
/// it. Handlers return one of these rather than throwing, and the implicit conversions mean a
/// handler can `return value;` or `return SomeErrors.NotFound();` without naming the type.
/// </summary>
public readonly record struct Response<T> : IResponse
{
    private readonly T? _value;
    private readonly List<Error>? _errors;

    private Response(T value)
    {
        IsSuccess = true;
        _value = value;
        _errors = null;
    }

    private Response(Error error)
    {
        IsSuccess = false;
        _value = default;
        _errors = [error];
    }

    private Response(List<Error> errors)
    {
        IsSuccess = false;
        _value = default;
        _errors = errors;
    }

    public bool IsSuccess { get; }

    /// <summary>
    /// The value. Reading it from a failed response throws rather than handing back
    /// <c>default</c>, which for a reference type is a null the caller never checked for and for
    /// a value type is a plausible-looking zero or empty Guid. Check <see cref="IsSuccess"/> first.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(
            $"Cannot read Value of a failed response. Errors: {string.Join("; ", Errors.Select(e => e.Code))}");

    public List<Error> Errors => IsSuccess ? [] : _errors ?? [];

    public static implicit operator Response<T>(T value) => new(value);

    public static implicit operator Response<T>(Error error) => new(error);

    public static implicit operator Response<T>(List<Error> errors) => new(errors);

    /// <summary>Throws if the operation failed, so a caller that cannot handle failure says so.</summary>
    public Response<T> EnsureSuccessResponse()
    {
        if (!IsSuccess)
        {
            throw new InvalidOperationException(string.Join("\n", Errors.Select(e => e.ToString())));
        }

        return this;
    }

    /// <summary>
    /// Replaces the record's synthesized member printing, which reads every public property —
    /// including <see cref="Value"/>, which throws on a failed response. Without this, ToString()
    /// on a failure would throw, and so would any assertion message or log line that formats one.
    /// </summary>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("IsSuccess = ").Append(IsSuccess);

        if (IsSuccess)
        {
            builder.Append(", Value = ").Append(_value);
        }
        else
        {
            builder.Append(", Errors = [").Append(string.Join(", ", Errors.Select(e => e.Code))).Append(']');
        }

        return true;
    }
}

/// <summary>
/// The result of an operation that has nothing to return. Kept as its own type rather than
/// Response&lt;NoData&gt; so a command handler's signature reads as what it is.
/// </summary>
public readonly record struct Response : IResponse
{
    private readonly List<Error>? _errors;

    private Response(bool isSuccess)
    {
        IsSuccess = isSuccess;
        _errors = null;
    }

    private Response(Error error)
    {
        IsSuccess = false;
        _errors = [error];
    }

    private Response(List<Error> errors)
    {
        IsSuccess = false;
        _errors = errors;
    }

    public bool IsSuccess { get; }

    public List<Error> Errors => IsSuccess ? [] : _errors ?? [];

    public static Response Success() => new(true);

    public static implicit operator Response(Error error) => new(error);

    public static implicit operator Response(List<Error> errors) => new(errors);

    /// <summary>Throws if the operation failed, so a caller that cannot handle failure says so.</summary>
    public Response EnsureSuccessResponse()
    {
        if (!IsSuccess)
        {
            throw new InvalidOperationException(string.Join("\n", Errors.Select(e => e.ToString())));
        }

        return this;
    }
}
