using Microsoft.AspNetCore.Identity;

namespace CharacterCreatorApp.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? DisplayName { get; set; }

        public static ApplicationUser Create(string userName, string email, string? displayName)
        {
            return new ApplicationUser { UserName = userName, Email = email, DisplayName = displayName };
        }
    }
}
