using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Authentication.CreatePasswordResetLink;

/// <summary>Answers with the one-time reset token for the account; the Api turns it into a link.</summary>
public record CreatePasswordResetLinkCommand(string Email) : ICommand<Response<string>>;
