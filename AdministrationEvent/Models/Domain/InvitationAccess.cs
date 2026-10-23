using System;
namespace Application
{
    public class InvitationAccess
    {
        public long UserId { get; set; }
        public long InvitationId { get; set; }
        public bool CanDesign { get; set; }
        public DateTime? AvailableFromUtc { get; set; }
    }
}
