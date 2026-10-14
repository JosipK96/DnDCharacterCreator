namespace CharacterCreatorApp.Contracts.Requests.CharacterDrafts
{
    public class SetBackgroundApiRequest
    {
        public required string BackgroundId { get; set; }

        /// <summary>
        /// A 2024 background's ability increases, {"WIS": 2, "CHA": 1} or +1 to each of its three;
        /// none for a 2014 background.
        /// </summary>
        public Dictionary<string, int>? AbilityIncreases { get; set; }
    }
}
