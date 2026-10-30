using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Authentication.CreatePasswordResetLink;

public class CreatePasswordResetLinkCommandHandler(IUserContext userContext, IAdminGate adminGate, IIdentityService identityService)
    : ICommandHandler<CreatePasswordResetLinkCommand, Response<string>>
{
    private readonly IUserContext _userContext = userContext;
    private readonly IAdminGate _adminGate = adminGate;
    private readonly IIdentityService _identityService = identityService;

    public async Task<Response<string>> Handle(CreatePasswordResetLinkCommand command, CancellationToken cancellationToken)
    {
        if (!_adminGate.IsAdmin(_userContext.Email))
        {
            return UserErrors.NotAnAdmin();
        }

        return await _identityService.CreatePasswordResetTokenAsync(command.Email, cancellationToken);
    }
}
