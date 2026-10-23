namespace Application
{
    public class SeatingPlan
    {
        public long Id { get; set; }
        public long InvitationId { get; set; }
        public string LayoutJson { get; set; } = "[]";
        public bool UseVenueImage { get; set; }
        public byte[] Image { get; set; }
        public string ImageType { get; set; }
    }
}
