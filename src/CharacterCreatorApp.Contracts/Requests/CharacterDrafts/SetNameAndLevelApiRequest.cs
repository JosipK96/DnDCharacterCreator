namespace CharacterCreatorApp.Contracts.Requests.CharacterDrafts
{
    public class SetNameAndLevelApiRequest
    {
        public required string Name { get; set; }
        public int Level { get; set; }

        /// <summary>
        /// "2014" or "2024", the rules the character is built with. Changing it on a draft clears
        /// the race, class, background and the choices made for them.
        /// </summary>
        public required string Edition { get; set; }
    }
}
