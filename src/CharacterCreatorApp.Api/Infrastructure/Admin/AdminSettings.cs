namespace CharacterCreatorApp.Api.Infrastructure.Admin;

/// <summary>
/// Who the owner is: the email of the one account that may issue password reset links. Set on
/// the host (Admin__Email), never in appsettings; left empty, nobody is an admin.
/// </summary>
public class AdminSettings
{
    public const string SectionName = "Admin";

    public string? Email { get; init; }
}
