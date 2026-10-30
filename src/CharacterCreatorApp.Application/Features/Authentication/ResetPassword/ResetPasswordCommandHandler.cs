using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Authentication.ResetPassword;

public class ResetPasswordCommandHandler(IIdentityService identityService) : ICommandHandler<ResetPasswordCommand, Response>
{
    private readonly IIdentityService _identityService = identityService;

    public Task<Response> Handle(ResetPasswordCommand command, CancellationToken cancellationToken) =>
        _identityService.ResetPasswordAsync(command.Email, command.Token, command.NewPassword, cancellationToken);
}
