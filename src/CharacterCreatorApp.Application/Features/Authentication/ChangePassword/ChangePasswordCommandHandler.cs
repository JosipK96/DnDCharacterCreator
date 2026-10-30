using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Authentication.ChangePassword;

public class ChangePasswordCommandHandler(IUserContext userContext, IIdentityService identityService) : ICommandHandler<ChangePasswordCommand, Response>
{
    private readonly IUserContext _userContext = userContext;
    private readonly IIdentityService _identityService = identityService;

    public Task<Response> Handle(ChangePasswordCommand command, CancellationToken cancellationToken) =>
        _identityService.ChangePasswordAsync(_userContext.UserId, command.CurrentPassword, command.NewPassword, cancellationToken);
}
