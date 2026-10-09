using System;
namespace Application
{
    public class GuestResponse
    {
        public long Id { get; set; }
        public long InvitationId { get; set; }
        public string Name { get; set; } = "";
        public string Contact { get; set; } = "";
        public bool Attending { get; set; }
        public int GuestCount { get; set; }
        public string Notes { get; set; } = "";
        public DateTime SubmittedAtUtc { get; set; }
    }
}
