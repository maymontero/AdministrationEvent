namespace Application
{
    public class VisualDesign
    {
        public long Id { get; set; }
        public long InvitationId { get; set; }
        public string DraftJson { get; set; } = "";
        public string PublishedJson { get; set; }
        public int Version { get; set; }
    }
}
