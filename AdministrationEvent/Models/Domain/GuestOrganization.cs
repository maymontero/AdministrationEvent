using System.Collections.Generic;
namespace Application
{
    public class GuestTag
    {
        public long Id { get; set; }
        public long InvitationId { get; set; }
        public string Name { get; set; } = "";
        public string Color { get; set; } = "#54745b";
    }
    public class SeatingTable
    {
        public long Id { get; set; }
        public long InvitationId { get; set; }
        public string Name { get; set; } = "";
        public int Capacity { get; set; } = 8;
    }
    public class GuestTagAssignment
    {
        public long GuestId { get; set; }
        public long TagId { get; set; }
    }
}
