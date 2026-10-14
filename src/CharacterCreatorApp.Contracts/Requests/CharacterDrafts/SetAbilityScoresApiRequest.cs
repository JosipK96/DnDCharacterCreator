namespace CharacterCreatorApp.Contracts.Requests.CharacterDrafts
{
    public class SetAbilityScoresApiRequest
    {
        public Dictionary<string, int> Scores { get; set; } = [];
    }
}
