using System.Security.Cryptography;
using System.Text;

using CharacterCreatorApp.Application.Features.Authentication;

using Microsoft.Extensions.Options;

namespace CharacterCreatorApp.Api.Infrastructure.Registration;

public class InviteCodeGate(IOptions<RegistrationSettings> settings) : IInviteCodeGate
{
    private readonly RegistrationSettings _settings = settings.Value;

    public bool Admits(string? inviteCode)
    {
        if (string.IsNullOrEmpty(_settings.InviteCode))
        {
            return true;
        }

        // Hashing first gives both sides the same length, so the fixed-time comparison
        // leaks neither the code's content nor its length.
        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(_settings.InviteCode));
        var given = SHA256.HashData(Encoding.UTF8.GetBytes(inviteCode ?? string.Empty));

        return CryptographicOperations.FixedTimeEquals(expected, given);
    }
}
