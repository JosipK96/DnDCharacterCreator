namespace CharacterCreatorApp.Domain.DomainModels.Feats
{
    public class FeatChoice
    {
        public required string Type { get; set; }
        public int Count { get; set; }
        public List<string> Options { get; set; } = [];
    }
}
