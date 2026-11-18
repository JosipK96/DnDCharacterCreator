using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Authentication.ThrowTestError;

/// <summary>
/// The owner's way to prove that errors reach the error tracker (#77): throws on purpose, so
/// the answer is a 500 and an event, never a success.
/// </summary>
public record ThrowTestErrorCommand : ICommand<Response>;
