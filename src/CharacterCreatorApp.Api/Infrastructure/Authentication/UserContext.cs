using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using CharacterCreatorApp.Application.Common.Interfaces;

namespace CharacterCreatorApp.Api.Infrastructure.Authentication
{
    public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public Guid UserId
        {
            get
            {
                var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

                return Guid.TryParse(userId, out var id)
                    ? id
                    : throw new InvalidOperationException(
                        "The request has no authenticated user. Every endpoint whose handler reads the user must be [Authorize].");
            }
        }

        // Signed into the token by JwtHelper, so it is the address the account signed in with. The
        // bearer handler maps the "email" claim to ClaimTypes.Email unless told not to.
        public string? Email
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;

                return user?.FindFirstValue(ClaimTypes.Email) ?? user?.FindFirstValue(JwtRegisteredClaimNames.Email);
            }
        }
    }
}
