using CharacterCreatorApp.Application.Features.Authentication;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CharacterCreatorApp.Api.Helpers.Jwt
{
    public class JwtHelper(IOptions<JwtSettings> settings, TimeProvider timeProvider) : IJwtHelper
    {
        private readonly JwtSettings _settings = settings.Value;
        private readonly TimeProvider _timeProvider = timeProvider;

        public string GenerateJwtToken(AuthenticatedUser user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(_settings.ExpiryInMinutes);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
