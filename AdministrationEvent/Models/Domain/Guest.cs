using System;
namespace Application
{
    public class Guest
    {
        public long Id { get; set; }
        public long? TableId { get; set; }
        public long InvitationId { get; set; }
        public string Name { get; set; } = "";
        public string Contact { get; set; } = "";
        public int InvitedCount { get; set; } = 1;
        public string Notes { get; set; } = "";
        public bool IsArchived { get; set; }
        public bool IsWaitlisted { get; set; }
    }
}
