using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Authentication.Login;

public record LoginUserCommand(string Email, string Password) : ICommand<Response<LoginResult>>;
