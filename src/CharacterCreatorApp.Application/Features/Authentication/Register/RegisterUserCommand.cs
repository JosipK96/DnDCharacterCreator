using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Authentication.Register;

public record RegisterUserCommand(string Email, string UserName, string Password, string? DisplayName) : ICommand<Response<bool>>;