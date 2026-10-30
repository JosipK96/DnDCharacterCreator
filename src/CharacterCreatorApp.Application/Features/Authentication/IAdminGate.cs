namespace CharacterCreatorApp.Application.Features.Authentication;

/// <summary>Says whether a signed-in user is the owner, the one who may issue password reset links.</summary>
public interface IAdminGate
{
    /// <summary>False when no admin is configured, so a missing setting never makes everyone an admin.</summary>
    bool IsAdmin(string? email);
}
