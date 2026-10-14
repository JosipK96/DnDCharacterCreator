using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Authentication.Login
{
    public class LoginUserCommandHandler(IIdentityService identityService,
        IJwtHelper jwtHelper) : ICommandHandler<LoginUserCommand, Response<LoginResult>>
    {
        private readonly IIdentityService _identityService = identityService;
        private readonly IJwtHelper _jwtHelper = jwtHelper;

        public async Task<Response<LoginResult>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
        {
            var user = await _identityService.CheckCredentialsAsync(command.Email, command.Password, cancellationToken);

            if (!user.IsSuccess)
            {
                return user.Errors;
            }

            return new LoginResult(_jwtHelper.GenerateJwtToken(user.Value));
        }
    }
}
