namespace CharacterCreatorApp.Application.ReadModels
{
    public class FeatChoiceReadModel
    {
        public required string Type { get; set; }
        public int Count { get; set; }
        public IReadOnlyCollection<string> Options { get; set; } = [];
    }
}
