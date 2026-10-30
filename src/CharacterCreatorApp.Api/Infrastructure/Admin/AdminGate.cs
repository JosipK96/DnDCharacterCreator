using CharacterCreatorApp.Application.Features.Authentication;

using Microsoft.Extensions.Options;

namespace CharacterCreatorApp.Api.Infrastructure.Admin;

public class AdminGate(IOptions<AdminSettings> settings) : IAdminGate
{
    private readonly AdminSettings _settings = settings.Value;

    public bool IsAdmin(string? email) =>
        !string.IsNullOrWhiteSpace(_settings.Email)
        && !string.IsNullOrWhiteSpace(email)
        && string.Equals(_settings.Email.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase);
}
