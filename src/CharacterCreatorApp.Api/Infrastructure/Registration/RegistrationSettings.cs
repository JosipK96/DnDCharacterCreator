namespace CharacterCreatorApp.Api.Infrastructure.Registration;

/// <summary>
/// Who may register. The invite code is handed to the group by hand and set on the host
/// (Registration__InviteCode); left empty, anyone can register, as in local development.
/// </summary>
public class RegistrationSettings
{
    public const string SectionName = "Registration";

    public string? InviteCode { get; init; }
}
