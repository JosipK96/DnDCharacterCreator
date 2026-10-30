using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Authentication.ResetPassword;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : ICommand<Response>;
