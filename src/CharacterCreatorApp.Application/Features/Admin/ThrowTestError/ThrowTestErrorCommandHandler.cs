using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Admin.ThrowTestError;

public class ThrowTestErrorCommandHandler(IUserContext userContext, IAdminGate adminGate)
    : ICommandHandler<ThrowTestErrorCommand, Response>
{
    public const string Message = "Deliberate test error, thrown by the owner to check error reporting.";

    private readonly IUserContext _userContext = userContext;
    private readonly IAdminGate _adminGate = adminGate;

    public Task<Response> Handle(ThrowTestErrorCommand command, CancellationToken cancellationToken)
    {
        if (!_adminGate.IsAdmin(_userContext.Email))
        {
            return Task.FromResult<Response>(UserErrors.NotAnAdmin());
        }

        throw new InvalidOperationException(Message);
    }
}
