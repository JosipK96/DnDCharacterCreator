namespace CharacterCreatorApp.Contracts.Requests.CharacterDrafts
{
    public class SetClassApiRequest
    {
        public required string ClassId { get; set; }
        public string? SubclassId { get; set; }
    }
}
