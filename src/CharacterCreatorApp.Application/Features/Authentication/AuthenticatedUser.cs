namespace CharacterCreatorApp.Application.Features.Authentication;

/// <summary>What a token is issued for: the account whose credentials were just checked.</summary>
public sealed record AuthenticatedUser(Guid Id, string Email);
