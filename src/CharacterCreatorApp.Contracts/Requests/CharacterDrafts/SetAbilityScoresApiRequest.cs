namespace CharacterCreatorApp.Contracts.Requests.CharacterDrafts
{
    public class SetAbilityScoresApiRequest
    {
        public Dictionary<string, int> Scores { get; set; } = [];

        /// <summary>
        /// How the scores were generated: "standard-array", "point-buy" or "rolled". The scores
        /// must be ones that method can produce.
        /// </summary>
        public string Method { get; set; } = string.Empty;
    }
}
