namespace CharacterCreatorApp.Domain.Common.Results;

public interface IResponse
{
    bool IsSuccess { get; }
    List<Error>? Errors { get; }
}