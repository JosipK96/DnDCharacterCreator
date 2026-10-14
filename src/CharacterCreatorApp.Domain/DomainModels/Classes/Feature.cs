namespace CharacterCreatorApp.Domain.DomainModels.Classes;

public class Feature
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public int Level { get; set; }
    public string? ClassId { get; set; }
    public Class? Class { get; set; }
    public string? SubclassId { get; set; }
    public Subclass? Subclass { get; set; }
    public required string Type { get; set; }
}