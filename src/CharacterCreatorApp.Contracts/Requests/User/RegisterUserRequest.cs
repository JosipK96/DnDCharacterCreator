namespace CharacterCreatorApp.Contracts.Requests.User
{
    public class RegisterUserRequest
    {
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public string? DisplayName { get; set; }
    }
}