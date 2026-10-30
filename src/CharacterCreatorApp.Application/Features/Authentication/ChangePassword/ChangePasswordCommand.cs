using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Authentication.ChangePassword;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand<Response>;
