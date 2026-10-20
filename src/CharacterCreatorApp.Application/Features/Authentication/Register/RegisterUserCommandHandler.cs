using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Authentication.Register
{
    public class RegisterUserCommandHandler(IIdentityService identityService, IInviteCodeGate inviteCodeGate) : ICommandHandler<RegisterUserCommand, Response<bool>>
    {
        private readonly IIdentityService _identityService = identityService;
        private readonly IInviteCodeGate _inviteCodeGate = inviteCodeGate;

        public async Task<Response<bool>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
        {
            // Before the email check, so someone without the code can't learn which emails have accounts.
            if (!_inviteCodeGate.Admits(command.InviteCode))
            {
                return UserErrors.InvalidInviteCode();
            }

            // Identity would refuse a duplicate email too, but as a validation error; checking
            // first is what makes it a 409.
            if (await _identityService.IsEmailInUseAsync(command.Email, cancellationToken))
            {
                return UserErrors.EmailAlreadyInUse();
            }

            var created = await _identityService.CreateUserAsync(
                command.UserName, command.Email, command.Password, command.DisplayName, cancellationToken);

            if (!created.IsSuccess)
            {
                return created.Errors;
            }

            return true;
        }
    }
}
